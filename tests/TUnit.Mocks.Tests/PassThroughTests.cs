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

public class PassThroughValue
{
    public override bool Equals(object? obj) => ReferenceEquals(this, obj);
    public override int GetHashCode() => 7;
    public override string ToString() => "money";
    public virtual bool Equals(string? other) => other == "base";
    public virtual object Describe() => "base";
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
    public async Task PassThrough_False_Strict_DefaultValueProvider_Behaves_Like_An_Interface_Member()
    {
        var mock = PassThroughSubject.Mock(MockBehavior.Strict);
        Mock.SetPassThrough(mock, false);
        Mock.SetDefaultValueProvider(mock, new FixedIntProvider(42));

        // The provider is explicit opt-in and, as for interface members, is consulted before the strict throw.
        await Assert.That(mock.Object.Zero()).IsEqualTo(42);
        await Assert.That(() => mock.Object.Greet("x")).Throws<MockStrictBehaviorException>();
    }

    [Test]
    public async Task Reset_Keeps_PassThrough_Like_Other_Configuration()
    {
        var mock = PassThroughSubject.Mock();
        Mock.SetPassThrough(mock, false);

        Mock.Reset(mock);

        await Assert.That(Mock.GetPassThrough(mock)).IsFalse();
        await Assert.That(mock.Object.Two(2, 3)).IsEqualTo(0);
    }

    [Test]
    public async Task PassThrough_False_Keeps_Object_Method_Overrides()
    {
        foreach (var behavior in new[] { MockBehavior.Loose, MockBehavior.Strict })
        {
            var mock = PassThroughValue.Mock(behavior);
            Mock.SetPassThrough(mock, false);
            var value = mock.Object;

            await Assert.That(value.Equals(value)).IsTrue();
            await Assert.That(value.GetHashCode()).IsEqualTo(7);
            await Assert.That(value.ToString()).IsEqualTo("money");
        }
    }

    [Test]
    public async Task PassThrough_False_Equals_Overloads_Are_Not_Treated_As_Object_Equals()
    {
        var loose = PassThroughValue.Mock();
        Mock.SetPassThrough(loose, false);
        await Assert.That(loose.Object.Equals("base")).IsFalse();

        var strict = PassThroughValue.Mock(MockBehavior.Strict);
        Mock.SetPassThrough(strict, false);
        await Assert.That(() => strict.Object.Equals("base")).Throws<MockStrictBehaviorException>();
    }

    [Test]
    public async Task PassThrough_False_Uses_DefaultValueProvider_For_Members_Returning_Object()
    {
        var mock = PassThroughValue.Mock();
        Mock.SetPassThrough(mock, false);
        Mock.SetDefaultValueProvider(mock, new ObjectProvider("provided"));

        await Assert.That(mock.Object.Describe()).IsEqualTo("provided");
    }

    [Test]
    public async Task PassThrough_False_Auto_Tracks_Setter_Value_When_A_Setter_Setup_Matches()
    {
        var mock = PassThroughSubject.Mock();
        Mock.SetPassThrough(mock, false);
        Mock.SetupAllProperties(mock);
        var setterRan = false;
        mock.Name.Set(Any()).Callback(() => setterRan = true);

        mock.Object.Name = "x";

        await Assert.That(setterRan).IsTrue();
        await Assert.That(mock.Object.Name).IsEqualTo("x");
    }

    [Test]
    public async Task IsOfType_Throws_For_Unrelated_Type_At_Setup()
    {
        await Assert.That(() => { Arg<int> _ = Arg.IsOfType<string>(); }).Throws<ArgumentException>();
    }

    [Test]
    public async Task IsOfType_Boundary_Cases_Are_Accepted()
    {
        Arg<object> valueTypeToObject = Arg.IsOfType<int>();     // value type boxed into object
        Arg<int?> underlyingToNullable = Arg.IsOfType<int>();    // int is assignable to int?
        Arg<string> sealedImplementingInterface = Arg.IsOfType<string>(); // identical type
        Arg<IComparable> sealedToInterface = Arg.IsOfType<string>();      // sealed class implements the interface
        Arg<MatcherBaseEvent> openClassToInterface = Arg.IsOfType<IDisposable>(); // a subclass could implement it
        Arg<IDisposable> interfaceToInterface = Arg.IsOfType<IComparable>();      // a type could implement both

        await Assert.That(valueTypeToObject.Matcher.Matches(5)).IsTrue();
        await Assert.That(underlyingToNullable.Matcher.Matches(5)).IsTrue();
        await Assert.That(sealedToInterface.Matcher.Matches("x")).IsTrue();
        await Assert.That(sealedImplementingInterface.Matcher.Matches("x")).IsTrue();
        await Assert.That(openClassToInterface.Matcher.Matches(new MatcherBaseEvent())).IsFalse();
        await Assert.That(interfaceToInterface.Matcher.Matches(new object())).IsFalse();
    }

    [Test]
    public async Task IsOfType_Boundary_Cases_Are_Rejected()
    {
        await Assert.That(() => { Arg<long> _ = Arg.IsOfType<int>(); }).Throws<ArgumentException>();            // unrelated value types
        await Assert.That(() => { Arg<IDisposable> _ = Arg.IsOfType<string>(); }).Throws<ArgumentException>();  // sealed class lacking the interface
        await Assert.That(() => { Arg<string> _ = Arg.IsOfType<IDisposable>(); }).Throws<ArgumentException>();  // interface the sealed class lacks
        await Assert.That(() => { Arg<MatcherBaseEvent> _ = Arg.IsOfType<string>(); }).Throws<ArgumentException>(); // unrelated classes
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

    private sealed class ObjectProvider(object value) : IDefaultValueProvider
    {
        public bool CanProvide(Type type) => type == typeof(object);

        public object? GetDefaultValue(Type type) => value;
    }

    private sealed class FixedIntProvider(int value) : IDefaultValueProvider
    {
        public bool CanProvide(Type type) => type == typeof(int);

        public object? GetDefaultValue(Type type) => value;
    }
}
