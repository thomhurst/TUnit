# Aspire Integration Testing

TUnit provides first-class support for [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/overview) integration testing through the `TUnit.Aspire` package. This package eliminates the boilerplate of managing an Aspire distributed application in tests, handling the full lifecycle (build, start, wait for resources, stop, dispose) automatically.

## Installation

```bash
dotnet add package TUnit.Aspire
```

:::info Prerequisites
- An Aspire AppHost project in your solution
- Docker running (Aspire uses containers for infrastructure resources)
- .NET 8.0 or later
:::

## Quick Start

### 1. Use the Fixture Directly

The simplest approach requires no subclassing at all:

```csharp
[ClassDataSource<AspireFixture<Projects.MyAppHost>>(Shared = SharedType.PerTestSession)]
public class ApiTests(AspireFixture<Projects.MyAppHost> fixture)
{
    [Test]
    public async Task GetWeatherForecast_ReturnsOk()
    {
        var client = fixture.CreateHttpClient("apiservice");

        var response = await client.GetAsync("/weatherforecast");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
```

That's it. The fixture will:
1. Build your Aspire AppHost
2. Start all containers and projects
3. Wait for all resources to become healthy
4. Provide HTTP clients and connection strings
5. Stop and dispose everything when tests complete

### 2. Subclass for Customization

For more control, create a subclass:

```csharp
using TUnit.Aspire;

public class AppFixture : AspireFixture<Projects.MyAppHost>
{
    protected override TimeSpan ResourceTimeout => TimeSpan.FromMinutes(3);

    protected override void ConfigureBuilder(IDistributedApplicationTestingBuilder builder)
    {
        // Configure the builder before the app is built
        builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
    }
}
```

Then use it in tests:

```csharp
[ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
public class ApiTests(AppFixture fixture)
{
    [Test]
    public async Task GetWeatherForecast_ReturnsOk()
    {
        var client = fixture.CreateHttpClient("apiservice");
        var response = await client.GetAsync("/weatherforecast");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
```

## Core Concepts

### Lifecycle

`AspireFixture<TAppHost>` implements `IAsyncInitializer` and `IAsyncDisposable`, integrating with TUnit's lifecycle automatically:

```
┌──────────────────────────────────────────────────────────────────┐
│                  FIXTURE LIFECYCLE                                │
├──────────────────────────────────────────────────────────────────┤
│  1. CreateAsync<TAppHost>()    Build the Aspire test builder     │
│  2. ConfigureBuilder()         Your customization hook           │
│  3. BuildAsync()               Build the distributed app         │
│  4. StartAsync()               Start containers & projects       │
│     ↳ Resource monitoring      Real-time state change logging    │
│  5. WaitForResources()         Wait for healthy/running state    │
│     ↳ Fail-fast detection      Immediate error on FailedToStart  │
│  ─────────────────────────────────────────────────────────────── │
│  6. Tests run                  Use CreateHttpClient, App, etc.   │
│  ─────────────────────────────────────────────────────────────── │
│  7. StopAsync()                Stop the application              │
│  8. DisposeAsync()             Clean up all resources             │
└──────────────────────────────────────────────────────────────────┘
```

### Shared Session

Use `Shared = SharedType.PerTestSession` to start the Aspire app once and share it across all tests:

```csharp
[ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
public class OrderTests(AppFixture fixture) { /* ... */ }

[ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
public class ProductTests(AppFixture fixture) { /* ... */ }
// Both test classes share the same AppFixture instance
```

This is the recommended approach since starting an Aspire distributed application is expensive (containers, databases, etc.).

### Resource Waiting

By default, the fixture waits for **all resources to become healthy** before tests run. You can customize this:

```csharp
public class AppFixture : AspireFixture<Projects.MyAppHost>
{
    // Option 1: Change the wait behavior via property
    protected override ResourceWaitBehavior WaitBehavior => ResourceWaitBehavior.AllRunning;

    // Option 2: Wait for specific resources only
    protected override ResourceWaitBehavior WaitBehavior => ResourceWaitBehavior.Named;
    protected override IEnumerable<string> ResourcesToWaitFor() => ["apiservice", "worker"];

    // Option 3: Full control over the waiting logic
    protected override async Task WaitForResourcesAsync(
        DistributedApplication app, CancellationToken cancellationToken)
    {
        var notifications = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifications.WaitForResourceAsync("apiservice",
            KnownResourceStates.Running, cancellationToken);
        await notifications.WaitForResourceAsync("worker",
            KnownResourceStates.Running, cancellationToken);
    }
}
```

