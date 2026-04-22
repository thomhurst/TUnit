# OpenTelemetry Tracing

TUnit emits `System.Diagnostics.Activity` trace spans at every level of the test lifecycle. When you configure an OpenTelemetry exporter (or any `ActivityListener`), you get distributed tracing for your test runs automatically. When no listener is attached, the cost is zero.

:::note
Activity tracing requires .NET 8 or later. It is not available on .NET Framework or .NET Standard targets.
:::

## Setup

### Option A: Zero-config (`TUnit.OpenTelemetry`)

Install the meta-package. OTLP is already bundled — no other OpenTelemetry packages needed for the common case:

```bash
dotnet add package TUnit.OpenTelemetry
```

Point it at a backend:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

That's it. The package auto-wires a `TracerProvider` at `[Before(TestDiscovery)]` (subscribed to `TUnit` and `TUnit.Lifecycle`), includes a pre-registered `TUnitTestCorrelationProcessor`, and disposes the provider at `[After(TestSession)]`.

#### Customizing (add exporters, processors, resources)

Call `TUnitOpenTelemetry.Configure` from any `[Before(TestDiscovery)]` hook with `Order` less than `int.MaxValue`:

```csharp
using OpenTelemetry.Trace;
using TUnit.OpenTelemetry;

public static class TraceCustomization
{
    [Before(TestDiscovery)]
    public static void Configure()
    {
        TUnitOpenTelemetry.Configure(builder => builder
            .AddConsoleExporter()
            .AddProcessor(new MyCustomProcessor()));
    }
}
```

Each callback is applied in registration order after the package's defaults, so you can override the resource or add additional exporters.

#### Environment switches

| Variable | Behavior |
|----------|----------|
| `TUNIT_OTEL_AUTOSTART=0` | Opt out — package does nothing, user hooks run as normal |
| `TUNIT_OTEL_AUTOSTART=1` | Force on — build the provider even if another listener is already attached |
| `OTEL_SERVICE_NAME` | Override `service.name` (defaults to the entry assembly name) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Enables OTLP export when set; when unset, the package stays dormant unless `Configure` is called |

#### Coexistence with hand-rolled setup

If you already register your own `TracerProvider` or `ActivityListener` in a `[Before(TestDiscovery)]` hook, the package detects the attached listener and stays out of the way — no duplicate spans. `Configure(...)` applies only to the package's provider; do not mix it with a separately built `Sdk.CreateTracerProviderBuilder()` in the same project. Pick one.

### Option B: Manual (full control)

Add the OpenTelemetry packages to your test project:

```bash
dotnet add package OpenTelemetry
dotnet add package OpenTelemetry.Exporter.Console
```

Then subscribe to the `"TUnit"` ActivitySource in a `[Before(TestDiscovery)]` hook:

```csharp
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

public class TraceSetup
{
    private static TracerProvider? _tracerProvider;

    [Before(TestDiscovery)]
    public static void SetupTracing()
    {
        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyTests"))
            .AddSource("TUnit")
            // Optional: export runner lifecycle traces (discovery, session,
            // assembly, suite, and shared setup/teardown) as a separate source.
            .AddSource("TUnit.Lifecycle")
            .AddConsoleExporter()
            .Build();
    }

    [After(TestSession)]
    public static void TeardownTracing()
    {
        _tracerProvider?.Dispose();
    }
}
```

Replace `AddConsoleExporter()` with your preferred exporter (Jaeger, Zipkin, OTLP, etc.).
Use one stable service name for the test runner (for example, `MyTests`) rather than a different
`service.name` per test. Individual tests are already distinguished by their own trace IDs and
TUnit tags such as `tunit.test.id`.

If you add `TUnitTestCorrelationProcessor` for cross-boundary tagging, register it **before** any synchronous exporter (`SimpleExportProcessor`-based). The built-in processor tags at both `OnStart` and `OnEnd`, so a `SimpleExport`-wrapped exporter that runs first would serialize the activity before the tag is applied. `BatchExportProcessor` (the default for OTLP/Jaeger/Zipkin) defers serialization, so order doesn't matter there.

### Option C: Raw `ActivityListener` (no SDK dependency)

If you don't want the OpenTelemetry SDK, you can subscribe directly with a `System.Diagnostics.ActivityListener`:

