# Complex Test Infrastructure Orchestration

TUnit provides a property injection system that can help orchestrate complex test infrastructure setups. This page demonstrates how TUnit handles test setups that typically require manual coordination in traditional testing approaches.

## Real-World Example: Full Stack Integration Testing

The `TUnit.Example.Asp.Net.TestProject` showcases how to spin up an entire test environment including Docker networks, Kafka, PostgreSQL, Redis, and even a Kafka UI - all with minimal code and automatic lifecycle management.

## Property Injection Chains

TUnit allows properties to be injected into other properties, creating dependency chains that are resolved and initialized in the correct order.

### Example: Docker Network Orchestration

```csharp
// Step 1: Create a shared Docker network
public class DockerNetwork : IAsyncInitializer, IAsyncDisposable
{
    public INetwork Instance { get; } = new NetworkBuilder()
        .WithName($"tunit-{Guid.NewGuid():N}")
        .Build();

    public async Task InitializeAsync() => await Instance.CreateAsync();
    public async ValueTask DisposeAsync() => await Instance.DisposeAsync();
}
```

### Example: Kafka Container with Network Injection

```csharp
// Step 2: Kafka needs the Docker network
public class InMemoryKafka : IAsyncInitializer, IAsyncDisposable
{
    // This property is automatically injected BEFORE InitializeAsync runs!
    [ClassDataSource<DockerNetwork>(Shared = SharedType.PerTestSession)]
    public required DockerNetwork DockerNetwork { get; init; }

    public KafkaContainer Container => field ??= new KafkaBuilder()
        .WithNetwork(DockerNetwork.Instance)  // Uses the injected network
        .Build();

    public async Task InitializeAsync() => await Container.StartAsync();
    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}
```

### Example: Kafka UI Depending on Kafka Container

```csharp
// Step 3: Kafka UI needs both the network AND the Kafka container
public class KafkaUI : IAsyncInitializer, IAsyncDisposable
{
    // Both dependencies are injected and initialized automatically!
    [ClassDataSource<DockerNetwork>(Shared = SharedType.PerTestSession)]
    public required DockerNetwork DockerNetwork { get; init; }

    [ClassDataSource<InMemoryKafka>(Shared = SharedType.PerTestSession)]
    public required InMemoryKafka Kafka { get; init; }

    public IContainer Container => field ??= new ContainerBuilder()
        .WithNetwork(DockerNetwork.Instance)
        .WithImage("provectuslabs/kafka-ui:latest")
        .WithPortBinding(8080, 8080)
        .WithEnvironment(new Dictionary<string, string>
        {
            // Can reference the Kafka container that was injected!
            ["KAFKA_CLUSTERS_0_NAME"] = "tunit_tests",
            ["KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS"] = $"{Kafka.Container.Name}:9093",
        })
        .Build();

    public async Task InitializeAsync() => await Container.StartAsync();
    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}
```

## Complete Integration: Web Application with Multiple Dependencies

Here's how everything comes together in a WebApplicationFactory that needs multiple infrastructure components:

```csharp
public class WebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    // All these dependencies are automatically initialized in dependency order!
    [ClassDataSource<InMemoryKafka>(Shared = SharedType.PerTestSession)]
    public required InMemoryKafka Kafka { get; init; }

    [ClassDataSource<KafkaUI>(Shared = SharedType.PerTestSession)]
    public required KafkaUI KafkaUI { get; init; }

    [ClassDataSource<InMemoryRedis>(Shared = SharedType.PerTestSession)]
    public required InMemoryRedis Redis { get; init; }

    [ClassDataSource<InMemoryPostgreSqlDatabase>(Shared = SharedType.PerTestSession)]
    public required InMemoryPostgreSqlDatabase PostgreSql { get; init; }

    public Task InitializeAsync()
    {
        _ = Server;  // Force initialization
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            // All containers are already running when this executes!
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Redis:ConnectionString", Redis.Container.GetConnectionString() },
                { "PostgreSql:ConnectionString", PostgreSql.Container.GetConnectionString() },
                { "Kafka:ConnectionString", Kafka.Container.GetBootstrapAddress() },
            });
        });
    }
}
```

## Writing Clean Tests

Your actual test code remains clean and focused:

```csharp
public class Tests : TestsBase
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task Test()
    {
        // Everything is already initialized and running!
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/ping");
        var content = await response.Content.ReadAsStringAsync();

        await Assert.That(content).IsEqualTo("Hello, World!");
    }
}
```

## Key Benefits

### 1. **Automatic Dependency Resolution**
TUnit determines the initialization order:
- Docker Network → Kafka Container → Kafka UI
- Docker Network → PostgreSQL Container
- Docker Network → Redis Container
- All containers → WebApplicationFactory

### 2. **Reduced Boilerplate**
Traditional approaches often require:
- Manual initialization order management
- Complex setup/teardown methods
- Careful coordination of shared resources
- Manual dependency injection wiring

### 3. **Resource Sharing**
Using `SharedType.PerTestSession` helps:
- Expensive resources (containers) are created once
- They're shared across all tests in the session
- Automatic cleanup when tests complete
- No resource leaks or orphaned containers

### 4. **Clean Separation of Concerns**
Each class has a single responsibility:
- `DockerNetwork` - manages the network
- `InMemoryKafka` - manages Kafka container
- `KafkaUI` - manages the UI container
- `WebApplicationFactory` - orchestrates the web app

