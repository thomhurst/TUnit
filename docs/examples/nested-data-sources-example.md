# Nested Data Sources with Complex Initialization

This example demonstrates how to use TUnit's nested data source initialization features to create integration tests with complex dependencies like test containers and WebApplicationFactory.

## Overview

When writing integration tests, you often need to:
1. Spin up test containers (Redis, SQL Server, etc.)
2. Create a WebApplicationFactory with custom services
3. Ensure proper initialization order
4. Share expensive resources across tests
5. Clean up resources properly

TUnit's data source initialization features make this elegant and efficient.

## Complete Example

### 1. Define Test Infrastructure Components

```csharp
using Testcontainers.MsSql;
using Testcontainers.Redis;
using TUnit.Core;

// Base class for test containers that need initialization
public abstract class TestContainer : IAsyncInitializer, IAsyncDisposable
{
    public abstract Task InitializeAsync();
    public abstract ValueTask DisposeAsync();
}

// Redis test container wrapper
public class RedisTestContainer : TestContainer
{
    private readonly RedisContainer _container;
    
    public string ConnectionString => _container.GetConnectionString();
    
    public RedisTestContainer()
    {
        _container = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }
    
    public override async Task InitializeAsync()
    {
        await _container.StartAsync();
    }
    
    public override async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

// SQL Server test container wrapper
public class SqlServerTestContainer : TestContainer
{
    private readonly MsSqlContainer _container;
    
    public string ConnectionString => _container.GetConnectionString();
    
    public SqlServerTestContainer()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd")
            .Build();
    }
    
    public override async Task InitializeAsync()
    {
        await _container.StartAsync();
        
        // Run migrations or seed data
        await InitializeDatabase();
    }
    
    private async Task InitializeDatabase()
    {
        // Your database initialization logic here
    }
    
    public override async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
```

### 2. Create the WebApplicationFactory Data Source

```csharp
public class TestApplication : IAsyncInitializer, IAsyncDisposable
{
    private WebApplicationFactory<Program>? _factory;
    
    // These properties will be initialized by TUnit before InitializeAsync is called
    [ClassDataSource<RedisTestContainer>]
    public required RedisTestContainer Redis { get; init; }
    
    [ClassDataSource<SqlServerTestContainer>]
    public required SqlServerTestContainer SqlServer { get; init; }
    
    public HttpClient Client { get; private set; } = null!;
    public IServiceProvider Services => _factory!.Services;
    
    public async Task InitializeAsync()
    {
        // At this point, Redis and SqlServer are already initialized
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing service registrations
                    RemoveDefaultServices(services);
                    
                    // Add test implementations using our test containers
                    services.AddSingleton<IConnectionMultiplexer>(_ => 
                        ConnectionMultiplexer.Connect(Redis.ConnectionString));
                    
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(SqlServer.ConnectionString));
                    
                    // Add test-specific services
                    services.AddSingleton<IEmailService, FakeEmailService>();
                    services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
                });
                
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Override configuration with test values
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["ConnectionStrings:Redis"] = Redis.ConnectionString,
                        ["ConnectionStrings:SqlServer"] = SqlServer.ConnectionString,
                        ["Features:EmailEnabled"] = "false",
                        ["Features:PaymentSandbox"] = "true"
                    });
                });
            });
        
        // Create the client after factory is configured
        Client = _factory.CreateClient();
        
        // Optionally warm up the application
        await WarmUpApplication();
    }
    
    private void RemoveDefaultServices(IServiceCollection services)
    {
        // Remove production Redis
        var redisDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(IConnectionMultiplexer));
        if (redisDescriptor != null)
            services.Remove(redisDescriptor);
        
        // Remove production DbContext
        var dbContextDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(ApplicationDbContext));
        if (dbContextDescriptor != null)
            services.Remove(dbContextDescriptor);
    }
    
    private async Task WarmUpApplication()
    {
        // Make a request to ensure the app is fully started
        await Client.GetAsync("/health");
    }
    
    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();
        await (_factory?.DisposeAsync() ?? ValueTask.CompletedTask);
    }
}
```

### 3. Create a Data Source Attribute

```csharp
// This attribute will provide a fully initialized TestApplication to tests
public class TestApplicationAttribute : DataSourceGeneratorAttribute<TestApplication>
{
    public override IEnumerable<TestApplication> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        // Return a single instance that will be initialized by TUnit
        yield return new TestApplication();
    }
}

### 4. Write Integration Tests

```csharp
[TestClass]
public class UserApiIntegrationTests
{
    [Test]
    [TestApplication]
    public async Task CreateUser_Should_Store_In_Database(TestApplication app)
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Name = "Test User"
        };
        
        // Act
        var response = await app.Client.PostAsJsonAsync("/api/users", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // Verify in database
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        
        user.Should().NotBeNull();
        user!.Name.Should().Be(request.Name);
    }
    
    [Test]
    [TestApplication]
    public async Task CreateUser_Should_Cache_In_Redis(TestApplication app)
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "cached@example.com",
            Name = "Cached User"
        };
        
        // Act
        var response = await app.Client.PostAsJsonAsync("/api/users", request);
        var location = response.Headers.Location?.ToString();
        
        // Assert - Check Redis cache
        var redis = app.Services.GetRequiredService<IConnectionMultiplexer>();
        var db = redis.GetDatabase();
        var cachedUser = await db.StringGetAsync($"user:{location}");
        
        cachedUser.HasValue.Should().BeTrue();
    }
}