```csharp
using System.Diagnostics;

public class TraceSetup
{
    private static ActivityListener? _listener;

    [Before(TestDiscovery)]
    public static void SetupTracing()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is "TUnit" or "TUnit.Lifecycle",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => Console.WriteLine($"▶ {activity.OperationName}"),
            ActivityStopped = activity => Console.WriteLine($"■ {activity.OperationName} ({activity.Duration.TotalMilliseconds:F1}ms)")
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [After(TestSession)]
    public static void TeardownTracing()
    {
        _listener?.Dispose();
    }
}
```

### Why `[Before(TestDiscovery)]`?

The listener **must** be registered in a `[Before(TestDiscovery)]` hook so it is active before the discovery span begins. TUnit's hook execution order is:

1. `[Before(TestDiscovery)]` — register your listener here
2. **Test discovery** — the `"test discovery"` span is emitted here
3. `[Before(TestSession)]` — session-level setup
4. Test execution — assembly, suite, and test case spans are emitted
5. `[After(TestSession)]` — dispose your listener here

If you register the listener later (e.g., in `[Before(Assembly)]`), the discovery span will not be captured.

## Activity Sources

TUnit emits two sources:

- `"TUnit"` — per-test traces intended for exporters, log correlation, and backend navigation
- `"TUnit.Lifecycle"` — optional runner lifecycle traces for discovery, session/assembly/suite spans, and shared setup/teardown

The default backend-friendly trace shape is:

```text
TUnit
test case (root span per test invocation)
  └── test body
        └── HttpClient / ASP.NET Core / EF Core / custom spans
```

Optional lifecycle spans are emitted separately:

```text
TUnit.Lifecycle
test session
  ├── test discovery
  └── test assembly
        └── test suite (one per test class)
              └── shared setup / teardown / hooks
```

Each `test case` starts its own trace on purpose, so every test invocation gets a unique W3C
TraceId. That keeps downstream service spans and logs correlated to a single test in Seq, Jaeger,
Aspire, and similar backends. Parent-child relationships stay within the per-test trace; runner
lifecycle spans are separate because they describe the whole session/class rather than one test.

## Attributes

