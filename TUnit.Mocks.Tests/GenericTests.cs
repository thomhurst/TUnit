using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Interfaces with generic methods for testing generic method support in mock generation.
/// </summary>
public interface IRepository
{
    T Get<T>(int id) where T : class;
    void Save<T>(T item) where T : class;
    TResult Transform<TInput, TResult>(TInput input) where TInput : class where TResult : class;
}

/// <summary>
/// Generic method whose type parameter does not appear in the parameter list, so calls can only be
/// distinguished by their type argument. Mirrors discussion #4981.
/// </summary>
public interface IGenericGreeter
{
    string Greet<T>() where T : class;
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class Order
{
    public int OrderId { get; set; }
}

public class Class1 { }
public class Class2 { }

/// <summary>
/// US7 Integration Tests: Generic method support in mock generation.
/// </summary>
public class GenericTests
{
    [Test]
    public async Task Generic_Method_Returns_Configured_Value()
    {
        // Arrange
        var mock = IRepository.Mock();
        var customer = new Customer { Id = 1, Name = "Alice" };
        mock.Get<Customer>(1).Returns(customer);

        // Act
        IRepository repo = mock.Object;
        var result = repo.Get<Customer>(1);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Name).IsEqualTo("Alice");
    }

    [Test]
    public async Task Generic_Method_Unconfigured_Returns_Default()
    {
        // Arrange
        var mock = IRepository.Mock();

        // Act
        IRepository repo = mock.Object;
        var result = repo.Get<Customer>(99);

        // Assert - default for reference type is null (via default!)
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Generic_Method_Different_Type_Arguments()
    {
        // Arrange
        var mock = IRepository.Mock();
        var customer = new Customer { Id = 1, Name = "Alice" };
        var order = new Order { OrderId = 42 };

        mock.Get<Customer>(1).Returns(customer);
        mock.Get<Order>(2).Returns(order);

        // Act
        IRepository repo = mock.Object;
        var c = repo.Get<Customer>(1);
        var o = repo.Get<Order>(2);

        // Assert
        await Assert.That(c).IsNotNull();
        await Assert.That(c.Name).IsEqualTo("Alice");
        await Assert.That(o).IsNotNull();
        await Assert.That(o.OrderId).IsEqualTo(42);
    }

    [Test]
    public void Generic_Void_Method_Does_Not_Throw()
    {
        // Arrange
        var mock = IRepository.Mock();
        var customer = new Customer { Id = 1, Name = "Bob" };

        // Act & Assert - should not throw in loose mode
        IRepository repo = mock.Object;
        repo.Save(customer);
    }

    [Test]
    public async Task Generic_Void_Method_Verify()
    {
        // Arrange
        var mock = IRepository.Mock();
        var customer = new Customer { Id = 1, Name = "Charlie" };

        // Act
        IRepository repo = mock.Object;
        repo.Save(customer);

        // Assert
        mock.Save<Customer>(Any()).WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Generic_Method_With_Any_Matcher()
    {
        // Arrange
        var mock = IRepository.Mock();
        var customer = new Customer { Id = 1, Name = "Any" };
        mock.Get<Customer>(Any()).Returns(customer);

        // Act
        IRepository repo = mock.Object;
        var r1 = repo.Get<Customer>(1);
        var r2 = repo.Get<Customer>(999);

        // Assert
        await Assert.That(r1).IsSameReferenceAs(customer);
        await Assert.That(r2).IsSameReferenceAs(customer);
    }

    [Test]
    public async Task Generic_Method_With_Two_Type_Parameters()
    {
        // Arrange
        var mock = IRepository.Mock();
        var order = new Order { OrderId = 10 };
        mock.Transform<Customer, Order>(Any()).Returns(order);

        // Act
        IRepository repo = mock.Object;
        var result = repo.Transform<Customer, Order>(new Customer { Id = 1, Name = "Test" });

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.OrderId).IsEqualTo(10);
    }

    [Test]
    public async Task Generic_Method_ZeroArgs_Distinguished_By_Type_Argument()
    {
        // Regression for discussion #4981: with no parameters, only the type argument distinguishes
        // the two setups. Before the fix both setups collided and the last one always won.
        var mock = IGenericGreeter.Mock();
        mock.Greet<Class1>().Returns("Hello!");
        mock.Greet<Class2>().Returns("Goodbye!");

        IGenericGreeter greeter = mock.Object;

        await Assert.That(greeter.Greet<Class1>()).IsEqualTo("Hello!");
        await Assert.That(greeter.Greet<Class2>()).IsEqualTo("Goodbye!");
    }

    [Test]
    public async Task Generic_Method_Wildcard_AnyType_Matches_Any_Type_Argument()
    {
        var mock = IGenericGreeter.Mock();
        mock.Greet<AnyType>().Returns("any");

        IGenericGreeter greeter = mock.Object;

        await Assert.That(greeter.Greet<Class1>()).IsEqualTo("any");
        await Assert.That(greeter.Greet<Class2>()).IsEqualTo("any");
    }

    [Test]
    public async Task Generic_Method_Exact_Type_Setup_Wins_Over_Wildcard()
    {
        // A later, more specific setup is matched first (last-wins iteration), while other type
        // arguments still fall through to the wildcard.
        var mock = IGenericGreeter.Mock();
        mock.Greet<AnyType>().Returns("any");
        mock.Greet<Class1>().Returns("specific");

        IGenericGreeter greeter = mock.Object;

        await Assert.That(greeter.Greet<Class1>()).IsEqualTo("specific");
        await Assert.That(greeter.Greet<Class2>()).IsEqualTo("any");
    }

    [Test]
    public void Generic_Void_Method_Verify_Discriminates_By_Type_Argument()
    {
        var mock = IRepository.Mock();
        IRepository repo = mock.Object;

        repo.Save(new Customer());
        repo.Save(new Customer());
        repo.Save(new Order());

        mock.Save<Customer>(Any()).WasCalled(Times.Exactly(2));
        mock.Save<Order>(Any()).WasCalled(Times.Once);
        mock.Save<AnyType>(Any()).WasCalled(Times.Exactly(3));
    }
}