Available `ResourceWaitBehavior` values:

| Value | Description |
|-------|-------------|
| `AllHealthy` | Wait for all resources to pass health checks (default) |
| `AllRunning` | Wait for all resources to reach the Running state |
| `Named` | Wait only for resources returned by `ResourcesToWaitFor()` |
| `None` | Don't wait — handle readiness manually in tests |

### Timeouts

The `ResourceTimeout` controls how long the fixture waits for both `StartAsync()` and resource readiness:

```csharp
public class AppFixture : AspireFixture<Projects.MyAppHost>
{
    // Default is 60 seconds. Increase for slow containers or CI environments.
    protected override TimeSpan ResourceTimeout => TimeSpan.FromMinutes(3);
}
```

When a timeout occurs, the error includes:
- Which resources are ready vs. still pending
- Recent container logs from pending resources
- Diagnostic information about the failure

## Public API

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `App` | `DistributedApplication` | The running Aspire app. Access for advanced scenarios. |

### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `CreateHttpClient(resourceName, endpointName?)` | `HttpClient` | Creates an HTTP client connected to the named resource |
| `GetConnectionStringAsync(resourceName, ct?)` | `Task<string?>` | Gets the connection string for the named resource |
| `WatchResourceLogs(resourceName)` | `IAsyncDisposable` | Streams resource logs to the current test's output |

### Virtual Methods (Override to Customize)

| Method | Default | Description |
|--------|---------|-------------|
| `InitializeAsync()` | Full lifecycle | Override to add post-start logic (migrations, seeding) |
| `DisposeAsync()` | Stop and dispose app | Override to add custom cleanup |
| `Args` | Empty | Command-line arguments passed to the AppHost entry point |
| `ConfigureBuilder(builder)` | No-op | Customize the builder before building |
| `ResourceTimeout` | 60 seconds | How long to wait for startup and resources |
| `WaitBehavior` | `AllHealthy` | Which resources to wait for |
| `ResourcesToWaitFor()` | Empty | Resource names when `WaitBehavior` is `Named` |
| `WaitForResourcesAsync(app, ct)` | Waits per `WaitBehavior` | Full control over resource waiting |
| `LogProgress(message)` | Writes to stderr | Override to route progress logs elsewhere |

### Overriding the Lifecycle

`InitializeAsync` and `DisposeAsync` are virtual, so you can add post-start or pre-dispose logic:

```csharp
public class AppFixture : AspireFixture<Projects.MyAppHost>
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync(); // Build, start, wait for resources

        // Post-start: run migrations, seed data, warm caches, etc.
        var connectionString = await GetConnectionStringAsync("postgresdb");
        await RunMigrationsAsync(connectionString!);
        await SeedTestDataAsync(connectionString!);
    }

    public override async ValueTask DisposeAsync()
    {
        // Pre-dispose: dump diagnostics on failure, clean up external state, etc.
        LogProgress("Cleaning up test data...");
        await base.DisposeAsync();
    }
}
```

### Passing Arguments to the AppHost

Use the `Args` property to pass command-line arguments to the AppHost entry point. These are forwarded to `DistributedApplicationTestingBuilder.CreateAsync` and are available in the AppHost's `builder.Configuration` during builder creation — before `ConfigureBuilder` is called:

```csharp
public class AppFixture : AspireFixture<Projects.MyAppHost>
{
    protected override string[] Args =>
    [
        "UseVolumes=false",
        "UsePostgresWithPersistentLifetime=false",
        "UsePostgresWithSessionLifetime=true"
    ];
}
```

:::tip When to use `Args` vs `ConfigureBuilder`
- Use **`Args`** for configuration values that the AppHost reads during `CreateBuilder(args)` — these must be set *before* the builder is created.
- Use **`ConfigureBuilder`** for service registrations, HTTP client defaults, and other configuration that can be applied *after* the builder is created.
:::

## Watching Resource Logs

Use `WatchResourceLogs()` inside a test to stream a resource's container logs to the test output. This is invaluable for debugging failures:

```csharp
[Test]
public async Task Debug_Api_Behavior()
{
    await using var _ = fixture.WatchResourceLogs("apiservice");

    var client = fixture.CreateHttpClient("apiservice");
    var response = await client.PostAsJsonAsync("/api/orders", new { /* ... */ });

    // If this fails, the apiservice container logs will be in the test output
    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
}
```

