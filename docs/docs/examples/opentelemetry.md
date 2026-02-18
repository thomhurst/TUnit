# OpenTelemetry Tracing

TUnit emits `System.Diagnostics.Activity` trace spans at every level of the test lifecycle. When you configure an OpenTelemetry exporter (or any `ActivityListener`), you get distributed tracing for your test runs automatically. When no listener is attached, the cost is zero.

:::note
Activity tracing requires .NET 8 or later. It is not available on .NET Framework or .NET Standard targets.
:::

## Setup

Add the OpenTelemetry packages to your test project:

```bash
dotnet add package OpenTelemetry
dotnet add package OpenTelemetry.Exporter.Console
```

Then subscribe to the `"TUnit"` ActivitySource in your test setup:

```csharp
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

public class TraceSetup
{
    private static TracerProvider? _tracerProvider;

    [Before(TestSession)]
    public static void SetupTracing()
    {
        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyTests"))
            .AddSource("TUnit")
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

## Span Hierarchy

TUnit creates a nested span tree that mirrors the test lifecycle:

```
test session
  └── test assembly
        └── test suite (one per test class)
              └── test case (one per test method invocation)
```

## Attributes

Each span carries tags that follow [OpenTelemetry semantic conventions](https://opentelemetry.io/docs/specs/semconv/) where applicable.

### Standard OTel Attributes

| Attribute | Span | Description |
|-----------|------|-------------|
| `test.case.name` | test case | Test method name |
| `test.case.result.status` | test case | `pass`, `fail`, or `skipped` |
| `test.suite.name` | test suite | Test class name |
| `error.type` | test case | Exception type (on failure) |
| `exception.type` | test case | Exception type (on exception event) |
| `exception.message` | test case | Exception message (on exception event) |
| `exception.stacktrace` | test case | Full stack trace (on exception event) |

### TUnit-Specific Attributes

| Attribute | Span | Description |
|-----------|------|-------------|
| `tunit.session.id` | test session | Unique session identifier |
| `tunit.filter` | test session | Active test filter expression |
| `tunit.assembly.name` | test assembly | Assembly name |
| `tunit.class.namespace` | test suite | Class namespace |
| `tunit.test.class` | test case | Fully qualified class name |
| `tunit.test.method` | test case | Method name |
| `tunit.test.id` | test case | Unique test instance ID |
| `tunit.test.categories` | test case | Test categories (string array) |
| `tunit.test.count` | session/assembly/suite | Total test count |
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

Swap the exporter in the setup:

```csharp
// OTLP (works with Jaeger, Tempo, Honeycomb, etc.)
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol

.AddOtlpExporter(opts => opts.Endpoint = new Uri("http://localhost:4317"))

// Zipkin
dotnet add package OpenTelemetry.Exporter.Zipkin

.AddZipkinExporter(opts => opts.Endpoint = new Uri("http://localhost:9411/api/v2/spans"))
```
