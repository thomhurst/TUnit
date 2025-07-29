namespace TUnit.UnitTests;

/// <summary>
/// Example showing how DependsOn works with inheritance
/// </summary>
public abstract class DatabaseTestBase
{
    protected string? ConnectionString { get; set; }

    [Test]
    public async Task InitializeDatabase()
    {
        // Simulating database initialization
        ConnectionString = "Server=test;Database=test;";
        await Task.Delay(1); // Simulate async work
        await Assert.That(ConnectionString).IsNotNull();
    }

    [Test]
    [DependsOn(nameof(InitializeDatabase))]
    public async Task CreateSchema()
    {
        // This depends on InitializeDatabase
        await Assert.That(ConnectionString).IsNotNull();
        // Simulate schema creation
        await Task.Delay(1);
    }
}

[InheritsTests]
public class UserRepositoryTests : DatabaseTestBase
{
    [Test]
    [DependsOn(nameof(CreateSchema))] // Depends on inherited test
    public async Task CanCreateUser()
    {
        // This test depends on the schema being created
        await Assert.That(ConnectionString).IsNotNull();
        // Simulate user creation
        await Task.Delay(1);
    }

}

[InheritsTests]
public class ProductRepositoryTests : DatabaseTestBase
{
    [Test]
    [DependsOn(nameof(InitializeDatabase))] // Can depend on specific base test
    public async Task CanCreateProduct()
    {
        // Only depends on database initialization, not schema
        await Assert.That(ConnectionString).IsNotNull();
        // Simulate product creation
        await Task.Delay(1);
    }
}

/// <summary>
/// Example with generic base classes
/// </summary>
public abstract class RepositoryTestBase<T> where T : class, new()
{
    protected T? Entity { get; set; }

    // This would be a test in derived classes, but not in the generic base
    protected async Task InitializeEntity()
    {
        Entity = new T();
        await Assert.That(Entity).IsNotNull();
    }
}

[InheritsTests]
public class CustomerRepositoryTests : RepositoryTestBase<Customer>
{
    [Test]
    public async Task InitializeCustomerEntity()
    {
        await InitializeEntity();
    }

    [Test]
    [DependsOn(nameof(InitializeCustomerEntity))] // Depends on concrete test
    public async Task CanSetCustomerName()
    {
        await Assert.That(Entity).IsNotNull();
        Entity!.Name = "Test Customer";
        await Assert.That(Entity.Name).IsEqualTo("Test Customer");
    }
}

public class Customer
{
    public string? Name { get; set; }
}