Dispose the returned value (or use `await using`) to stop watching.

## Building Fixture Chains

For real-world apps, you'll want layered fixtures. Use TUnit's `[ClassDataSource]` property injection to create dependency chains:

### HTTP Client Fixture

```csharp
public class ApiClientFixture : IAsyncInitializer
{
    [ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
    public required AppFixture App { get; init; }

    public HttpClient Client { get; private set; } = null!;

    public Task InitializeAsync()
    {
        Client = App.CreateHttpClient("apiservice");
        Client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        return Task.CompletedTask;
    }
}
```

### Database Fixture

```csharp
public class DatabaseFixture : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
    public required AppFixture App { get; init; }

    public NpgsqlConnection Connection { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await App.GetConnectionStringAsync("postgresdb");
        Connection = new NpgsqlConnection(connectionString);
        await Connection.OpenAsync();
    }

    public async ValueTask DisposeAsync() => await Connection.DisposeAsync();
}
```

### Redis Fixture

```csharp
public class RedisFixture : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
    public required AppFixture App { get; init; }

    public IConnectionMultiplexer Connection { get; private set; } = null!;
    public IDatabase Database => Connection.GetDatabase();

    public async Task InitializeAsync()
    {
        var connectionString = await App.GetConnectionStringAsync("redis");
        Connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
    }

    public async ValueTask DisposeAsync() => await Connection.DisposeAsync();
}
```

### Using Fixtures in Tests

```csharp
[Category("Integration"), Category("Cache")]
public class ProductCacheTests
{
    [ClassDataSource<ApiClientFixture>(Shared = SharedType.PerTestSession)]
    public required ApiClientFixture Api { get; init; }

    [ClassDataSource<RedisFixture>(Shared = SharedType.PerTestSession)]
    public required RedisFixture Redis { get; init; }

    [Test]
    public async Task Product_Is_Cached_After_Fetch()
    {
        // Create a product via API
        var response = await Api.Client.PostAsJsonAsync("/api/products",
            new { Name = "Test", Category = "electronics", Price = 9.99m });
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();

        // Fetch it (triggers caching)
        await Api.Client.GetAsync($"/api/products/{product!.Id}");

        // Verify Redis has the cached entry
        var cached = await Redis.Database.StringGetAsync($"product:{product.Id}");
        await Assert.That(cached.HasValue).IsTrue();
    }
}
```

TUnit resolves the dependency chain automatically: `AppFixture` starts first, then `ApiClientFixture` and `RedisFixture` initialize using the running app.

## Diagnostics

### Progress Logging

During initialization, the fixture logs progress to stderr for CI visibility:

```
[Aspire] Creating distributed application builder for MyAppHost...
[Aspire] Builder created in 0.3s
[Aspire] Building application...
[Aspire] Application built in 1.2s
[Aspire] Starting application with resources: [postgres, redis, apiservice, worker]
[Aspire]   [postgres] unknown -> Starting
[Aspire]   [redis] unknown -> Starting
[Aspire]   [postgres] Starting -> Running
[Aspire]   [redis] Starting -> Running
[Aspire] Application started in 8.5s. Waiting for resources...
[Aspire]   Resource 'apiservice' is healthy (1/4)
[Aspire]   Resource 'worker' is healthy (2/4)
[Aspire] All resources ready.
```

Override `LogProgress` to route these messages elsewhere:

```csharp
public class AppFixture : AspireFixture<Projects.MyAppHost>
{
    protected override void LogProgress(string message)
    {
        // Route to your preferred logger
        Console.WriteLine(message);
    }
}
```

### Timeout Diagnostics

When a timeout occurs, the error message includes container logs from the failing resources, so you can see exactly what went wrong without having to reproduce the failure:

```
TimeoutException: Timed out after 60s waiting for the Aspire application to start.

--- redis logs ---
  Error accepting a client connection: error:0A000126:SSL routines::unexpected eof
  Error accepting a client connection: error:0A000126:SSL routines::unexpected eof
```

### Fail-Fast Detection

The default resource waiting logic watches for resources entering a `FailedToStart` state. If any resource fails, the fixture throws immediately with that resource's logs instead of waiting for the full timeout.

## CI/CD

### GitHub Actions

