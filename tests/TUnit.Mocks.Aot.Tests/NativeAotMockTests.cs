using TUnit.Mocks.Arguments;
using TUnit.Mocks.Exceptions;

namespace TUnit.Mocks.Aot.Tests;

public interface ICalculator
{
    int Add(int a, int b);
}

public interface IRepository<T>
{
    T? Find(int id);
    void Save(T item);
}

public interface ILookup<TKey, TValue>
{
    TValue Get(TKey key);
}

public interface IClock
{
    DateTimeOffset Now { get; }
}

public interface IService
{
    Task<int> GetCountAsync(CancellationToken cancellationToken);
    ValueTask<string> GetNameAsync();
    string Describe(string value);
    IClock Clock { get; }
    event EventHandler<int>? Changed;
}

public readonly record struct Money(decimal Amount, string Currency);

/// <summary>
/// Everyday TUnit.Mocks usage that must publish under Native AOT without trim/AOT warnings
/// (TreatWarningsAsErrors) and run correctly as a native executable. See issue #7027.
/// </summary>
public class NativeAotMockTests
{
    [Test]
    public async Task Basic_Mock()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(1, 2).Returns(3);

        await Assert.That(mock.Object.Add(1, 2)).IsEqualTo(3);
        mock.Add(1, 2).WasCalled(Times.Once);
    }

    [Test]
    public async Task Generic_Interface_With_Reference_Type_Argument()
    {
        var mock = Mock.Of<IRepository<string>>();
        mock.Find(1).Returns("one");

        await Assert.That(mock.Object.Find(1)).IsEqualTo("one");
    }

    [Test]
    public async Task Generic_Interface_With_Value_Type_Argument()
    {
        var mock = Mock.Of<IRepository<int>>();
        mock.Find(1).Returns(42);

        mock.Object.Save(5);

        await Assert.That(mock.Object.Find(1)).IsEqualTo(42);
        mock.Save(5).WasCalled(Times.Once);
    }

    [Test]
    public async Task Generic_Interface_With_User_Struct_Argument()
    {
        var mock = Mock.Of<IRepository<Money>>();
        mock.Find(1).Returns(new Money(1.5m, "GBP"));

        await Assert.That(mock.Object.Find(1)).IsEqualTo(new Money(1.5m, "GBP"));
    }

    [Test]
    public async Task Generic_Interface_With_Multiple_Value_Type_Arguments()
    {
        var mock = Mock.Of<ILookup<long, Guid>>();
        var id = Guid.NewGuid();
        mock.Get(9).Returns(id);

        await Assert.That(mock.Object.Get(9)).IsEqualTo(id);
    }

    [Test]
    public async Task Generic_Interface_Via_Static_Extension()
    {
        var mock = IRepository<double>.Mock();
        mock.Find(2).Returns(2.5);

        await Assert.That(mock.Object.Find(2)).IsEqualTo(2.5);
    }

    [Test]
    public async Task Async_Members_Properties_And_Auto_Mocks()
    {
        var mock = Mock.Of<IService>();
        mock.GetCountAsync(Any()).Returns(3);
        mock.GetNameAsync().Returns("svc");
        mock.Describe(Any()).Returns(() => "described");

        var service = mock.Object;

        await Assert.That(await service.GetCountAsync(CancellationToken.None)).IsEqualTo(3);
        await Assert.That(await service.GetNameAsync()).IsEqualTo("svc");
        await Assert.That(service.Describe("x")).IsEqualTo("described");
        await Assert.That(service.Clock).IsNotNull();
    }

    [Test]
    public async Task Events_Can_Be_Raised()
    {
        var mock = Mock.Of<IService>();
        var received = 0;
        mock.Object.Changed += (_, value) => received = value;

        mock.RaiseChanged(5);

        await Assert.That(received).IsEqualTo(5);
    }

    [Test]
    public async Task Strict_Mock_Throws_For_Unconfigured_Calls()
    {
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);

        await Assert.That(() => mock.Object.Add(1, 1)).Throws<MockStrictBehaviorException>();
    }

    [Test]
    public async Task Throws_And_Verification_Failures()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(0, 0).Throws<DivideByZeroException>();

        await Assert.That(() => mock.Object.Add(0, 0)).Throws<DivideByZeroException>();
        await Assert.That(() => mock.Add(5, 5).WasCalled(Times.Once)).Throws<MockVerificationException>();
    }

    [Test]
    public async Task Repository_Verifies_All()
    {
        var repository = new MockRepository();
        var calculator = repository.Of<ICalculator>();
        var store = repository.Of<IRepository<int>>();
        calculator.Add(1, 1).Returns(2);
        store.Find(1).Returns(1);

        await Assert.That(calculator.Object.Add(1, 1)).IsEqualTo(2);
        await Assert.That(store.Object.Find(1)).IsEqualTo(1);
    }
}
