using System.Diagnostics;
using OpenTelemetry.Trace;
using TUnit.Core;

namespace TUnit.OpenTelemetry.Tests;

public static class TestSessionSetup
{
    [Before(HookType.TestDiscovery, Order = int.MinValue)]
    public static void RegisterDummyConfigurator()
    {
        // Without this, AutoStart.Start() returns early because no endpoint + no user config.
        // A no-op configurator is enough to keep the AutoStart path live for the coexistence tests.
        TUnitOpenTelemetry.Configure(_ => { });
    }
}
