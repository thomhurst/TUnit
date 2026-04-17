using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.OpenTelemetry.Tests;

[NotInParallel("TUnitOpenTelemetryGlobalState")] // mutates process-wide AutoStart + TUnitOpenTelemetry configurators
public class EndToEndTests
{
    [Test]
    public async Task TUnitSource_Spans_AreExported()
    {
        var autostartOriginal = Environment.GetEnvironmentVariable("TUNIT_OTEL_AUTOSTART");
        // Force-build the provider even if a prior listener is still attached so the
        // InMemory exporter is guaranteed to receive our spans.
        Environment.SetEnvironmentVariable("TUNIT_OTEL_AUTOSTART", "1");

        var spans = new List<Activity>();
        TUnitOpenTelemetry.ResetForTests();
        TUnitOpenTelemetry.Configure(b => b.AddInMemoryExporter(spans));
        AutoStart.StartForTesting(resetFirst: true);

        // Suppress the ambient Activity.Current (the TUnit runner sets tunit.test.id baggage
        // on it) so the child span we create is independent of the outer test context.
        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            using (var source = new ActivitySource("TUnit"))
            using (var activity = source.StartActivity("e2e-probe"))
            {
                activity?.SetTag("probe", "value");
            }

            AutoStart.Stop();

            var probe = spans.Single(s => s.OperationName == "e2e-probe");
            var probeTag = probe.TagObjects.FirstOrDefault(t => t.Key == "probe").Value?.ToString();
            await Assert.That(probeTag).IsEqualTo("value");
        }
        finally
        {
            Activity.Current = previous;
            Environment.SetEnvironmentVariable("TUNIT_OTEL_AUTOSTART", autostartOriginal);
            // Re-arm for subsequent tests in this class and the runner at large.
            TUnitOpenTelemetry.ResetForTests();
            AutoStart.StartForTesting(resetFirst: true);
        }
    }
}
