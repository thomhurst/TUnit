---
sidebar_position: 20
---

# Distributed Tracing

This page is for users wiring TUnit up to a tracing backend like Seq, Jaeger, Tempo, or the Aspire dashboard. If you just want a working setup, start with [OpenTelemetry Tracing](/docs/examples/opentelemetry). If you're hitting problems, jump straight to its [Troubleshooting](/docs/examples/opentelemetry#troubleshooting) section.

:::note
Distributed tracing requires .NET 8 or later.
:::

## What TUnit emits

Every test produces:

- A **test case** span (the root of the test's trace) — gets a fresh trace ID.
- A **test body** span underneath it.
- Any spans your code or libraries (HttpClient, ASP.NET Core, EF Core, etc.) create — automatically nested under the test body.

Separately, TUnit also emits **lifecycle spans** for the run as a whole (test session, assembly, suite). These are on a different source so backends don't mix them with per-test data.

```text
test case (one per test, own trace ID)
  └── test body
        └── HttpClient / EF Core / your code
```

```text
test session
  ├── test discovery
  └── test assembly
        └── test suite (one per class)
              └── shared setup / teardown / hooks
```

The two sources you usually subscribe to:

| Source name | What's in it |
|-------------|--------------|
| `TUnit` | The test case + test body spans |
| `TUnit.Lifecycle` | Session, discovery, assembly, suite, shared setup/teardown |

## Backend setup

The general setup in [OpenTelemetry Tracing](/docs/examples/opentelemetry#setup) works everywhere. Backend-specific notes follow.

:::tip Zero-config setup
Install [`TUnit.OpenTelemetry`](/docs/examples/opentelemetry#option-a-zero-config-tunitopentelemetry) and set `OTEL_EXPORTER_OTLP_ENDPOINT`. The package auto-wires a `TracerProvider` at test discovery, pre-registers `TUnitTestCorrelationProcessor`, and flushes at session end. Call `TUnitOpenTelemetry.Configure(...)` to add exporters or processors.
:::

### Seq

Point the OTLP exporter at Seq's ingestion endpoint:

```csharp
.AddOtlpExporter(opts =>
{
    opts.Endpoint = new Uri("http://localhost:5341/ingest/otlp/v1/traces");
    opts.Protocol = OtlpExportProtocol.HttpProtobuf;
    opts.Headers = "X-Seq-ApiKey=your-key";
})
```

Useful Seq queries:

```text
tunit.session.id = '<id>'                  -- one full test run
tunit.test.class = 'MyTests'               -- one class
tunit.test.id = '<id>'                     -- one specific test invocation
test.case.result.status = 'fail'           -- only failures
```

### Jaeger or Tempo

```csharp
.AddOtlpExporter(opts => opts.Endpoint = new Uri("http://localhost:4317"))
```

Jaeger groups by trace ID, so each test appears as a separate trace. Use the tag search box (`tunit.session.id="<id>"`) to find all traces from one run.

### Aspire dashboard

Aspire is wired up automatically through `TUnit.Aspire`. See [Aspire integration](/docs/examples/aspire). The dashboard understands the link between `test case` and `test suite` spans, so it groups them naturally.

### Other backends (Honeycomb, Datadog, etc.)

Use the OTLP exporter pointed at your vendor endpoint and set `OTEL_EXPORTER_OTLP_HEADERS` for auth. No TUnit-specific config needed.

## How to find spans for a test

Every TUnit-emitted span carries these tags. Use them in your backend's search UI:

| Tag | What it identifies |
|-----|--------------------|
| `tunit.test.id` | One specific test invocation (one retry attempt) |
| `tunit.test.node_uid` | All retry attempts of the same logical test |
| `tunit.session.id` | One whole test run |
| `tunit.test.class` | All tests in a class |
| `tunit.assembly.name` | All tests in an assembly |

For cross-process correlation (your test calling your SUT), use `tunit.test.id`. It's the most reliable — see [Limitations](#limitations) below for why trace IDs alone aren't always enough.

## When tracing across processes

Cross-process baggage propagation (e.g. `tunit.test.id` reaching your SUT) depends on both sides using the W3C `baggage` header rather than .NET's default `Correlation-Context`.

TUnit handles this automatically: a module initializer in `TUnit.Core` replaces the default `DistributedContextPropagator.LegacyPropagator` with `DistributedContextPropagator.CreateW3CPropagator()`. Any custom propagator you set yourself is left alone. If you want to retain the legacy behavior, set `TUNIT_KEEP_LEGACY_PROPAGATOR=1`.

For the SUT side, if it shares the test process (e.g. `TestWebApplicationFactory<T>`), alignment flows automatically. For out-of-process SUTs that don't reference `TUnit.Core`, align the propagator yourself on startup — either match `DistributedContextPropagator.Current` or, if you use the OpenTelemetry SDK:

```csharp
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(
[
    new TraceContextPropagator(),
    new BaggagePropagator(),
]));
```

## Limitations

### Static `ActivitySource` in third-party libraries

Some libraries (message brokers like DotPulsar, EF providers, connection pools) hold a `static` `ActivitySource` and emit spans from background threads. Those threads may have captured the wrong test's context, so the spans end up under the wrong trace.

**You can't fix the parent chain after the fact.** What works:

- Add the [`TUnitTagProcessor`](/docs/examples/opentelemetry#spans-from-test-a-are-showing-up-under-test-b) so spans always carry `tunit.test.id` even when the trace ID is wrong, then filter by tag.
- For hosted services inside `TestWebApplicationFactory<T>`, this leak is auto-mitigated — each `IHostedService.StartAsync` runs under `ExecutionContext.SuppressFlow()`, so background tasks it spawns capture a clean context. Override `SuppressHostedServiceExecutionContextFlow` and return `false` to opt out. Third-party `ActivitySource` instances captured at class-load time remain a residual concern.

### `WebApplicationFactory` without TUnit's wrapper

The vanilla `WebApplicationFactory<T>` returns an `HttpClient` that skips .NET's HTTP tracing. No `traceparent` is injected and the server starts a fresh trace.

Use [`TestWebApplicationFactory<T>`](/docs/examples/aspnet) or wrap with `TracedWebApplicationFactory<T>`. The `TUnit0064` analyzer raises a warning (with a code fix) when a class inherits directly from `WebApplicationFactory<T>`.

### `IHttpClientFactory` clients in the SUT

`TestWebApplicationFactory<T>` auto-registers an `IHttpMessageHandlerBuilderFilter` that prepends the TUnit tracing and test-id handlers to every `IHttpClientFactory` pipeline built in the SUT. Outbound calls from `AddHttpClient<T>()`, named clients, and typed clients all carry `traceparent`, `baggage`, and `X-TUnit-TestId` automatically — no manual `.AddHttpMessageHandler<>()` wiring required.

Opt out per-test when the SUT already instruments its own outbound HTTP (for example via the OpenTelemetry HttpClient instrumentation) by setting `WebApplicationTestOptions.AutoPropagateHttpClientFactory = false`:

```csharp
protected override void ConfigureTestOptions(WebApplicationTestOptions options)
{
    options.AutoPropagateHttpClientFactory = false;
}
```

### SUT-side OpenTelemetry wiring

`TestWebApplicationFactory<T>` also augments the SUT's `TracerProvider` automatically — the `TUnit.AspNetCore.Http` activity source, the `TUnitTestCorrelationProcessor`, and ASP.NET Core + HttpClient instrumentation are layered on top of whatever `AddOpenTelemetry().WithTracing(...)` wiring the SUT already has. That means spans emitted inside the SUT stay queryable per-test (`tunit.test.id` tag) even when third-party libraries break the parent chain.

Opt out per-test with `WebApplicationTestOptions.AutoConfigureOpenTelemetry = false` when the SUT owns its own processors and you don't want TUnit's defaults layered on top.

### Raw `HttpClient`

`new HttpClient()` can't be intercepted. Either route through `IHttpClientFactory` or set the `traceparent` header manually.

## HTML report vs OpenTelemetry backends

TUnit's HTML report and a backend like Seq render the same data differently:

| | HTML report | OpenTelemetry backend |
|--|-------------|------------------------|
| Hierarchy | Folds each test under its class using span links | Each test is a separate trace |
| Filtering | Built-in UI controls | Backend query language |
| Cross-service spans | Only what the engine sees in-process | Everything every exporter sends in |
| Persistence | One file per run | Long-term, queryable across runs |

Use the HTML report for debugging a single run. Use a backend for run-over-run analysis and cross-service correlation.

## Related pages

- [OpenTelemetry Tracing](/docs/examples/opentelemetry) — first-time setup, full attribute reference, troubleshooting.
- [ASP.NET Core Integration Testing](/docs/examples/aspnet) — `TestWebApplicationFactory<T>` setup.
- [HTML Test Report — Distributed Tracing](/docs/guides/html-report#distributed-tracing) — how the HTML report renders the data.
- [Aspire integration](/docs/examples/aspire) — Aspire dashboard and OTLP receiver.
