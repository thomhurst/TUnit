# ASP.NET Core Integration Testing

TUnit provides first-class support for ASP.NET Core integration testing through the `TUnit.AspNetCore` package. This package enables per-test isolation with shared infrastructure, making it easy to write fast, parallel integration tests.

## Installation

```bash
dotnet add package TUnit.AspNetCore
```

## Quick Start

### 1. Create a Test Factory

Create a factory that extends `TestWebApplicationFactory<TEntryPoint>`:

```csharp
using TUnit.AspNetCore;
using TUnit.Core.Interfaces;

public class WebApplicationFactory : TestWebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Configure shared services and settings
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:Default", "..." }
            });
        });
    }
}
```

### 2. Create a Test Base Class

Create a base class that extends `WebApplicationTest<TFactory, TEntryPoint>`:

```csharp
using TUnit.AspNetCore;

public abstract class TestsBase : WebApplicationTest<WebApplicationFactory, Program>
{
}
```

### 3. Write Tests

```csharp
public class TodoApiTests : TestsBase
{
    [Test]
    public async Task GetTodos_ReturnsOk()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/todos");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
```

## Core Concepts

### Why Test Isolation Matters

:::warning Critical for Parallel Execution
TUnit runs tests in parallel by default. Without proper isolation, tests will interfere with each other, causing flaky failures that are difficult to debug.
:::

When tests share resources like database tables, message queues, or cache keys, you'll encounter problems:

| Shared Resource | What Goes Wrong |
|-----------------|-----------------|
| Database table | Test A inserts a record, Test B's `COUNT(*)` assertion fails |
| Message queue | Test A consumes Test B's messages |
| Cache key | Test A overwrites Test B's cached data |
| Redis key | Test A deletes keys that Test B is using |
| S3 bucket path | Test A's cleanup deletes Test B's files |

**The solution**: Give each test its own isolated resources using `GetIsolatedName()` and `GetIsolatedPrefix()`:

```csharp
protected override async Task SetupAsync()
{
    // Each test gets unique resources that no other test will touch
    var tableName = GetIsolatedName("todos");      // "Test_42_todos"
    var queueName = GetIsolatedName("events");     // "Test_42_events"
    var cachePrefix = GetIsolatedPrefix();         // "test_42_"

    await CreateTableAsync(tableName);
    await CreateQueueAsync(queueName);
}
```

This ensures:
- Tests can run in parallel without interference
- Test failures are deterministic and reproducible
- You can run the same test multiple times (with `[Repeat]`) safely

### WebApplicationTest Pattern

The `WebApplicationTest<TFactory, TEntryPoint>` base class provides:

- **Per-test isolation**: Each test gets its own delegating factory via `WithWebHostBuilder`
- **Shared infrastructure**: The global factory (containers, connections) is shared across tests
- **Parallel execution**: Tests run in parallel with complete isolation
- **Lifecycle hooks**: Async setup runs before sync configuration

### Lifecycle Order

1. `GlobalFactory` is injected (shared per test session)
2. `SetupAsync()` runs (for async operations like creating database tables)
3. `ConfigureTestServices()` runs (sync, for DI configuration)
4. `ConfigureTestConfiguration()` runs (sync, for app configuration)
5. `ConfigureWebHostBuilder()` runs (sync, escape hatch for advanced scenarios)
6. Test runs with isolated `Factory`
7. Factory is disposed

## Override Methods

### SetupAsync

Use for async operations that must complete before the factory is created:

```csharp
public class TodoTests : TestsBase
{
    protected string TableName { get; private set; } = null!;

    protected override async Task SetupAsync()
    {
        TableName = GetIsolatedName("todos");
        await CreateTableAsync(TableName);
    }

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        // TableName is already set from SetupAsync
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Database:TableName", TableName }
        });
    }
}
```

### ConfigureTestServices

Use for DI configuration:

```csharp
protected override void ConfigureTestServices(IServiceCollection services)
{
    // Replace a service with a mock
    services.ReplaceService<IEmailService>(new FakeEmailService());

    // Add test-specific services
    services.AddSingleton<ITestHelper, TestHelper>();
}
```

### ConfigureTestConfiguration

Use for app configuration:

```csharp
protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
{
    config.AddInMemoryCollection(new Dictionary<string, string?>
    {
        { "Feature:Enabled", "true" },
        { "Api:BaseUrl", "https://test.example.com" }
    });
}
```

### ConfigureWebHostBuilder

Escape hatch for advanced scenarios:

