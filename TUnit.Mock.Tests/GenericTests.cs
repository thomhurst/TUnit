using TUnit.Mock;
using TUnit.Mock.Arguments;

namespace TUnit.Mock.Tests;

/// <summary>
/// Interfaces with generic methods for testing generic method support in mock generation.
/// </summary>
public interface IRepository
{
    T Get<T>(int id) where T : class;
    void Save<T>(T item) where T : class;
    TResult Transform<TInput, TResult>(TInput input) where TInput : class where TResult : class;
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

/// <summary>
/// US7 Integration Tests: Generic method support in mock generation.
/// </summary>
public class GenericTests
{
    [Test]
    public async Task Generic_Method_Returns_Configured_Value()
    {
        // Arrange
        var mock = Mock.Of<IRepository>();
        var customer = new Customer { Id = 1, Name = "Alice" };
        mock.Setup.Get<Customer>(1).Returns(customer);

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
        var mock = Mock.Of<IRepository>();

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
        var mock = Mock.Of<IRepository>();
        var customer = new Customer { Id = 1, Name = "Alice" };
        var order = new Order { OrderId = 42 };

        mock.Setup.Get<Customer>(1).Returns(customer);
        mock.Setup.Get<Order>(2).Returns(order);

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
        var mock = Mock.Of<IRepository>();
        var customer = new Customer { Id = 1, Name = "Bob" };

        // Act & Assert - should not throw in loose mode
        IRepository repo = mock.Object;
        repo.Save(customer);
    }

    [Test]
    public async Task Generic_Void_Method_Verify()
    {
        // Arrange
        var mock = Mock.Of<IRepository>();
        var customer = new Customer { Id = 1, Name = "Charlie" };

        // Act
        IRepository repo = mock.Object;
        repo.Save(customer);

        // Assert
        mock.Verify.Save<Customer>(Arg.Any<Customer>()).WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Generic_Method_With_Any_Matcher()
    {
        // Arrange
        var mock = Mock.Of<IRepository>();
        var customer = new Customer { Id = 1, Name = "Any" };
        mock.Setup.Get<Customer>(Arg.Any<int>()).Returns(customer);

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
        var mock = Mock.Of<IRepository>();
        var order = new Order { OrderId = 10 };
        mock.Setup.Transform<Customer, Order>(Arg.Any<Customer>()).Returns(order);

        // Act
        IRepository repo = mock.Object;
        var result = repo.Transform<Customer, Order>(new Customer { Id = 1, Name = "Test" });

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.OrderId).IsEqualTo(10);
    }
}
