using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.OpenTelemetry.Tests;

[NotInParallel("TUnitOpenTelemetryGlobalState")] // these tests mutate process-wide AutoStart + env vars + TUnitOpenTelemetry configurators
public class AutoStartTests
{
    [Test]
    public async Task AutoStart_RegistersListenerForTUnitSource()
    {
        // AutoStart.Start fires via [Before(TestDiscovery)] before this test runs.
        using var source = new ActivitySource("TUnit");
        await Assert.That(source.HasListeners()).IsTrue();
    }

    [Test]
    public async Task AutoStart_SkipsIfUserAlreadyAttachedListener()
    {
        AutoStart.Stop();

        using var userListener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "TUnit",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(userListener);

        AutoStart.StartForTesting(resetFirst: true);
        try
        {
            await Assert.That(AutoStart.HasProviderForTesting).IsFalse();
        }
        finally
        {
            // Leave AutoStart re-armed for subsequent tests (in this class and the runner
            // at large) since the session-level AutoStart already disposed once.
            AutoStart.StartForTesting(resetFirst: true);
        }
    }

    [Test]
    public async Task AutoStart_ForceOn_BuildsEvenWhenListenerPresent()
    {
        var original = Environment.GetEnvironmentVariable("TUNIT_OTEL_AUTOSTART");
        Environment.SetEnvironmentVariable("TUNIT_OTEL_AUTOSTART", "1");
        try
        {
            using var userListener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "TUnit",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(userListener);

            TUnitOpenTelemetry.ResetForTests();
            TUnitOpenTelemetry.Configure(b => b.AddInMemoryExporter(new List<Activity>()));

            AutoStart.StartForTesting(resetFirst: true);
            await Assert.That(AutoStart.HasProviderForTesting).IsTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("TUNIT_OTEL_AUTOSTART", original);
            TUnitOpenTelemetry.ResetForTests();
            AutoStart.StartForTesting(resetFirst: true);
        }
    }

    [Test]
    public async Task Start_NoEndpointNoConfig_DoesNotBuildProvider()
    {
        var endpointOriginal = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        var autostartOriginal = Environment.GetEnvironmentVariable("TUNIT_OTEL_AUTOSTART");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
        Environment.SetEnvironmentVariable("TUNIT_OTEL_AUTOSTART", null);
        TUnitOpenTelemetry.ResetForTests();
        try
        {
            AutoStart.StartForTesting(resetFirst: true);
            await Assert.That(AutoStart.HasProviderForTesting).IsFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", endpointOriginal);
            Environment.SetEnvironmentVariable("TUNIT_OTEL_AUTOSTART", autostartOriginal);
            AutoStart.StartForTesting(resetFirst: true);
        }
    }

    [Test]
    public async Task Start_WithOptOutEnv_DoesNotBuildProvider()
    {
        var original = Environment.GetEnvironmentVariable("TUNIT_OTEL_AUTOSTART");
        Environment.SetEnvironmentVariable("TUNIT_OTEL_AUTOSTART", "0");
        try
        {
            AutoStart.StartForTesting(resetFirst: true);
            await Assert.That(AutoStart.HasProviderForTesting).IsFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("TUNIT_OTEL_AUTOSTART", original);
            // Re-arm for subsequent tests in the runner
            AutoStart.StartForTesting(resetFirst: true);
        }
    }

    [Test]
    public async Task Start_SetsDefaultServiceName()
    {
        var autostartOriginal = Environment.GetEnvironmentVariable("TUNIT_OTEL_AUTOSTART");
        Environment.SetEnvironmentVariable("TUNIT_OTEL_AUTOSTART", "1");
        TUnitOpenTelemetry.ResetForTests();
        TUnitOpenTelemetry.Configure(b => b.AddInMemoryExporter(new List<Activity>()));
        try
        {
            AutoStart.StartForTesting(resetFirst: true);

            var resource = AutoStart.GetResourceForTesting();
            await Assert.That(resource).IsNotNull();
            var serviceNameAttr = resource!.Attributes.FirstOrDefault(a => a.Key == "service.name");
            await Assert.That(serviceNameAttr.Key).IsEqualTo("service.name");
            var serviceName = serviceNameAttr.Value?.ToString();
            // Not the OTel default placeholder ("unknown_service" or "unknown_service:<process>")
            await Assert.That(serviceName).IsNotNull();
            await Assert.That(serviceName!.StartsWith("unknown_service", StringComparison.Ordinal)).IsFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("TUNIT_OTEL_AUTOSTART", autostartOriginal);
            TUnitOpenTelemetry.ResetForTests();
            AutoStart.StartForTesting(resetFirst: true);
        }
    }
}
