using System.Diagnostics;
using OpenTelemetry;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.OpenTelemetry.Tests;

public class CorrelationProcessorTests
{
    [Test]
    public async Task Processor_CopiesBaggageToTag()
    {
        using var listener = AttachPermissiveListener("CorrelationProcessorTests.Copies");
        var processor = new TUnitTestCorrelationProcessor();

        using var parent = new Activity("parent").Start();
        parent.AddBaggage("tunit.test.id", "test-123");

        using var child = new ActivitySource("CorrelationProcessorTests.Copies").StartActivity("child")!;
        processor.OnStart(child);

        await Assert.That(child.GetTagItem("tunit.test.id")).IsEqualTo("test-123");
    }

    [Test]
    public async Task Processor_SkipsWhenAlreadyTagged()
    {
        using var listener = AttachPermissiveListener("CorrelationProcessorTests.Skips");
        var processor = new TUnitTestCorrelationProcessor();

        using var parent = new Activity("parent").Start();
        parent.AddBaggage("tunit.test.id", "from-baggage");

        using var child = new ActivitySource("CorrelationProcessorTests.Skips").StartActivity("child")!;
        child.SetTag("tunit.test.id", "already-set");
        processor.OnStart(child);

        await Assert.That(child.GetTagItem("tunit.test.id")).IsEqualTo("already-set");
    }

    [Test]
    public async Task Processor_NoOp_WhenNoBaggage()
    {
        using var listener = AttachPermissiveListener("CorrelationProcessorTests.NoBaggage");
        var processor = new TUnitTestCorrelationProcessor();

        // Suppress the ambient Activity.Current (which the TUnit test runner has set with
        // tunit.test.id baggage) so we can exercise the "no baggage" code path in isolation.
        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            using var child = new ActivitySource("CorrelationProcessorTests.NoBaggage").StartActivity("child")!;
            processor.OnStart(child);

            await Assert.That(child.GetTagItem("tunit.test.id")).IsNull();
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    private static ActivityListener AttachPermissiveListener(string sourceName)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
