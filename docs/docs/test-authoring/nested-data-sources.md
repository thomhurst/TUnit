---
sidebar_position: 7
---

# Nested Data Sources with Initialization

When writing integration tests, you often need complex test fixtures that depend on other initialized resources. TUnit's nested data source initialization feature makes this elegant and automatic.

## The Problem

Traditional integration test setup often requires:
- Starting test containers (databases, message queues, etc.)
- Initializing WebApplicationFactory with custom services
- Ensuring proper initialization order
- Managing resource lifecycle

This typically leads to complex setup code with manual initialization chains.

## The Solution

TUnit automatically initializes nested data sources in the correct order using any data source attribute that implements `IDataSourceAttribute` (such as `[ClassDataSource<T>]`).

## Basic Example

Here's a complete example of setting up integration tests with Redis and WebApplicationFactory:

```csharp
using Testcontainers.Redis;
using TUnit.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using StackExchange.Redis;

// 1. Define a test container that needs initialization
public class RedisTestContainer : IAsyncInitializer, IAsyncDisposable
{
    private readonly RedisContainer _container;
    
    public string ConnectionString => _container.GetConnectionString();
    
    public RedisTestContainer()
    {
        _container = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }
    
    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

// 2. Create a test application that depends on Redis
public class TestApplication : IAsyncInitializer, IAsyncDisposable
{
    private WebApplicationFactory<Program>? _factory;
    
    // This property will be initialized BEFORE InitializeAsync is called
    [ClassDataSource<RedisTestContainer>]
    public required RedisTestContainer Redis { get; init; }
    
    public HttpClient Client { get; private set; } = null!;
    
    public async Task InitializeAsync()
    {
        // At this point, Redis is already started and ready!
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace production Redis with our test container
                    services.AddSingleton<IConnectionMultiplexer>(_ => 
                        ConnectionMultiplexer.Connect(Redis.ConnectionString));
                });
            });
        
        Client = _factory.CreateClient();
    }
    
    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();
        if (_factory != null) await _factory.DisposeAsync();
    }
}

// 3. Create a data source attribute
public class TestApplicationAttribute : DataSourceGeneratorAttribute<TestApplication>
{
    public override IEnumerable<TestApplication> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return new TestApplication();
    }
}

// 4. Use in tests
public class UserApiTests
{
    [Test]
    [TestApplication]
    public async Task CreateUser_Should_Cache_In_Redis(TestApplication app)
    {
        // Arrange
        var user = new { Name = "John", Email = "john@example.com" };
        
        // Act
        var response = await app.Client.PostAsJsonAsync("/api/users", user);
        
        // Assert
        response.EnsureSuccessStatusCode();
        
        // Verify the user was cached in Redis
        var services = app.Client.Services;
        var redis = services.GetRequiredService<IConnectionMultiplexer>();
        var cached = await redis.GetDatabase().StringGetAsync("user:john@example.com");
        
        Assert.That(cached.HasValue).IsTrue();
    }
}
```

## Multiple Dependencies

You can have multiple nested dependencies, and TUnit will initialize them in the correct order:

```csharp
public class CompleteTestEnvironment : IAsyncInitializer, IAsyncDisposable
{
    private WebApplicationFactory<Program>? _factory;
    
    // All of these will be initialized before InitializeAsync
    [ClassDataSource<RedisTestContainer>]
    public required RedisTestContainer Redis { get; init; }
    
    [ClassDataSource<PostgresTestContainer>]
    public required PostgresTestContainer Database { get; init; }
    
    [ClassDataSource<LocalStackContainer>]
    public required LocalStackContainer LocalStack { get; init; }
    
    public HttpClient Client { get; private set; } = null!;
    
    public async Task InitializeAsync()
    {
        // All containers are running at this point
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Wire up all test services
                    ConfigureRedis(services);
                    ConfigureDatabase(services);
                    ConfigureAwsServices(services);
                });
            });
        
        Client = _factory.CreateClient();
        
        // Run any post-initialization setup
        await SeedTestData();
    }
    
    // ... configuration methods
}
```