### 5. Advanced Scenario: Custom Test Data with Initialized Context

```csharp
// Data source that provides test scenarios with pre-populated data
public class OrderTestScenarioAttribute : AsyncDataSourceGeneratorAttribute<OrderTestScenario>
{
    [ClassDataSource<TestApplication>]
    public required TestApplication App { get; init; }
    
    public override async IAsyncEnumerable<OrderTestScenario> GenerateDataSourcesAsync(DataGeneratorMetadata metadata)
    {
        // App is already initialized here with Redis and SQL Server running
        
        // Seed test data
        var customerId = await CreateTestCustomer();
        var productIds = await CreateTestProducts();
        
        yield return new OrderTestScenario
        {
            Name = "Valid order with single item",
            App = App,
            CustomerId = customerId,
            OrderItems = new[]
            {
                new OrderItem { ProductId = productIds[0], Quantity = 1 }
            },
            ExpectedTotal = 29.99m
        };
        
        yield return new OrderTestScenario
        {
            Name = "Valid order with multiple items",
            App = App,
            CustomerId = customerId,
            OrderItems = new[]
            {
                new OrderItem { ProductId = productIds[0], Quantity = 2 },
                new OrderItem { ProductId = productIds[1], Quantity = 1 }
            },
            ExpectedTotal = 89.97m
        };
        
        yield return new OrderTestScenario
        {
            Name = "Order exceeding stock",
            App = App,
            CustomerId = customerId,
            OrderItems = new[]
            {
                new OrderItem { ProductId = productIds[0], Quantity = 1000 }
            },
            ExpectedException = typeof(InsufficientStockException)
        };
    }
    
    private async Task<Guid> CreateTestCustomer()
    {
        using var scope = App.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Test Customer",
            Email = "customer@test.com"
        };
        
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        
        return customer.Id;
    }
    
    private async Task<Guid[]> CreateTestProducts()
    {
        using var scope = App.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var products = new[]
        {
            new Product { Id = Guid.NewGuid(), Name = "Widget", Price = 29.99m, Stock = 100 },
            new Product { Id = Guid.NewGuid(), Name = "Gadget", Price = 59.99m, Stock = 50 }
        };
        
        dbContext.Products.AddRange(products);
        await dbContext.SaveChangesAsync();
        
        return products.Select(p => p.Id).ToArray();
    }
}

public class OrderTestScenario
{
    public required string Name { get; init; }
    public required TestApplication App { get; init; }
    public required Guid CustomerId { get; init; }
    public required OrderItem[] OrderItems { get; init; }
    public decimal? ExpectedTotal { get; init; }
    public Type? ExpectedException { get; init; }
}

// Use the test scenarios
[TestClass]
public class OrderProcessingTests
{
    [Test]
    [OrderTestScenario]
    public async Task ProcessOrder_Scenarios(OrderTestScenario scenario)
    {
        // Arrange
        var orderRequest = new CreateOrderRequest
        {
            CustomerId = scenario.CustomerId,
            Items = scenario.OrderItems
        };
        
        // Act
        var response = await scenario.App.Client.PostAsJsonAsync("/api/orders", orderRequest);
        
        // Assert
        if (scenario.ExpectedException != null)
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
            order!.Total.Should().Be(scenario.ExpectedTotal);
        }
    }
}
```

## Key Benefits

1. **Automatic Initialization Order**: TUnit ensures that nested dependencies (Redis, SQL Server) are initialized before the WebApplicationFactory.

2. **Resource Sharing**: Expensive resources like test containers can be shared across tests using `SharedType` attributes.

3. **Clean Async Code**: No need for `GetAwaiter().GetResult()` or other synchronous workarounds.

4. **Proper Cleanup**: Resources are disposed in reverse initialization order automatically.

5. **Test Isolation**: Each test can have its own isolated instance or share instances as needed.

## Best Practices

1. **Use IAsyncInitializer**: Implement this interface for any class that needs async initialization.

2. **Declare Dependencies**: Use data source attributes like `[ClassDataSource<T>]` to declare dependencies that need to be initialized first.

3. **Share Expensive Resources**: Use `SharedType.PerClass` or `SharedType.Keyed` for resources that are expensive to create.

4. **Dispose Properly**: Implement `IAsyncDisposable` to ensure resources are cleaned up.

5. **Seed Test Data**: Use async data source generators to create test scenarios with pre-populated data.

## Performance Considerations

- Test containers take time to start. Share them across tests when possible.
- Use `SharedType.PerAssembly` for truly global resources.
- Consider parallel test execution impact on shared resources.
- Use keyed sharing to create multiple isolated groups of tests.