```yaml
jobs:
  integration-tests:
    runs-on: ubuntu-latest
    timeout-minutes: 15
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 9.0.x

      - run: dotnet build MyApp.Tests -c Release
      - run: dotnet run --project MyApp.Tests -c Release --no-build
        env:
          ASPIRE_ALLOW_UNSECURED_TRANSPORT: "true"
```

:::warning ASPIRE_ALLOW_UNSECURED_TRANSPORT
Set `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true` in CI environments where the ASP.NET Core developer certificate isn't trusted. Without this, container health checks may fail with TLS errors.
:::

### Tips for CI

- **Increase `ResourceTimeout`** — CI runners are slower than local machines. 2-5 minutes is typical.
- **Use `Shared = SharedType.PerTestSession`** — Start the app once, not per test class.
- **Check Docker availability** — Aspire requires Docker. Ensure your CI runner has it installed.

## Templates

TUnit includes project templates for Aspire testing:

```bash
# Install TUnit templates
dotnet new install TUnit.Templates

# Scaffold a complete Aspire solution with tests
dotnet new tunit-aspire-starter -n MyApp

# Add a test project to an existing Aspire solution
dotnet new tunit-aspire-test -n MyApp.Tests
```

## FAQ & Troubleshooting

### StartAsync hangs or times out

**Symptom:** Tests time out during startup with no obvious error.

**Common causes:**
1. **TLS/SSL errors** — Set `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true` or call `.WithoutHttpsCertificate()` on container resources in your AppHost.
2. **Docker images not pulled** — First run pulls container images, which can take minutes. Increase `ResourceTimeout`.
3. **Docker not running** — Aspire requires Docker. Verify with `docker info`.

The fixture logs resource state changes in real time to stderr, so check your CI output for lines like `[redis] Running -> unhealthy`.

### How do I access infrastructure directly?

Use `App` to access the full `DistributedApplication`, then get services or connection strings:

```csharp
// Direct service access
var notifications = fixture.App.Services.GetRequiredService<ResourceNotificationService>();

// Connection strings
var connStr = await fixture.GetConnectionStringAsync("postgresdb");
```

### Can I run different AppHosts in different test classes?

Yes. Create separate fixtures for each AppHost:

```csharp
public class AppAFixture : AspireFixture<Projects.AppHostA> { }
public class AppBFixture : AspireFixture<Projects.AppHostB> { }

[ClassDataSource<AppAFixture>(Shared = SharedType.PerTestSession)]
public class AppATests(AppAFixture fixture) { /* ... */ }

[ClassDataSource<AppBFixture>(Shared = SharedType.PerTestSession)]
public class AppBTests(AppBFixture fixture) { /* ... */ }
```

### How do I skip waiting for tool containers?

Tool containers like pgAdmin or RedisInsight don't need to be ready before tests run. Use `Named` wait behavior:

```csharp
public class AppFixture : AspireFixture<Projects.MyAppHost>
{
    protected override ResourceWaitBehavior WaitBehavior => ResourceWaitBehavior.Named;

    protected override IEnumerable<string> ResourcesToWaitFor()
        => ["apiservice", "worker", "postgres", "redis"];
    // pgadmin, redisinsight are excluded — tests don't need them
}
```

### My resource never becomes healthy

If a resource stays in `Running` but never reaches `Healthy`, check:
1. The resource has a health check configured (`.WithHttpHealthCheck("/health")` or similar)
2. The health check endpoint is reachable from inside the container network
3. Use `WatchResourceLogs("resourceName")` in a test to see the resource's output

If the resource doesn't have health checks, use `AllRunning` instead of `AllHealthy`:

```csharp
protected override ResourceWaitBehavior WaitBehavior => ResourceWaitBehavior.AllRunning;
```

### What's the difference between TUnit.Aspire and TUnit.AspNetCore?

| | TUnit.Aspire | TUnit.AspNetCore |
|---|---|---|
| **Purpose** | Test distributed apps (multiple services + infrastructure) | Test a single ASP.NET Core app |
| **Infrastructure** | Real containers via Aspire/Docker | In-process `TestServer` or Testcontainers |
| **Isolation** | Shared app, per-test HTTP clients | Per-test `WebApplicationFactory` |
| **Use when** | Your app uses Aspire orchestration | Your app is a single ASP.NET Core project |

They can be used together — for example, using Aspire to manage infrastructure while using `TestWebApplicationFactory` for per-test app isolation.
