namespace TUnit.UnitTests;

/// <summary>
/// Example showing how DependsOn works with inheritance
/// </summary>
public abstract class DatabaseTestBase
{
    // Static dictionary to simulate a "database" that persists across test instances
    protected static readonly Dictionary<string, object> Database = new();

    [Test]
    public async Task InitializeDatabase()
    {
        // Simulating database initialization
        Database["initialized"] = true;
        Database["connectionString"] = "Server=test;Database=test;";
        await Task.Delay(1); // Simulate async work
        await Assert.That(Database.ContainsKey("initialized")).IsTrue();
    }

    [Test]
    [DependsOn(nameof(InitializeDatabase))]
    public async Task CreateSchema()
    {
        // This depends on InitializeDatabase
        await Assert.That(Database.ContainsKey("initialized")).IsTrue();
        await Assert.That(Database["connectionString"]).IsNotNull();
        
        // Simulate schema creation
        Database["schemaCreated"] = true;
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
        await Assert.That(Database.ContainsKey("schemaCreated")).IsTrue();
        await Assert.That(Database["connectionString"]).IsNotNull();
        
        // Simulate user creation
        Database["userCreated"] = true;
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
        await Assert.That(Database.ContainsKey("initialized")).IsTrue();
        await Assert.That(Database["connectionString"]).IsNotNull();
        
        // Simulate product creation
        Database["productCreated"] = true;
        await Task.Delay(1);
    }
}

/// <summary>
/// Example with generic base classes
/// </summary>
public abstract class RepositoryTestBase<T> where T : class, new()
{
    // Static dictionary to store entities by type
    protected static readonly Dictionary<Type, object> Entities = new();

    [Test]
    public async Task InitializeEntity()
    {
        var entity = new T();
        Entities[typeof(T)] = entity;
        await Assert.That(Entities.ContainsKey(typeof(T))).IsTrue();
        await Task.Delay(1); // Simulate async work
    }
}

[InheritsTests]
public class CustomerRepositoryTests : RepositoryTestBase<Customer>
{
    [Test]
    [DependsOn(nameof(InitializeEntity))] // Depends on base test
    public async Task CanSetCustomerName()
    {
        await Assert.That(Entities.ContainsKey(typeof(Customer))).IsTrue();
        var entity = (Customer)Entities[typeof(Customer)];
        await Assert.That(entity).IsNotNull();
        
        entity.Name = "Test Customer";
        await Assert.That(entity.Name).IsEqualTo("Test Customer");
    }
}

public class Customer
{
    public string? Name { get; set; }
}