```csharp
protected override void ConfigureWebHostBuilder(IWebHostBuilder builder)
{
    builder.UseEnvironment("Staging");
    builder.UseSetting("MyFeature:Enabled", "true");
    builder.ConfigureKestrel(options => options.AddServerHeader = false);
}
```

## Test Isolation Helpers

### GetIsolatedName

Creates a unique name for resources like database tables:

```csharp
// In a test with UniqueId = 42:
var tableName = GetIsolatedName("todos");  // Returns "Test_42_todos"
var topicName = GetIsolatedName("orders"); // Returns "Test_42_orders"
```

### GetIsolatedPrefix

Creates a unique prefix for key-based resources:

```csharp
// In a test with UniqueId = 42:
var prefix = GetIsolatedPrefix();       // Returns "test_42_"
var dotPrefix = GetIsolatedPrefix("."); // Returns "test.42."
```

## Container Integration

### With Testcontainers

```csharp
public class InMemoryDatabase : IAsyncInitializer, IAsyncDisposable
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await Container.StartAsync();
    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}

public class WebApplicationFactory : TestWebApplicationFactory<Program>
{
    [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryDatabase Database { get; init; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:ConnectionString", Database.Container.GetConnectionString() }
            });
        });
    }
}
```

### Per-Test Table Isolation

```csharp
public abstract class TodoTestBase : TestsBase
{
    [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryDatabase Database { get; init; } = null!;

    protected string TableName { get; private set; } = null!;

    protected override async Task SetupAsync()
    {
        TableName = GetIsolatedName("todos");
        await CreateTableAsync(TableName);
    }

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Database:TableName", TableName }
        });
    }

    [After(HookType.Test)]
    public async Task CleanupTable()
    {
        await DropTableAsync(TableName);
    }

    private async Task CreateTableAsync(string name) { /* ... */ }
    private async Task DropTableAsync(string name) { /* ... */ }
}
```

## HTTP Exchange Capture

Capture and inspect HTTP requests/responses for assertions:

```csharp
public class CaptureTests : TestsBase
{
    protected override WebApplicationTestOptions Options => new()
    {
        EnableHttpExchangeCapture = true
    };

    [Test]
    public async Task RequestIsCaptured()
    {
        var client = Factory.CreateClient();

        await client.GetAsync("/api/todos");

        await Assert.That(HttpCapture).IsNotNull();
        await Assert.That(HttpCapture!.Last!.Response.StatusCode)
            .IsEqualTo(HttpStatusCode.OK);
    }
}
```

### Capture Options

```csharp
protected override WebApplicationTestOptions Options => new()
{
    EnableHttpExchangeCapture = true,
    CaptureRequestBody = true,
    CaptureResponseBody = true,
    MaxBodySize = 1024 * 1024  // 1MB limit
};
```

### Inspecting Captured Exchanges

```csharp
// Get the last exchange
var last = HttpCapture!.Last;

// Get all exchanges
var all = HttpCapture.All;

// Inspect request
await Assert.That(last!.Request.Method).IsEqualTo("POST");
await Assert.That(last.Request.Path).IsEqualTo("/api/todos");
await Assert.That(last.Request.Body).Contains("\"title\"");

// Inspect response
await Assert.That(last.Response.StatusCode).IsEqualTo(HttpStatusCode.Created);
await Assert.That(last.Response.Body).Contains("\"id\"");
```

## TUnit Logging Integration

Server logs are automatically correlated with TUnit test output:

```csharp
protected override WebApplicationTestOptions Options => new()
{
    AddTUnitLogging = true  // Default is true
};
```

Logs from your ASP.NET Core app will appear in the test output, making debugging easier.

## Best Practices

### 1. Always Isolate Shared Resources

:::tip Golden Rule
If a resource is shared (database, queue, cache), each test must use its own isolated instance of that resource.
:::

```csharp
// ❌ BAD: All tests share the same table - will cause flaky failures
protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
{
    config.AddInMemoryCollection(new Dictionary<string, string?>
    {
        { "Database:TableName", "todos" }  // Shared = flaky!
    });
}

// ✅ GOOD: Each test gets its own table
protected override async Task SetupAsync()
{
    TableName = GetIsolatedName("todos");  // "Test_42_todos"
    await CreateTableAsync(TableName);
}

protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
{
    config.AddInMemoryCollection(new Dictionary<string, string?>
    {
        { "Database:TableName", TableName }  // Isolated = reliable!
    });
}
```