Each span carries tags that follow [OpenTelemetry semantic conventions](https://opentelemetry.io/docs/specs/semconv/) where applicable.

### Standard OTel Attributes

| Attribute | Span | Description |
|-----------|------|-------------|
| `test.case.name` | test case | Test method name |
| `test.case.result.status` | test case | `pass`, `fail`, or `skipped` |
| `test.suite.name` | test suite / test case | Test class name |
| `error.type` | test case | Exception type (on failure) |
| `exception.type` | test case | Exception type (on exception event) |
| `exception.message` | test case | Exception message (on exception event) |
| `exception.stacktrace` | test case | Full stack trace (on exception event) |

### TUnit-Specific Attributes

| Attribute | Span | Description |
|-----------|------|-------------|
| `tunit.session.id` | test session / test case | Unique session identifier |
| `tunit.filter` | test session | Active test filter expression |
| `tunit.assembly.name` | test assembly / test case | Assembly name |
| `tunit.class.namespace` | test suite / test case | Class namespace |
| `tunit.test.class` | test case | Fully qualified class name |
| `tunit.test.method` | test case | Method name |
| `tunit.test.id` | test case | Unique test instance ID |
| `tunit.test.categories` | test case | Test categories (string array) |
| `tunit.test.count` | session/assembly/suite/discovery | Total test count |
| `tunit.test.retry_attempt` | test case | Current retry attempt (when retrying) |
| `tunit.test.skip_reason` | test case | Reason the test was skipped |

## Span Status

Following OTel instrumentation conventions:

- **Passed tests**: status is left as `Unset` (the default — success is implicit)
- **Failed tests**: status is set to `Error` with an exception event recorded
- **Skipped tests**: status is left as `Unset` with `test.case.result.status` = `skipped`

## Retries

When a test is configured with `[Retry]`, each failed attempt produces its own span with `Error` status and the recorded exception. The retry attempt that finally passes (or the last failing attempt) is the final span for that test.

## Using with Jaeger, Zipkin, or OTLP

Swap the exporter in the setup code above. Each exporter needs its own NuGet package.

### OTLP (works with Jaeger, Tempo, Honeycomb, etc.)

```bash
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

```csharp
.AddOtlpExporter(opts => opts.Endpoint = new Uri("http://localhost:4317"))
```

### Zipkin

```bash
dotnet add package OpenTelemetry.Exporter.Zipkin
```

```csharp
.AddZipkinExporter(opts => opts.Endpoint = new Uri("http://localhost:9411/api/v2/spans"))
```

### ASP.NET Core Integration Tests

If you use `TestWebApplicationFactory` or `TracedWebApplicationFactory`, outgoing requests
automatically propagate the current test trace via W3C `traceparent` and `baggage` headers.

The factory also augments the SUT's `TracerProvider` automatically — no manual `services.AddOpenTelemetry().WithTracing(...)` wiring is needed for the basics:

- Adds the `TUnitTestCorrelationProcessor` so spans from libraries with broken parent chains are still tagged with `tunit.test.id`.
- Adds ASP.NET Core and HttpClient instrumentation.

Your own `WithTracing` callback on the SUT is preserved; TUnit's defaults are layered on top. If you configure your own exporter (OTLP, Jaeger, Zipkin, in-memory), test spans flow straight through it.

Set `WebApplicationTestOptions.AutoConfigureOpenTelemetry = false` per-test to opt out — useful if the SUT owns its own processors and you don't want TUnit's defaults layered on top.

## Test Context Correlation via Activity Baggage

TUnit stores the test context ID as Activity baggage (`tunit.test.id`) on each test case span. This enables automatic `TestContext.Current` resolution across service boundaries when Activity propagation is in place.

When a test triggers work in a shared service host (e.g., via HTTP, gRPC, or messaging), the `AsyncLocal<TestContext>` doesn't flow because the server processes requests on its own thread pool. However, if OpenTelemetry propagation is configured, `Activity.Current` **does** flow via W3C `traceparent`/`baggage` headers. TUnit uses this: when `TestContext.Current` finds no AsyncLocal value, it falls back to checking `Activity.Current` for the `tunit.test.id` baggage item and resolves the originating test context automatically.

This means console output, `ILogger` calls, and any code that reads `TestContext.Current` on the server side will automatically correlate to the correct test — with no manual setup beyond enabling OpenTelemetry propagation.

```csharp
// No extra code needed — just having OTel configured is sufficient.
// The test's Activity carries tunit.test.id baggage, which propagates
// automatically via W3C headers to OTel-instrumented services.

[Test]
public async Task Api_Returns_Expected_Result()
{
    // Activity baggage propagates via HTTP headers automatically
    var response = await _httpClient.GetAsync("/api/data");

    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
}
```

For scenarios without OpenTelemetry, see [Cross-Thread Output Correlation](/docs/extending/logging#cross-thread-output-correlation) for the manual `TestContext.MakeCurrent()` approach.

## Troubleshooting

Find your symptom below.

### "I see one trace per test instead of one trace per class"

That's expected. Each test gets its own trace ID so spans, logs, and exports stay tied to a single test run. To group across tests, search by tag in your backend:

| Want to see... | Search for |
|----------------|-----------|
| One specific test run | `tunit.test.id = "<id>"` |
| All retries of one test | `tunit.test.node_uid = "<uid>"` |
| Everything from one test session | `tunit.session.id = "<id>"` |
| All tests in a class | `tunit.test.class = "MyNamespace.MyTests"` |

In Seq use `tunit.session.id = '<id>'`. In Jaeger or Tempo use the tag filter box.

### "Spans from test A are showing up under test B"

Usually a background worker (a hosted service, a message broker like DotPulsar, or a connection pool) started during one test and kept running. Anything it produces inherits whichever test was current when it started.

**Quickest fix**: tag every span with the current test's ID so you can still filter even when the parent is wrong.

If you installed `TUnit.OpenTelemetry` (Option A), `TUnitTestCorrelationProcessor` is already registered for you — no additional setup.

For manual setups, add this processor to your tracer builder:

```csharp
using System.Diagnostics;
using OpenTelemetry;

public sealed class TUnitTagProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        var testId = Activity.Current?.GetBaggageItem("tunit.test.id");
        if (testId is not null)
        {
            activity.SetTag("tunit.test.id", testId);
        }
    }
}

