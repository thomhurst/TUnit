using TUnit.Aspire.Telemetry;
using TUnit.Core;

namespace TUnit.Aspire;

/// <summary>
/// Starts and stops the Aspire runner trace exporter for the current test session.
/// This keeps per-test TUnit spans visible in external OTLP backends alongside SUT spans.
/// </summary>
public static class AspireTelemetryHooks
{
    [Before(HookType.TestSession, Order = int.MaxValue)]
    public static void StartRunnerTraceExport(TestSessionContext context)
    {
        TestTraceExporter.TryStart(context);
    }

    [After(HookType.TestSession, Order = int.MaxValue)]
    public static void StopRunnerTraceExport()
    {
        TestTraceExporter.Dispose();
    }
}
