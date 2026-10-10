using TUnit.Mocks;
using TUnit.Mocks.Exceptions;

namespace TUnit.Mocks.Tests;

public interface IPassThroughDependency
{
    int GetValue();
}

public class PassThroughSubject
{
    public virtual string Name { get; set; } = "base-name";
    public virtual string this[int index] { get => "base-" + index; set { } }
    public virtual int Zero() => 100;
    public virtual int One(int a) => a + 1;
    public virtual int Two(int a, int b) => a + b;
    public virtual int Three(int a, int b, int c) => a + b + c;
    public virtual int Eight(int a, int b, int c, int d, int e, int f, int g, int h) => a + b + c + d + e + f + g + h;
    public virtual string Greet(string name) => $"Hello, {name}";
    public virtual Task<int> GetAsync() => Task.FromResult(7);
    public virtual IPassThroughDependency GetDependency() => throw new InvalidOperationException("base should not run");
    public virtual void Run() => BaseRunCount++;
    public int BaseRunCount { get; private set; }
}

public class PassThroughTests
{
    [Test]
    public async Task PassThrough_Defaults_To_True_And_Runs_Base()
    {
        var mock = PassThroughSubject.Mock();

        await Assert.That(Mock.GetPassThrough(mock)).IsTrue();
        await Assert.That(mock.Object.Two(2, 3)).IsEqualTo(5);
        await Assert.That(mock.Object.Name).IsEqualTo("base-name");
    }

    [Test]
    public async Task PassThrough_False_Returns_Defaults_For_Unconfigured_Members()
    {
        var mock = PassThroughSubject.Mock();
        Mock.SetPassThrough(mock, false);
        var subject = mock.Object;

        await Assert.That(subject.Zero()).IsEqualTo(0);
        await Assert.That(subject.One(5)).IsEqualTo(0);
        await Assert.That(subject.Two(2, 3)).IsEqualTo(0);
        await Assert.That(subject.Three(1, 2, 3)).IsEqualTo(0);
        await Assert.That(subject.Eight(1, 2, 3, 4, 5, 6, 7, 8)).IsEqualTo(0);
        await Assert.That(subject.Greet("x")).IsEqualTo("");
        await Assert.That(await subject.GetAsync()).IsEqualTo(0);
        await Assert.That(subject.Name).IsEqualTo("");
    }

    [Test]
    public async Task PassThrough_False_Does_Not_Run_Base_Void_Method()
    {
        var mock = PassThroughSubject.Mock();
        Mock.SetPassThrough(mock, false);

        mock.Object.Run();

        await Assert.That(mock.Object.BaseRunCount).IsEqualTo(0);
        mock.Run().WasCalled(Times.Once);
    }

    [Test]
    public async Task PassThrough_True_Runs_Base_Void_Method()
    {
        var mock = PassThroughSubject.Mock();

        mock.Object.Run();

        await Assert.That(mock.Object.BaseRunCount).IsEqualTo(1);
    }

    [Test]
    public async Task PassThrough_False_Configured_Setups_Still_Win()
    {
        var mock = PassThroughSubject.Mock();
        Mock.SetPassThrough(mock, false);
        mock.Two(1, 2).Returns(99);
        mock.One(Any()).Returns(5);

        await Assert.That(mock.Object.Two(1, 2)).IsEqualTo(99);
        await Assert.That(mock.Object.Two(3, 4)).IsEqualTo(0);
        await Assert.That(mock.Object.One(10)).IsEqualTo(5);
    }

    [Test]
    public async Task PassThrough_False_Calls_Are_Still_Recorded_And_Verifiable()
    {
        var mock = PassThroughSubject.Mock();
        Mock.SetPassThrough(mock, false);

        mock.Object.Greet("Alice");

        mock.Greet("Alice").WasCalled(Times.Once);
        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(1);
    }

    [Test]
    public async Task PassThrough_Can_Be_Toggled_Back_To_True()
    {
        var mock = PassThroughSubject.Mock();
        Mock.SetPassThrough(mock, false);
        await Assert.That(mock.Object.Two(2, 3)).IsEqualTo(0);

        Mock.SetPassThrough(mock, true);

        await Assert.That(mock.Object.Two(2, 3)).IsEqualTo(5);
    }

    [Test]
    public async Task PassThrough_False_Strict_Throws_For_Unconfigured_Virtual_Members()
    {
        var mock = PassThroughSubject.Mock(MockBehavior.Strict);
        Mock.SetPassThrough(mock, false);

        Assert.Throws<MockStrictBehaviorException>(() => mock.Object.Two(1, 2));
        Assert.Throws<MockStrictBehaviorException>(() => mock.Object.Zero());
        Assert.Throws<MockStrictBehaviorException>(() => mock.Object.Run());
        await Assert.That(Mock.GetPassThrough(mock)).IsFalse();
    }

