# Nested Data Sources - Quick Start

This example shows how to use nested data sources with initialization for integration testing with WebApplicationFactory and test containers.

## Simple Example: WebApp with Redis

```csharp
using Testcontainers.Redis;
using TUnit.Core;

// Step 1: Create a test container wrapper
public class RedisFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly RedisContainer _container = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();
    
    public string ConnectionString => _container.GetConnectionString();
    
    public async Task InitializeAsync() => await _container.StartAsync();
    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

// Step 2: Create WebApplicationFactory that uses the container
public class TestApp : IAsyncInitializer, IAsyncDisposable
{
    private WebApplicationFactory<Program>? _factory;
    
    // This property will be initialized before InitializeAsync is called
    [ClassDataSource<RedisFixture>]
    public required RedisFixture Redis { get; init; }
    
    public HttpClient Client { get; private set; } = null!;
    
    public async Task InitializeAsync()
    {
        // Redis is already initialized and running here!
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace production Redis with test container
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

// Step 3: Create a data source attribute
[SharedType(SharedType.PerClass)] // Share the expensive resources per test class
public class TestAppAttribute : DataSourceGeneratorAttribute<TestApp>
{
    public override IEnumerable<TestApp> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return new TestApp();
    }
}

// Step 4: Use in tests
[TestClass]
public class ApiIntegrationTests
{
    [Test]
    [TestApp]
    public async Task Get_Users_Returns_Cached_Data(TestApp app)
    {
        // Both Redis and WebApp are initialized and ready to use
        var response = await app.Client.GetAsync("/api/users");
        response.EnsureSuccessStatusCode();
    }
}
```

## Multiple Nested Dependencies

```csharp
// Complex app with multiple test containers
public class FullTestApp : IAsyncInitializer, IAsyncDisposable
{
    private WebApplicationFactory<Program>? _factory;
    
    // All these will be initialized before InitializeAsync
    [ClassDataSource<RedisFixture>]
    public required RedisFixture Redis { get; init; }
    
    [ClassDataSource<PostgresFixture>]
    public required PostgresFixture Postgres { get; init; }
    
    [ClassDataSource<RabbitMqFixture>]
    public required RabbitMqFixture RabbitMq { get; init; }
    
    public HttpClient Client { get; private set; } = null!;
    
    public async Task InitializeAsync()
    {
        // All dependencies are initialized in the correct order
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Wire up all test containers
                    services.AddSingleton<IConnectionMultiplexer>(_ => 
                        ConnectionMultiplexer.Connect(Redis.ConnectionString));
                    
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(Postgres.ConnectionString));
                    
                    services.AddMassTransit(x =>
                    {
                        x.UsingRabbitMq((context, cfg) =>
                        {
                            cfg.Host(RabbitMq.ConnectionString);
                        });
                    });
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
```

## Key Points

✅ **Automatic Initialization**: Properties marked with data source attributes (like `[ClassDataSource<T>]`) are initialized before `InitializeAsync` is called

✅ **Proper Order**: TUnit handles the dependency graph and initializes in the correct order

✅ **Resource Sharing**: Use `SharedType` to share expensive resources across tests

✅ **Clean Async**: Everything is async from top to bottom - no blocking calls needed

✅ **Automatic Cleanup**: Resources are disposed in reverse initialization order