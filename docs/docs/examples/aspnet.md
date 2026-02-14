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

Understanding the execution order is critical for writing correct tests. Here's the complete verified order:

```
┌─────────────────────────────────────────────────────────────────┐
│                    TEST LIFECYCLE                               │
├─────────────────────────────────────────────────────────────────┤
│  1. ConfigureTestOptions        Set test options (HTTP capture) │
│  2. SetupAsync                  Async setup (create tables)     │
│  ───────────────────────────────────────────────────────────    │
│  3. Factory.ConfigureWebHost    Base factory configuration      │
│  4. Factory.ConfigureStartup... Base factory startup config     │
│  ───────────────────────────────────────────────────────────    │
│  5. ConfigureTestConfiguration  Test config (overrides factory) │
│  6. ConfigureWebHostBuilder     Escape hatch (low-level access) │
│  7. ConfigureTestServices       Test services (overrides)       │
│  ───────────────────────────────────────────────────────────    │
│  8. Application Startup         Server starts                   │
│  ───────────────────────────────────────────────────────────    │
│  9. Test Method Executes        Your test code runs             │
│ 10. Factory Disposed            Cleanup                         │
└─────────────────────────────────────────────────────────────────┘
```

**Key Points:**

| Hook | Scope | Purpose |
|------|-------|---------|
| `ConfigureTestOptions` | Per-test | Enable features like HTTP capture |
| `SetupAsync` | Per-test | Async operations before config (create DB tables) |
| `Factory.ConfigureWebHost` | Shared | Base configuration for all tests |
| `Factory.ConfigureStartupConfiguration` | Shared | Base startup configuration |
| `ConfigureTestConfiguration` | Per-test | Override factory configuration |
| `ConfigureWebHostBuilder` | Per-test | Low-level escape hatch |
| `ConfigureTestServices` | Per-test | Override factory services |

:::tip Tests Can Override Factory
The order is designed so that **tests can override factory defaults**. Factory configuration runs first (steps 3-4), then test-specific configuration (steps 5-7) can override those values.
:::

:::warning Factory Methods Run Once
`Factory.ConfigureWebHost` and `Factory.ConfigureStartupConfiguration` run **once per test session** (when the factory is first used), not per-test. If different test classes need fundamentally different factory configurations, use different factory classes.
:::

## Override Methods

### ConfigureTestOptions

Use to configure test-level options before anything else runs:

```csharp
protected override void ConfigureTestOptions(WebApplicationTestOptions options)
{
    options.EnableHttpExchangeCapture = true;  // Capture HTTP requests/responses
}
```

This runs **first** in the lifecycle, before `SetupAsync`. Use it to enable features that affect how the test infrastructure is set up.

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

:::tip Available on All Tests
The isolation helpers (`UniqueId`, `GetIsolatedName`, `GetIsolatedPrefix`) are also available on `TestContext.Current!.Isolation` for any test — not just ASP.NET Core tests. Use `TestContext.Current!.Isolation.GetIsolatedName("resource")` when you don't inherit from `WebApplicationTest`. Both share the same counter, so IDs are unique across all test types.
:::

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

## FAQ & Troubleshooting

### Why does my test configuration not override the factory?

**Problem:** You set a value in `ConfigureTestConfiguration` but the factory's value is still used.

**Solution:** Make sure you're using the same configuration key. The test configuration runs **after** the factory configuration (step 5 vs steps 3-4), so it should override. Check that:

1. You're using `AddInMemoryCollection` which adds to the config sources
2. The configuration key path is exactly the same
3. You're not accidentally reading from a different source (e.g., `appsettings.json`)

```csharp
// Factory sets default
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.ConfigureAppConfiguration((_, config) =>
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Database:ConnectionString", "factory-default" }
        });
    });
}

// Test overrides - this WILL work because it runs after
protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
{
    config.AddInMemoryCollection(new Dictionary<string, string?>
    {
        { "Database:ConnectionString", "test-specific-value" }  // This wins!
    });
}
```