## Sharing Resources

Expensive resources like test containers should be shared across tests:

```csharp
// Share the same instance across all tests in a class
[SharedType(SharedType.PerClass)]
public class SharedTestApplicationAttribute : TestApplicationAttribute
{
}

// Or share with a specific key for fine-grained control
[SharedType(SharedType.Keyed, "integration-tests")]
public class KeyedTestApplicationAttribute : TestApplicationAttribute
{
}

// Usage
[TestClass]
public class OrderApiTests
{
    [Test]
    [SharedTestApplication] // Reuses the same instance for all tests in this class
    public async Task Test1(TestApplication app) { /* ... */ }
    
    [Test]
    [SharedTestApplication] // Same instance as Test1
    public async Task Test2(TestApplication app) { /* ... */ }
}
```

## Async Data Generation with Dependencies

You can also use async data source generators that depend on initialized resources:

```csharp
public class UserTestDataAttribute : AsyncDataSourceGeneratorAttribute<UserTestData>
{
    // This will be initialized first
    [ClassDataSource<TestApplication>]
    public required TestApplication App { get; init; }
    
    public override async IAsyncEnumerable<UserTestData> GenerateDataSourcesAsync(
        DataGeneratorMetadata metadata)
    {
        // App is fully initialized here, including database
        var dbContext = App.Services.GetRequiredService<AppDbContext>();
        
        // Create test users
        var adminUser = new User { Email = "admin@test.com", Role = "Admin" };
        var regularUser = new User { Email = "user@test.com", Role = "User" };
        
        dbContext.Users.AddRange(adminUser, regularUser);
        await dbContext.SaveChangesAsync();
        
        yield return new UserTestData 
        { 
            User = adminUser, 
            App = App,
            ExpectedPermissions = new[] { "read", "write", "delete" }
        };
        
        yield return new UserTestData 
        { 
            User = regularUser, 
            App = App,
            ExpectedPermissions = new[] { "read" }
        };
    }
}
```

## How It Works

1. TUnit detects properties marked with data source attributes (like `[ClassDataSource<T>]`)
2. It builds a dependency graph and initializes in the correct order
3. Each object's `InitializeAsync` is called after its dependencies are ready
4. Disposal happens in reverse order automatically

## Best Practices

1. **Implement IAsyncInitializer**: For any class that needs async initialization
2. **Use Data Source Attributes**: Use attributes like `[ClassDataSource<T>]` to declare dependencies that must be initialized first
3. **Share Expensive Resources**: Use `SharedType` attributes to avoid creating multiple containers
4. **Dispose Properly**: Implement `IAsyncDisposable` for cleanup
5. **Keep Initialization Fast**: Do only essential setup in `InitializeAsync`

## Common Patterns

### Database Migrations
```csharp
public async Task InitializeAsync()
{
    await _container.StartAsync();
    
    // Run migrations after container starts
    using var connection = new NpgsqlConnection(ConnectionString);
    await connection.ExecuteAsync(@"
        CREATE TABLE IF NOT EXISTS users (
            id SERIAL PRIMARY KEY,
            email VARCHAR(255) UNIQUE NOT NULL
        )");
}
```

### Seeding Test Data
```csharp
public async Task InitializeAsync()
{
    // ... create WebApplicationFactory
    
    // Seed data after app starts
    using var scope = _factory.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<ITestDataSeeder>();
    await seeder.SeedAsync();
}
```

### Health Checks
```csharp
public async Task InitializeAsync()
{
    // ... create client
    
    // Wait for app to be healthy
    var healthCheck = await Client.GetAsync("/health");
    healthCheck.EnsureSuccessStatusCode();
}
```

## Summary

Nested data source initialization in TUnit:
- ✅ Eliminates manual initialization chains
- ✅ Ensures correct initialization order
- ✅ Supports complex dependency graphs
- ✅ Works seamlessly with async operations
- ✅ Provides automatic cleanup

This makes integration testing with complex dependencies simple and maintainable.