Common resources that need isolation:
- **Database tables**: Use `GetIsolatedName("tablename")`
- **Message queues/topics**: Use `GetIsolatedName("queue")`
- **Cache keys**: Use `GetIsolatedPrefix()` as a key prefix
- **Blob storage paths**: Use `GetIsolatedPrefix()` as a path prefix
- **Redis keys**: Use `GetIsolatedPrefix()` as a key prefix

### 2. Use Base Classes for Common Setup

```csharp
// Shared base for all tests
public abstract class TestsBase : WebApplicationTest<WebApplicationFactory, Program>
{
}

// Specialized base for database tests
public abstract class DatabaseTestBase : TestsBase
{
    protected override async Task SetupAsync()
    {
        await CreateSchemaAsync();
    }
}

// Actual tests
public class UserTests : DatabaseTestBase
{
    [Test]
    public async Task CreateUser_Works() { /* ... */ }
}
```

### 3. Clean Up Resources

```csharp
[After(HookType.Test)]
public async Task Cleanup()
{
    await CleanupTestDataAsync();
}
```

### 4. Inject Containers at Factory Level

```csharp
public class WebApplicationFactory : TestWebApplicationFactory<Program>
{
    // Shared across all tests
    [ClassDataSource<PostgresContainer>(Shared = SharedType.PerTestSession)]
    public PostgresContainer Postgres { get; init; } = null!;

    [ClassDataSource<RedisContainer>(Shared = SharedType.PerTestSession)]
    public RedisContainer Redis { get; init; } = null!;
}
```

## Complete Example

```csharp
// Container wrapper
public class InMemoryPostgres : IAsyncInitializer, IAsyncDisposable
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder().Build();
    public async Task InitializeAsync() => await Container.StartAsync();
    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}

// Factory with shared container
public class WebApplicationFactory : TestWebApplicationFactory<Program>
{
    [ClassDataSource<InMemoryPostgres>(Shared = SharedType.PerTestSession)]
    public InMemoryPostgres Postgres { get; init; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:ConnectionString", Postgres.Container.GetConnectionString() }
            });
        });
    }
}

// Base class
public abstract class TestsBase : WebApplicationTest<WebApplicationFactory, Program>
{
}

// Test base with table isolation
public abstract class TodoTestBase : TestsBase
{
    [ClassDataSource<InMemoryPostgres>(Shared = SharedType.PerTestSession)]
    public InMemoryPostgres Postgres { get; init; } = null!;

    protected string TableName { get; private set; } = null!;

    protected override async Task SetupAsync()
    {
        TableName = GetIsolatedName("todos");
        await CreateTableAsync();
    }

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Database:TableName", TableName }
        });
    }

    [After(HookType.Test)]
    public async Task Cleanup() => await DropTableAsync();

    private async Task CreateTableAsync() { /* ... */ }
    private async Task DropTableAsync() { /* ... */ }
}

// Actual tests
public class TodoApiTests : TodoTestBase
{
    [Test]
    public async Task CreateTodo_ReturnsCreated()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/todos", new { Title = "Test" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }

    [Test, Repeat(5)]
    public async Task ParallelTests_AreIsolated()
    {
        var client = Factory.CreateClient();

        // Each repetition has its own table
        await client.PostAsJsonAsync("/todos", new { Title = "Isolated" });

        var todos = await client.GetFromJsonAsync<List<Todo>>("/todos");
        await Assert.That(todos!.Count).IsEqualTo(1);  // Always 1, not 5
    }
}
```

## Migrating from Basic WebApplicationFactory

If you're currently using `WebApplicationFactory<TEntryPoint>` directly:

**Before:**
```csharp
public class MyTests
{
    [ClassDataSource<WebAppFactory>(Shared = SharedType.PerTestSession)]
    public required WebAppFactory Factory { get; init; }

    [Test]
    public async Task Test1()
    {
        var client = Factory.CreateClient();
        // Tests share state - not isolated!
    }
}
```

**After:**
```csharp
public class MyTests : WebApplicationTest<WebAppFactory, Program>
{
    [Test]
    public async Task Test1()
    {
        var client = Factory.CreateClient();  // Isolated per test!
    }
}
```

The key benefits:
- Each test gets its own isolated factory via `WithWebHostBuilder`
- `SetupAsync` enables async initialization before factory creation
- `ConfigureTestServices` and `ConfigureTestConfiguration` are per-test
- Built-in isolation helpers (`GetIsolatedName`, `GetIsolatedPrefix`)
