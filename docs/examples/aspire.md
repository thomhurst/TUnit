# Aspire Integration Testing

TUnit provides first-class support for [Aspire](https://aspire.dev/get-started/what-is-aspire/) integration testing through the `TUnit.Aspire` package. This package eliminates the boilerplate of managing an Aspire distributed application in tests, handling the full lifecycle (build, start, wait for resources, stop, dispose) automatically.

## Installation[​](#installation "Direct link to Installation")

```
dotnet add package TUnit.Aspire
```

Shared test-infrastructure libraries

`TUnit.Aspire` references the `TUnit` metapackage, which marks the consuming project as a test project. If you are building a **shared library** of common testing infrastructure (one that is referenced by your actual test projects rather than run directly), reference `TUnit.Aspire.Core` instead — it depends only on `TUnit.Core` and won't flag the project as a test project. Your test projects then bring in `TUnit.Aspire` (or `TUnit` directly) themselves.

```
dotnet add package TUnit.Aspire.Core
```

Prerequisites

* An Aspire AppHost project in your solution
* Docker running (Aspire uses containers for infrastructure resources)
* .NET 8.0 or later

## Quick Start[​](#quick-start "Direct link to Quick Start")

### 1. Use the Fixture Directly[​](#1-use-the-fixture-directly "Direct link to 1. Use the Fixture Directly")

The simplest approach requires no subclassing at all:

```
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

### 2. Subclass for Customization[​](#2-subclass-for-customization "Direct link to 2. Subclass for Customization")

For more control, create a subclass:

```
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

```
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

## Core Concepts[​](#core-concepts "Direct link to Core Concepts")

### Lifecycle[​](#lifecycle "Direct link to Lifecycle")

`AspireFixture<TAppHost>` implements `IAsyncInitializer` and `IAsyncDisposable`, integrating with TUnit's lifecycle automatically:

```
┌──────────────────────────────────────────────────────────────────┐

│                  FIXTURE LIFECYCLE                                │

├──────────────────────────────────────────────────────────────────┤

│  1. CreateAsync<TAppHost>()    Build the Aspire test builder     │

│     ↳ ConfigureAppHost()       Configure options & host settings │

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

### Shared Session[​](#shared-session "Direct link to Shared Session")

Use `Shared = SharedType.PerTestSession` to start the Aspire app once and share it across all tests:

```
[ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]

public class OrderTests(AppFixture fixture) { /* ... */ }



[ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]

public class ProductTests(AppFixture fixture) { /* ... */ }

// Both test classes share the same AppFixture instance
```

This is the recommended approach since starting an Aspire distributed application is expensive (containers, databases, etc.).

### Resource Waiting[​](#resource-waiting "Direct link to Resource Waiting")

By default, the fixture waits for **all resources to become healthy** before tests run. You can customize this:

```
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

| Value        | Description                                                |
| ------------ | ---------------------------------------------------------- |
| `AllHealthy` | Wait for all resources to pass health checks (default)     |
| `AllRunning` | Wait for all resources to reach the Running state          |
| `Named`      | Wait only for resources returned by `ResourcesToWaitFor()` |
| `None`       | Don't wait — handle readiness manually in tests            |

### Removing Resources[​](#removing-resources "Direct link to Removing Resources")

Use `ResourcesToRemove()` to exclude specific resources from the distributed application before it is built. This is useful when your AppHost defines UI tools or optional infrastructure (e.g. `pgAdmin`, `RedisInsight`, `seq`) that are not needed — and potentially slow to start — during automated tests:

```
public class AppFixture : AspireFixture<Projects.MyAppHost>

{

    protected override IEnumerable<string> ResourcesToRemove()

        => ["pgadmin", "redisinsight", "seq"];

}
```

tip

Resources are removed by exact name (case-sensitive) after the builder is created but before the app is built, so they never start. Unrecognised names are silently ignored.

### Timeouts[​](#timeouts "Direct link to Timeouts")

```
public class AppFixture : AspireFixture<Projects.MyAppHost>

{

    // Default is 60 seconds. Increase for slow containers or CI environments.

    protected override TimeSpan ResourceTimeout => TimeSpan.FromMinutes(3);

}
```

When a timeout occurs, the error includes:

* Which resources are ready vs. still pending
* Recent container logs from pending resources
* Diagnostic information about the failure

## Public API[​](#public-api "Direct link to Public API")

### Properties[​](#properties "Direct link to Properties")

| Property | Type                     | Description                                            |
| -------- | ------------------------ | ------------------------------------------------------ |
| `App`    | `DistributedApplication` | The running Aspire app. Access for advanced scenarios. |

### Methods[​](#methods "Direct link to Methods")

| Method                                          | Returns            | Description                                                                                                                                                                               |
| ----------------------------------------------- | ------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `CreateHttpClient(resourceName, endpointName?)` | `HttpClient`       | Creates an HTTP client connected to the named resource. When telemetry collection is enabled, automatically propagates `traceparent` and `baggage` headers for cross-process correlation. |
| `GetConnectionStringAsync(resourceName, ct?)`   | `Task<string?>`    | Gets the connection string for the named resource                                                                                                                                         |
| `WatchResourceLogs(resourceName)`               | `IAsyncDisposable` | Streams resource logs to the current test's output                                                                                                                                        |

### Virtual Methods (Override to Customize)[​](#virtual-methods-override-to-customize "Direct link to Virtual Methods (Override to Customize)")

| Method                                | Default                  | Description                                                                                            |
| ------------------------------------- | ------------------------ | ------------------------------------------------------------------------------------------------------ |
| `InitializeAsync()`                   | Full lifecycle           | Override to add post-start logic (migrations, seeding)                                                 |
| `DisposeAsync()`                      | Stop and dispose app     | Override to add custom cleanup                                                                         |
| `Args`                                | Empty                    | Command-line arguments passed to the AppHost entry point                                               |
| `ConfigureAppHost(options, settings)` | No-op                    | Configure `DistributedApplicationOptions` and `HostApplicationBuilderSettings` during builder creation |
| `ConfigureBuilder(builder)`           | No-op                    | Customize the builder before building                                                                  |
| `EnableTelemetryCollection`           | `true`                   | Starts an OTLP receiver that correlates SUT logs to the originating test                               |
| `ResourceTimeout`                     | 60 seconds               | How long to wait for startup and resources                                                             |
| `WaitBehavior`                        | `AllHealthy`             | Which resources to wait for                                                                            |
| `ResourcesToWaitFor()`                | Empty                    | Resource names when `WaitBehavior` is `Named`                                                          |
| `ResourcesToRemove()`                 | Empty                    | Resource names to remove from the builder before the app is built                                      |
| `WaitForResourcesAsync(app, ct)`      | Waits per `WaitBehavior` | Full control over resource waiting                                                                     |
| `LogProgress(message)`                | Writes to stderr         | Override to route progress logs elsewhere                                                              |

### Overriding the Lifecycle[​](#overriding-the-lifecycle "Direct link to Overriding the Lifecycle")

`InitializeAsync` and `DisposeAsync` are virtual, so you can add post-start or pre-dispose logic:

```
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

### Passing Arguments to the AppHost[​](#passing-arguments-to-the-apphost "Direct link to Passing Arguments to the AppHost")

Use the `Args` property to pass command-line arguments to the AppHost entry point. These are forwarded to `DistributedApplicationTestingBuilder.CreateAsync` and are available in the AppHost's `builder.Configuration` during builder creation — before `ConfigureBuilder` is called:

```
public class AppFixture : AspireFixture<Projects.MyAppHost>

{

    protected override string[] Args =>

    [

        "--UseVolumes=false",

        "--UsePostgresWithPersistentLifetime=false",

        "--UsePostgresWithSessionLifetime=true"

    ];

}
```

When to use `Args` vs `ConfigureAppHost` vs `ConfigureBuilder`

* Use **`Args`** for configuration values that the AppHost reads during `CreateBuilder(args)` — these must be set *before* the builder is created.
* Use **`ConfigureAppHost`** to configure `DistributedApplicationOptions` (e.g., `DisableDashboard`) and `HostApplicationBuilderSettings` — these are passed to `CreateAsync` during builder creation.
* Use **`ConfigureBuilder`** for service registrations, HTTP client defaults, and other configuration that can be applied *after* the builder is created.

## Watching Resource Logs[​](#watching-resource-logs "Direct link to Watching Resource Logs")

Use `WatchResourceLogs()` inside a test to stream a resource's container logs to the test output. This is invaluable for debugging failures:

```
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

## Per-Test Telemetry Correlation[​](#per-test-telemetry-correlation "Direct link to Per-Test Telemetry Correlation")

By default, `AspireFixture` runs a lightweight OTLP receiver that automatically correlates SUT logs back to the test that triggered them. When a test calls `CreateHttpClient`, the outgoing request carries W3C `traceparent` and `baggage` headers (including `tunit.test.id`). The SUT's OpenTelemetry SDK exports logs with the same TraceId, and TUnit's receiver routes them to the correct test's output.

When the Aspire dashboard (or another OTLP backend wired through `DOTNET_DASHBOARD_OTLP_ENDPOINT_URL`) is enabled, `TUnit.Aspire` also exports the runner's own `"TUnit"` spans automatically. That means external backends see the full parent-child request trace out of the box:

```
test case

  └── test body

        └── GET /your-endpoint

              └── downstream HTTP / DB / custom spans
```

Only the per-test `"TUnit"` source is auto-exported this way. Session, discovery, assembly, and suite spans stay on the separate `"TUnit.Lifecycle"` source unless you opt in manually with your own tracer provider.

```
[Test]

public async Task Create_Order_Returns_201()

{

    var client = fixture.CreateHttpClient("apiservice");

    var response = await client.PostAsJsonAsync("/api/orders", new { Item = "Widget" });



    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

    // SUT logs for THIS request automatically appear in THIS test's output

}
```

When multiple tests run concurrently against the same resource, each test only sees its own request's logs — correlation is based on TraceId, not the resource name.

### SUT requirements[​](#sut-requirements "Direct link to SUT requirements")

The SUT must have OpenTelemetry configured to export logs and traces via OTLP. `AspireFixture` automatically injects the following environment variables into all project resources:

| Variable                      | Value                     | Purpose                                                  |
| ----------------------------- | ------------------------- | -------------------------------------------------------- |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://127.0.0.1:{port}` | Points to TUnit's OTLP receiver                          |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `http/protobuf`           | Protocol for OTLP export                                 |
| `OTEL_SERVICE_NAME`           | Aspire resource name      | Shown as `[service-name]` prefix in test output          |
| `OTEL_BLRP_SCHEDULE_DELAY`    | `1000`                    | Reduces log batch export delay for faster test feedback  |
| `OTEL_BSP_SCHEDULE_DELAY`     | `1000`                    | Reduces span batch export delay for faster test feedback |

The SUT only needs to register the OpenTelemetry exporters — TUnit handles everything else.

**If your SUT uses Aspire `ServiceDefaults`** (the default for `dotnet new aspire` projects), telemetry correlation works out of the box. No additional configuration is needed — `AddServiceDefaults()` already configures OpenTelemetry with OTLP export, and TUnit injects the endpoint automatically.

**If your SUT does not use `ServiceDefaults`**, add the OpenTelemetry packages and register the exporters:

```
<ItemGroup>

  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />

  <PackageReference Include="OpenTelemetry.Extensions.Hosting" />

  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" />

</ItemGroup>
```

```
builder.Services.AddOpenTelemetry()

    .WithTracing(tracing => tracing

        .AddAspNetCoreInstrumentation()

        .AddOtlpExporter())

    .WithLogging(logging => logging.AddOtlpExporter());



builder.Logging.AddOpenTelemetry(otel =>

{

    otel.IncludeFormattedMessage = true;

    otel.IncludeScopes = true;

});
```

Note that `OTEL_SERVICE_NAME` and `OTEL_EXPORTER_OTLP_ENDPOINT` are injected by TUnit, so the SUT does not need `.ConfigureResource()` or any endpoint configuration.

The key pieces are:

* **`AddAspNetCoreInstrumentation()`** — ensures incoming HTTP requests create spans that carry the test's TraceId, so logs within that request context inherit it.
* **`AddOtlpExporter()`** on both tracing and logging — exports telemetry to TUnit's OTLP receiver (endpoint is injected automatically).
* **`IncludeFormattedMessage = true`** — without this, log bodies are empty in the test output. Aspire `ServiceDefaults` sets this by default.

### Dashboard coexistence[​](#dashboard-coexistence "Direct link to Dashboard coexistence")

If the Aspire dashboard is enabled, TUnit's receiver acts as a transparent proxy — it processes telemetry for correlation and forwards SUT telemetry to the dashboard's original OTLP endpoint. At the same time, `TUnit.Aspire` exports the runner's per-test `"TUnit"` spans directly to that same OTLP backend. The result is a complete backend trace tree with the runner root spans and the SUT request spans in one trace, without extra test-project setup.

### Disabling telemetry collection[​](#disabling-telemetry-collection "Direct link to Disabling telemetry collection")

Override `EnableTelemetryCollection` to opt out:

```
public class AppFixture : AspireFixture<Projects.MyAppHost>

{

    protected override bool EnableTelemetryCollection => false;

}
```

When disabled, `CreateHttpClient` delegates directly to Aspire's default implementation without adding trace propagation headers.

### Limitations[​](#limitations "Direct link to Limitations")

* **Startup logs**: Logs emitted during app startup have no active trace context and cannot be correlated to a test. Use `WatchResourceLogs` for these.
* **Non-HTTP triggers**: Background jobs, timers, and message queue consumers that generate logs without an incoming HTTP request won't carry the test's TraceId.
* **Container resources**: Infrastructure resources like Redis and PostgreSQL don't have an OpenTelemetry SDK and can't export OTLP. Use `WatchResourceLogs` for their logs.

## Building Fixture Chains[​](#building-fixture-chains "Direct link to Building Fixture Chains")

For real-world apps, you'll want layered fixtures. Use TUnit's `[ClassDataSource]` property injection to create dependency chains:

### HTTP Client Fixture[​](#http-client-fixture "Direct link to HTTP Client Fixture")

```
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

### Database Fixture[​](#database-fixture "Direct link to Database Fixture")

```
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

### Redis Fixture[​](#redis-fixture "Direct link to Redis Fixture")

```
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

### Using Fixtures in Tests[​](#using-fixtures-in-tests "Direct link to Using Fixtures in Tests")

```
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

## Diagnostics[​](#diagnostics "Direct link to Diagnostics")

### Progress Logging[​](#progress-logging "Direct link to Progress Logging")

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

```
public class AppFixture : AspireFixture<Projects.MyAppHost>

{

    protected override void LogProgress(string message)

    {

        // Route to your preferred logger

        Console.WriteLine(message);

    }

}
```

### Timeout Diagnostics[​](#timeout-diagnostics "Direct link to Timeout Diagnostics")

When a timeout occurs, the error message includes container logs from the failing resources, so you can see exactly what went wrong without having to reproduce the failure:

```
TimeoutException: Timed out after 60s waiting for the Aspire application to start.



--- redis logs ---

  Error accepting a client connection: error:0A000126:SSL routines::unexpected eof

  Error accepting a client connection: error:0A000126:SSL routines::unexpected eof
```

### Fail-Fast Detection[​](#fail-fast-detection "Direct link to Fail-Fast Detection")

The default resource waiting logic watches for resources entering a `FailedToStart` state. If any resource fails, the fixture throws immediately with that resource's logs instead of waiting for the full timeout.

## CI/CD[​](#cicd "Direct link to CI/CD")

### GitHub Actions[​](#github-actions "Direct link to GitHub Actions")

```
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

ASPIRE\_ALLOW\_UNSECURED\_TRANSPORT

Set `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true` in CI environments where the ASP.NET Core developer certificate isn't trusted. Without this, container health checks may fail with TLS errors.

### Tips for CI[​](#tips-for-ci "Direct link to Tips for CI")

* **Increase `ResourceTimeout`** — CI runners are slower than local machines. 2-5 minutes is typical.
* **Use `Shared = SharedType.PerTestSession`** — Start the app once, not per test class.
* **Check Docker availability** — Aspire requires Docker. Ensure your CI runner has it installed.

## Templates[​](#templates "Direct link to Templates")

TUnit includes project templates for Aspire testing:

```
# Install TUnit templates

dotnet new install TUnit.Templates



# Scaffold a complete Aspire solution with tests

dotnet new tunit-aspire-starter -n MyApp



# Add a test project to an existing Aspire solution

dotnet new tunit-aspire-test -n MyApp.Tests
```

## FAQ & Troubleshooting[​](#faq--troubleshooting "Direct link to FAQ & Troubleshooting")

### StartAsync hangs or times out[​](#startasync-hangs-or-times-out "Direct link to StartAsync hangs or times out")

**Symptom:** Tests time out during startup with no obvious error.

**Common causes:**

1. **TLS/SSL errors** — Set `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true` or call `.WithoutHttpsCertificate()` on container resources in your AppHost.
2. **Docker images not pulled** — First run pulls container images, which can take minutes. Increase `ResourceTimeout`.
3. **Docker not running** — Aspire requires Docker. Verify with `docker info`.

The fixture logs resource state changes in real time to stderr, so check your CI output for lines like `[redis] Running -> unhealthy`.

### How do I access infrastructure directly?[​](#how-do-i-access-infrastructure-directly "Direct link to How do I access infrastructure directly?")

Use `App` to access the full `DistributedApplication`, then get services or connection strings:

```
// Direct service access

var notifications = fixture.App.Services.GetRequiredService<ResourceNotificationService>();



// Connection strings

var connStr = await fixture.GetConnectionStringAsync("postgresdb");
```

### Can I run different AppHosts in different test classes?[​](#can-i-run-different-apphosts-in-different-test-classes "Direct link to Can I run different AppHosts in different test classes?")

Yes. Create separate fixtures for each AppHost:

```
public class AppAFixture : AspireFixture<Projects.AppHostA> { }

public class AppBFixture : AspireFixture<Projects.AppHostB> { }



[ClassDataSource<AppAFixture>(Shared = SharedType.PerTestSession)]

public class AppATests(AppAFixture fixture) { /* ... */ }



[ClassDataSource<AppBFixture>(Shared = SharedType.PerTestSession)]

public class AppBTests(AppBFixture fixture) { /* ... */ }
```

### How do I skip waiting for tool containers?[​](#how-do-i-skip-waiting-for-tool-containers "Direct link to How do I skip waiting for tool containers?")

Tool containers like pgAdmin or RedisInsight don't need to be ready before tests run. If you want them to still run (e.g. for manual inspection), use `Named` wait behavior:

```
public class AppFixture : AspireFixture<Projects.MyAppHost>

{

    protected override ResourceWaitBehavior WaitBehavior => ResourceWaitBehavior.Named;



    protected override IEnumerable<string> ResourcesToWaitFor()

        => ["apiservice", "worker", "postgres", "redis"];

    // pgadmin, redisinsight are excluded — tests don't need them

}
```

If you don't need them to run at all during tests, remove them entirely instead:

```
public class AppFixture : AspireFixture<Projects.MyAppHost>

{

    protected override IEnumerable<string> ResourcesToRemove()

        => ["pgadmin", "redisinsight"];

}
```

### My resource never becomes healthy[​](#my-resource-never-becomes-healthy "Direct link to My resource never becomes healthy")

If a resource stays in `Running` but never reaches `Healthy`, check:

1. The resource has a health check configured (`.WithHttpHealthCheck("/health")` or similar)
2. The health check endpoint is reachable from inside the container network
3. Use `WatchResourceLogs("resourceName")` in a test to see the resource's output

If the resource doesn't have health checks, use `AllRunning` instead of `AllHealthy`:

```
protected override ResourceWaitBehavior WaitBehavior => ResourceWaitBehavior.AllRunning;
```

### What's the difference between TUnit.Aspire and TUnit.AspNetCore?[​](#whats-the-difference-between-tunitaspire-and-tunitaspnetcore "Direct link to What's the difference between TUnit.Aspire and TUnit.AspNetCore?")

|                    | TUnit.Aspire                                               | TUnit.AspNetCore                          |
| ------------------ | ---------------------------------------------------------- | ----------------------------------------- |
| **Purpose**        | Test distributed apps (multiple services + infrastructure) | Test a single ASP.NET Core app            |
| **Infrastructure** | Real containers via Aspire/Docker                          | In-process `TestServer` or Testcontainers |
| **Isolation**      | Shared app, per-test HTTP clients                          | Per-test `WebApplicationFactory`          |
| **Use when**       | Your app uses Aspire orchestration                         | Your app is a single ASP.NET Core project |

They can be used together — for example, using Aspire to manage infrastructure while using `TestWebApplicationFactory` for per-test app isolation.
