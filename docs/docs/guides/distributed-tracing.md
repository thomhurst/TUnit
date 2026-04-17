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

If your test process and your SUT are different processes (or you're using `WebApplicationFactory` heavily), make sure both sides agree on the propagator:

```csharp
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(
[
    new TraceContextPropagator(),
    new BaggagePropagator(),
]));
```

Without this, .NET's default propagator emits `Correlation-Context`, but the OpenTelemetry SDK only reads W3C `baggage`. The mismatch silently drops baggage and you lose `tunit.test.id` on the SUT side.

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

Outbound HTTP calls the SUT itself makes (e.g. to a downstream service) are not auto-instrumented yet. Add the handler manually:

```csharp
services.AddHttpClient<IDownstreamApi, DownstreamApi>()
    .AddHttpMessageHandler<ActivityPropagationHandler>();
```

Tracking automation: [#5590](https://github.com/thomhurst/TUnit/issues/5590).

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
