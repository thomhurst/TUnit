using System.Diagnostics;
using OpenTelemetry;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

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
    public async Task Processor_TagsOnEnd_WhenBaggageAddedAfterStart()
    {
        using var listener = AttachPermissiveListener("CorrelationProcessorTests.DeferredBaggage");
        var processor = new TUnitTestCorrelationProcessor();

        // Simulates server activities whose baggage is populated by the propagator
        // after ActivitySource.StartActivity returns (e.g. ASP.NET Core Hosting).
        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            using var child = new ActivitySource("CorrelationProcessorTests.DeferredBaggage").StartActivity("child")!;
            processor.OnStart(child);
            await Assert.That(child.GetTagItem("tunit.test.id")).IsNull();

            child.AddBaggage("tunit.test.id", "from-propagator");
            processor.OnEnd(child);

            await Assert.That(child.GetTagItem("tunit.test.id")).IsEqualTo("from-propagator");
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    [Test]
    public async Task Processor_FallsBackToTraceRegistry_WhenActivityHasNoBaggage()
    {
        using var listener = AttachPermissiveListener("CorrelationProcessorTests.TraceRegistryFallback");
        var processor = new TUnitTestCorrelationProcessor();

        // Simulates a span that outlives the test's async context: no baggage on the
        // activity, Activity.Current belongs to another test, but the trace ID is
        // still registered against the originating test.
        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            using var child = new ActivitySource("CorrelationProcessorTests.TraceRegistryFallback").StartActivity("child")!;
            var traceId = child.TraceId.ToString();
            // Additive registration — child.TraceId is random, so it can't collide with
            // registrations from other concurrent tests, and entries live until session end.
            TraceRegistry.Register(traceId, testNodeUid: "node-42", contextId: "ctx-42");
            processor.OnEnd(child);

            await Assert.That(child.GetTagItem("tunit.test.id")).IsEqualTo("ctx-42");
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    [Test]
    public async Task ScopedProcessor_DoesNotTag_ActivitiesFromOtherFactories()
    {
        // Regression for the OptOut_DoesNotTag_AspNetCoreSpans cross-factory leak:
        // factory A's processor must skip activities that factory B's request pipeline
        // tagged with B's factory id, even when the TraceRegistry fallback would happily
        // resolve a TestId from somewhere.
        using var listener = AttachPermissiveListener("CorrelationProcessorTests.ScopedAlien");
        var scopeA = new CorrelationScope();
        var scopeB = new CorrelationScope();
        var processorA = new TUnitTestCorrelationProcessor(scopeA);

        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            using var alien = new ActivitySource("CorrelationProcessorTests.ScopedAlien").StartActivity("alien")!;
            alien.AddBaggage(CorrelationScope.FactoryIdBaggageKey, scopeB.FactoryId);
            // A trace registry hit would otherwise satisfy the existing fallback path —
            // include it so the test proves the per-factory guard is what blocks tagging.
            TraceRegistry.Register(alien.TraceId.ToString(), testNodeUid: "node-x", contextId: "ctx-x");

            processorA.OnEnd(alien);

            await Assert.That(alien.GetTagItem("tunit.test.id")).IsNull();
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    [Test]
    public async Task ScopedProcessor_Tags_ActivitiesFromOwnFactory()
    {
        using var listener = AttachPermissiveListener("CorrelationProcessorTests.ScopedOwn");
        var scope = new CorrelationScope();
        var processor = new TUnitTestCorrelationProcessor(scope);

        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            using var own = new ActivitySource("CorrelationProcessorTests.ScopedOwn").StartActivity("own")!;
            own.AddBaggage(CorrelationScope.FactoryIdBaggageKey, scope.FactoryId);
            TraceRegistry.Register(own.TraceId.ToString(), testNodeUid: "node-y", contextId: "ctx-y");

            processor.OnEnd(own);

            await Assert.That(own.GetTagItem("tunit.test.id")).IsEqualTo("ctx-y");
        }
        finally
        {
            Activity.Current = previous;
        }
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