// then in your tracer builder:
.AddProcessor(new TUnitTagProcessor())
```

Register the correlation processor **before** any synchronous exporter (`SimpleExportProcessor`-based). The built-in `TUnitTestCorrelationProcessor` tags at both `OnStart` and `OnEnd`, and a `SimpleExport`-wrapped exporter that runs first would serialize the activity before the tag is applied. `BatchExportProcessor` (the default for OTLP/Jaeger/Zipkin) defers serialization, so order doesn't matter there.

Now you can filter by `tunit.test.id` in your backend even when the trace hierarchy is wrong.

**Better fix** if you control the worker: stop it from capturing the test's context in the first place.

```csharp
using (ExecutionContext.SuppressFlow())
{
    _ = Task.Run(BackgroundLoopAsync);
}
```

For `IHostedService` registrations inside ASP.NET Core integration tests, `TestWebApplicationFactory<T>` does this automatically — every registered hosted service has its `StartAsync` wrapped in `ExecutionContext.SuppressFlow()`, so background tasks it spawns capture a clean context. Override `SuppressHostedServiceExecutionContextFlow` and return `false` to opt out if you intentionally rely on `Activity.Current` flowing into a hosted service.

**Last resort**: run affected tests one at a time with `[NotInParallel]`.

### "My SUT spans show no parent / appear orphaned"

Two common causes.

**1. The parent span isn't exported to the same backend.** The test-side `test case` span lives in the test process. If you only export from the SUT, the backend sees a child whose parent it has never seen. Either export the `"TUnit"` source from the test process too, or rely on the `tunit.test.id` tag (above) instead of trace hierarchy.

**2. The two processes use different baggage formats.** .NET defaults to `Correlation-Context`. The OpenTelemetry SDK reads W3C `baggage`. TUnit auto-aligns `DistributedContextPropagator.Current` to W3C on module load, and `TestWebApplicationFactory<T>` re-applies this for in-process SUTs via an `IStartupFilter` — no manual wiring needed. Set `TUNIT_KEEP_LEGACY_PROPAGATOR=1` to opt out.

For an **out-of-process** SUT that doesn't reference `TUnit.Core`, you still need to align it yourself:

```csharp
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(
[
    new TraceContextPropagator(),
    new BaggagePropagator(),
]));
```

### "My HTTP calls don't carry the test trace"

If you inherit from `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<T>` directly, the `HttpClient` it returns skips .NET's normal HTTP tracing. No `traceparent` header is sent, so the server starts a fresh trace.

Switch your factory to `TestWebApplicationFactory<T>`:

```csharp
public class MyFactory : TestWebApplicationFactory<Program> { }
```

Or, if you can't change the inheritance, wrap your existing factory:

```csharp
var traced = new TracedWebApplicationFactory<Program>(myExistingFactory);
var client = traced.CreateClient();
```

Both attach the trace propagation handler automatically. See [ASP.NET Core integration](./aspnet.md) for full setup.

Outbound HTTP calls the SUT itself makes through `IHttpClientFactory` (`AddHttpClient<T>()`, named clients, typed clients) are also auto-instrumented by `TestWebApplicationFactory<T>`. Opt out per-test via `WebApplicationTestOptions.AutoPropagateHttpClientFactory = false` when the SUT already owns its outbound tracing.

### "No spans show up in my exporter at all"

Check in order:

1. Did you register the listener in `[Before(TestDiscovery)]`? `[Before(Test)]` or `[Before(Class)]` is too late.
2. Did you call `.AddSource("TUnit")` (and `"TUnit.Lifecycle"` if you want runner spans)? Each source has to be added explicitly.
3. Did you dispose the `TracerProvider` in `[After(TestSession)]`? Without disposal, buffered spans never get flushed.

## HTML Report Integration

TUnit's built-in [HTML test report](/docs/guides/html-report) automatically captures activity spans and renders them as trace timelines — no OpenTelemetry SDK required. The report also captures spans from instrumented libraries like HttpClient, ASP.NET Core, and EF Core when they execute within a test's context.

For details on distributed trace collection, linking external traces, and accessing the test's `Activity`, see the [Distributed Tracing](/docs/guides/html-report#distributed-tracing) section of the HTML report guide.