### Why can't I access SetupAsync results in ConfigureTestOptions?

**Problem:** You want to use a value from `SetupAsync` in `ConfigureTestOptions`, but `ConfigureTestOptions` runs first.

**Solution:** This is by design. `ConfigureTestOptions` runs before `SetupAsync` because test options affect how the infrastructure is set up. If you need async setup before options, consider:

1. Moving the logic to a `[Before(HookType.Test)]` method that runs even earlier
2. Using lazy initialization in `SetupAsync`

### Why are my parallel tests interfering with each other?

**Problem:** Tests that pass individually fail when run in parallel.

**Solution:** You're sharing resources without isolation. Use `GetIsolatedName()` and `GetIsolatedPrefix()`:

```csharp
// BAD: All parallel tests share the same table
var tableName = "todos";

// GOOD: Each test gets its own table
var tableName = GetIsolatedName("todos");  // "Test_42_todos", "Test_43_todos", etc.
```

### Can I have different factory configurations for different test classes?

**Problem:** Test class A needs PostgreSQL, test class B needs SQLite.

**Solution:** Create different factory classes:

```csharp
public class PostgresFactory : TestWebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // PostgreSQL configuration
    }
}

public class SqliteFactory : TestWebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // SQLite configuration
    }
}

public class PostgresTests : WebApplicationTest<PostgresFactory, Program> { }
public class SqliteTests : WebApplicationTest<SqliteFactory, Program> { }
```

### What's the difference between Factory and GlobalFactory?

| Property | Type | Scope | Use Case |
|----------|------|-------|----------|
| `Factory` | `WebApplicationFactory<TEntryPoint>` | Per-test | Creating HTTP clients, accessing services |
| `GlobalFactory` | `TFactory` (your custom type) | Shared | Accessing custom factory properties (containers, etc.) |

```csharp
public class MyTests : WebApplicationTest<WebApplicationFactory, Program>
{
    [Test]
    public async Task Example()
    {
        // Use Factory for per-test operations
        var client = Factory.CreateClient();
        var services = Factory.Services;

        // Use GlobalFactory to access custom properties
        var connectionString = GlobalFactory.Database.Container.GetConnectionString();
    }
}
```

### Why does my service registration not work?

**Problem:** You register a service in `ConfigureTestServices` but the old implementation is still used.

**Solution:** Use `ReplaceService` instead of `AddSingleton`:

```csharp
protected override void ConfigureTestServices(IServiceCollection services)
{
    // BAD: Adds a second registration, original may still be resolved
    services.AddSingleton<IEmailService, FakeEmailService>();

    // GOOD: Removes existing registration and adds new one
    services.ReplaceService<IEmailService>(new FakeEmailService());
}
```

### How do I debug lifecycle issues?

Create a test that logs all lifecycle events:

```csharp
public class LifecycleDebugTest : WebApplicationTest<WebApplicationFactory, Program>
{
    protected override void ConfigureTestOptions(WebApplicationTestOptions options)
    {
        Console.WriteLine("1. ConfigureTestOptions");
    }

    protected override async Task SetupAsync()
    {
        Console.WriteLine("2. SetupAsync");
        await base.SetupAsync();
    }

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        Console.WriteLine("5. ConfigureTestConfiguration");
    }

    protected override void ConfigureWebHostBuilder(IWebHostBuilder builder)
    {
        Console.WriteLine("6. ConfigureWebHostBuilder");
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        Console.WriteLine("7. ConfigureTestServices");
    }

    [Test]
    public async Task Debug_Lifecycle()
    {
        Console.WriteLine("9. Test executing");
        _ = Factory.CreateClient();
        await Assert.That(true).IsTrue();
    }
}
```

### Can I run async code in ConfigureTestServices?

**Problem:** ASP.NET Core's configuration methods are synchronous, but you need async initialization.

**Solution:** Do async work in `SetupAsync`, then use the results in sync methods:

```csharp
public class MyTest : TestsBase
{
    private string _authToken = null!;

    protected override async Task SetupAsync()
    {
        // Async work here
        _authToken = await GetAuthTokenAsync();
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        // Use the result from SetupAsync
        services.AddSingleton(new AuthConfig { Token = _authToken });
    }
}
```

### Why does my Program.cs run before ConfigureWebHost's ConfigureAppConfiguration?

**Problem:** You set configuration values in `ConfigureWebHost` using `ConfigureAppConfiguration`, but your app's `Program.cs` doesn't see them during startup. Your breakpoint in Program.cs hits **before** the `ConfigureAppConfiguration` callback.

```csharp
// Factory - this approach has a timing issue!
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    Console.WriteLine("ConfigureWebHost called");  // This runs first...

    builder.ConfigureAppConfiguration((_, config) =>
    {
        Console.WriteLine("ConfigureAppConfiguration callback");  // ...but THIS runs AFTER Program.cs!
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "SomeKey", "SomeValue" }
        });
    });
}

// Program.cs - this runs BEFORE ConfigureAppConfiguration callback!
var builder = WebApplication.CreateBuilder(args);
if (builder.Configuration["SomeKey"] != "SomeValue")
{
    throw new InvalidOperationException("SomeKey not found!");  // This throws!
}
```

**Root Cause:** This is **expected behavior** of ASP.NET Core's `WebApplicationFactory`. The `ConfigureAppConfiguration` callbacks registered in `ConfigureWebHost` are **deferred** and run **after** your app's `Program.cs` code, not before.

**Solution:** Use `ConfigureStartupConfiguration` instead, which uses `builder.UseSetting()` to apply configuration **before** your app's `Program.cs` runs:

```csharp
public class WebApplicationFactory : TestWebApplicationFactory<Program>
{
    /// <summary>
    /// Use ConfigureStartupConfiguration for configuration your Program.cs needs during startup.
    /// This runs BEFORE Program.cs.
    /// </summary>
    protected override void ConfigureStartupConfiguration(IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "SomeKey", "SomeValue" },  // Available when Program.cs runs!
            { "Database:ConnectionString", "..." }
        });
    }

    /// <summary>
    /// ConfigureWebHost can still be used for other customizations,
    /// but NOT for configuration that Program.cs needs during startup.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Safe to use ConfigureAppConfiguration for config that's only
        // needed AFTER the app has started (e.g., in controllers, services)
    }
}
```

**When to use each method:**

| Method | Runs When | Use For |
|--------|-----------|---------|
| `ConfigureStartupConfiguration` | Before Program.cs | Configuration needed during app startup |
| `ConfigureWebHost` + `ConfigureAppConfiguration` | After Program.cs | Configuration only needed after app starts |

## API Reference

### WebApplicationTest Properties

| Property | Type | Description |
|----------|------|-------------|
| `UniqueId` | `int` | Unique identifier for this test instance |
| `GlobalFactory` | `TFactory` | Shared factory (your custom type) |
| `Factory` | `WebApplicationFactory<TEntryPoint>` | Per-test isolated factory |
| `Services` | `IServiceProvider` | DI container from per-test factory |
| `HttpCapture` | `HttpExchangeCapture?` | Captured HTTP exchanges (if enabled) |

### WebApplicationTest Methods

| Method | Description |
|--------|-------------|
| `GetIsolatedName(string baseName)` | Returns `"Test_{UniqueId}_{baseName}"` |
| `GetIsolatedPrefix(string separator = "_")` | Returns `"test{separator}{UniqueId}{separator}"` |

### WebApplicationTestOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableHttpExchangeCapture` | `bool` | `false` | Capture HTTP requests/responses |

### Service Collection Extensions

| Method | Description |
|--------|-------------|
| `ReplaceService<T>(instance)` | Replace service with instance |
| `ReplaceService<T>(factory)` | Replace service with factory |
| `ReplaceService<TService, TImpl>()` | Replace service with implementation |
| `RemoveService<T>()` | Remove service registration |
| `AddTUnitLogging(context)` | Add TUnit logging provider |