    [Test]
    public async Task PassThrough_False_Strict_Allows_Configured_Members()
    {
        var mock = PassThroughSubject.Mock(MockBehavior.Strict);
        Mock.SetPassThrough(mock, false);
        mock.Two(1, 2).Returns(3);

        await Assert.That(mock.Object.Two(1, 2)).IsEqualTo(3);
    }

    [Test]
    public async Task PassThrough_False_Loose_AutoMocks_Interface_Returns()
    {
        var mock = PassThroughSubject.Mock();
        Mock.SetPassThrough(mock, false);

        var dependency = mock.Object.GetDependency();

        await Assert.That(dependency).IsNotNull();
        Mock.Get(dependency).GetValue().Returns(42);
        await Assert.That(dependency.GetValue()).IsEqualTo(42);
    }

    [Test]
    public async Task PassThrough_False_With_AutoTracked_Properties_Stores_Values()
    {
        var mock = PassThroughSubject.Mock();
        Mock.SetPassThrough(mock, false);
        Mock.SetupAllProperties(mock);

        mock.Object.Name = "Alice";

        await Assert.That(mock.Object.Name).IsEqualTo("Alice");
    }

    [Test]
    public async Task PassThrough_False_Uses_Custom_DefaultValueProvider()
    {
        var mock = PassThroughSubject.Mock();
        Mock.SetPassThrough(mock, false);
        Mock.SetDefaultValueProvider(mock, new FixedIntProvider(-1));

        await Assert.That(mock.Object.Two(1, 2)).IsEqualTo(-1);
    }

    [Test]
    public async Task PassThrough_Static_Helpers_Round_Trip()
    {
        var mock = PassThroughSubject.Mock();

        Mock.SetPassThrough(mock, false);

        await Assert.That(Mock.GetPassThrough(mock)).IsFalse();
        await Assert.That(mock.Object.Two(2, 3)).IsEqualTo(0);
    }

    [Test]
    public async Task PassThrough_False_Wrap_Mock_Does_Not_Delegate_To_Instance()
    {
        var real = new PassThroughSubject();
        var mock = Mock.Wrap(real);
        await Assert.That(mock.Object.Two(2, 3)).IsEqualTo(5);

        Mock.SetPassThrough(mock, false);

        await Assert.That(mock.Object.Two(2, 3)).IsEqualTo(0);
    }

    [Test]
    public async Task PassThrough_False_Does_Not_Auto_Track_Indexer_Setters()
    {
        var mock = PassThroughSubject.Mock();
        Mock.SetPassThrough(mock, false);
        Mock.SetupAllProperties(mock);
        var subject = mock.Object;

        subject[5] = "value";

        await Assert.That(subject[5]).IsEqualTo("");
    }

    [Test]
    public async Task PassThrough_False_Strict_DefaultValueProvider_Does_Not_Hide_Unexpected_Calls()
    {
        var mock = PassThroughSubject.Mock(MockBehavior.Strict);
        Mock.SetPassThrough(mock, false);
        Mock.SetDefaultValueProvider(mock, new FixedIntProvider(42));

        await Assert.That(() => mock.Object.Zero()).Throws<MockStrictBehaviorException>();
        await Assert.That(() => mock.Object.Two(1, 2)).Throws<MockStrictBehaviorException>();
    }

    [Test]
    public async Task IsOfType_Throws_For_Unrelated_Type_At_Setup()
    {
        await Assert.That(() => { Arg<int> _ = Arg.IsOfType<string>(); }).Throws<ArgumentException>();
    }

    [Test]
    public async Task IsOfType_Error_Names_Both_Types()
    {
        var ex = Assert.Throws<ArgumentException>(() => { Arg<int> _ = Arg.IsOfType<string>(); });

        await Assert.That(ex.Message).Contains("String").And.Contains("Int32");
    }

    [Test]
    public async Task PassThrough_False_Strict_Wrap_Mock_Throws_For_Unconfigured_Calls()
    {
        var mock = Mock.Wrap(MockBehavior.Strict, new PassThroughSubject());
        Mock.SetPassThrough(mock, false);
        mock.Two(1, 2).Returns(99);

        await Assert.That(mock.Object.Two(1, 2)).IsEqualTo(99);
        await Assert.That(() => mock.Object.Zero()).Throws<MockStrictBehaviorException>();
        await Assert.That(() => mock.Object.Three(1, 2, 3)).Throws<MockStrictBehaviorException>();
    }

    private sealed class FixedIntProvider(int value) : IDefaultValueProvider
    {
        public bool CanProvide(Type type) => type == typeof(int);

        public object? GetDefaultValue(Type type) => value;
    }
}