## Advanced Scenarios

### Database Migrations
```csharp
public class InMemoryPostgreSqlDatabase : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<DockerNetwork>(Shared = SharedType.PerTestSession)]
    public required DockerNetwork DockerNetwork { get; init; }


    public PostgreSqlContainer Container => field ??= new PostgreSqlBuilder()
        .WithUsername("User")
        .WithPassword("Password")
        .WithDatabase("TestDatabase")
        .WithNetwork(DockerNetwork.Instance)
        .Build();

    public async Task InitializeAsync()
    {
        await Container.StartAsync();

        // Run migrations after container starts
        using var connection = new NpgsqlConnection(Container.GetConnectionString());
        await connection.OpenAsync();
        // Run your migration logic here
    }

    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}
```

### EF Core Code First with Per-Test Schema Isolation

For EF Core Code First applications, use per-test PostgreSQL schemas instead of per-test table names. This avoids fighting EF Core's table naming conventions:

```csharp
// DbContext with dynamic schema support
public class TodoDbContext : DbContext
{
    public string SchemaName { get; set; } = "public";
    public DbSet<Todo> Todos => Set<Todo>();

    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        // ... entity configuration
    }
}

// IModelCacheKeyFactory ensures different schemas get different model caches
public class SchemaModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
        => context is TodoDbContext tc
            ? (context.GetType(), tc.SchemaName, designTime)
            : (object)(context.GetType(), designTime);
}

// Test base: creates schema + tables in SetupAsync, drops in cleanup
public abstract class EfCoreTodoTestBase
    : WebApplicationTest<EfCoreWebApplicationFactory, Program>
{
    [ClassDataSource<InMemoryPostgreSqlDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryPostgreSqlDatabase PostgreSql { get; init; } = null!;

    protected string SchemaName { get; private set; } = null!;

    protected override async Task SetupAsync()
    {
        SchemaName = GetIsolatedName("schema"); // e.g. "Test_42_schema"

        // Create schema, then let EF Core create tables
        await using var conn = new NpgsqlConnection(
            PostgreSql.Container.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{SchemaName}\"";
        await cmd.ExecuteNonQueryAsync();

        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseNpgsql(PostgreSql.Container.GetConnectionString())
            .ReplaceService<IModelCacheKeyFactory, SchemaModelCacheKeyFactory>()
            .Options;
        await using var db = new TodoDbContext(options) { SchemaName = SchemaName };
        await db.Database.EnsureCreatedAsync();
    }

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Database:Schema", SchemaName }
        });
    }

    [After(HookType.Test)]
    public async Task CleanupSchema()
    {
        await using var conn = new NpgsqlConnection(
            PostgreSql.Container.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"DROP SCHEMA IF EXISTS \"{SchemaName}\" CASCADE";
        await cmd.ExecuteNonQueryAsync();
    }
}
```

See the full working example in `TUnit.Example.Asp.Net.TestProject/EfCore/`.

## Comparison with Other Frameworks

### Without TUnit (Traditional Approach)
```csharp
public class TestFixture : IAsyncLifetime
{
    private INetwork? _network;
    private KafkaContainer? _kafka;
    private IContainer? _kafkaUi;

    public async Task InitializeAsync()
    {
        // Manual orchestration required
        _network = new NetworkBuilder().Build();
        await _network.CreateAsync();

        _kafka = new KafkaBuilder()
            .WithNetwork(_network)
            .Build();
        await _kafka.StartAsync();

        _kafkaUi = new ContainerBuilder()
            .WithNetwork(_network)
            .WithEnvironment("KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS",
                $"{_kafka.Name}:9093")  // Manual wiring
            .Build();
        await _kafkaUi.StartAsync();
    }

    public async Task DisposeAsync()
    {
        // Manual cleanup in reverse order
        if (_kafkaUi != null) await _kafkaUi.DisposeAsync();
        if (_kafka != null) await _kafka.DisposeAsync();
        if (_network != null) await _network.DisposeAsync();
    }
}
```

### With TUnit
Declare your dependencies with attributes and TUnit manages the orchestration.

## Best Practices

1. **Use `SharedType.PerTestSession`** for expensive resources like containers
2. **Implement `IAsyncInitializer`** for async initialization logic
3. **Implement `IAsyncDisposable`** for proper cleanup
4. **Use `required` properties** to ensure compile-time safety
5. **Keep classes focused** - one responsibility per class
6. **Use TUnit's orchestration** - avoid manual dependency management

## Multiple Test Projects and SharedType.PerTestSession
In larger solutions, it is often beneficial to structure tests into different test projects, sometimes alongside a common test library for shared  common code like infrastructure orchestration. Test runners like `dotnet test`, typically launch separate .NET processes for each test project. And because each test project runs as its own process, they cant share the dependencies.

This means that classes configured with a `SharedType.PerTestSession` lifetime will be **initialized once per test project**, rather than once for the entire test session.

If you intend for services or data to be shared across those separate test projects, you will need to consolidate the execution using a Test Orchestrator approach to load all projects into a single process and run `dotnet test` directly on that. 

## Summary

TUnit's property injection system helps simplify complex test infrastructure setup through a declarative, type-safe approach. By handling initialization order, lifecycle management, and dependency injection, TUnit allows you to focus on writing tests that validate your application's behavior.

The framework manages the orchestration that would otherwise require manual coordination, helping to create cleaner, more maintainable test code with less boilerplate.
