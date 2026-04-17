using TUnit.Aspire.Telemetry;
using TUnit.Core;
using TUnit.OpenTelemetry;

namespace TUnit.Aspire;

/// <summary>
/// Registers a Configure callback on <see cref="TUnitOpenTelemetry"/> so that the shared
/// auto-wired TracerProvider exports spans to the Aspire dashboard's OTLP endpoint when
/// <c>DOTNET_DASHBOARD_OTLP_ENDPOINT_URL</c> is present.
/// </summary>
public static class AspireTelemetryHooks
{
    [Before(HookType.TestDiscovery, Order = AutoStart.AutoStartOrder - 1)]
    public static void RegisterAspireExporter(TestSessionContext context)
    {
        var endpoint = TestTraceExporter.TryGetDashboardEndpoint();
        if (endpoint is null)
        {
            return;
        }

        TUnitOpenTelemetry.Configure(builder =>
            TestTraceExporter.AddToBuilder(builder, context, endpoint));
    }
}
