using NSubstitute;
using TUnit.Core.Interfaces;
using TUnit.Engine.Services;

namespace TUnit.UnitTests;

public class EventReceiverRegistrationTests
{
    [Test]
    [Arguments("start")]
    [Arguments("end")]
    public async Task DuplicateDelegateSubscriptionsRunOnlyInLateStage(string eventName)
    {
        var context = CreateContext(new OrdinaryObject(), []);
        var calls = 0;
        Func<object, TestContext, ValueTask> handler = (_, _) => { calls++; return default; };
        var selected = eventName == "start"
            ? context.InternalEvents.OnTestStart ??= new()
            : context.InternalEvents.OnTestEnd ??= new();
        selected.Add(handler);
        selected.Add(handler);
        try
        {
            var orchestrator = new EventReceiverOrchestrator(null!);
            async Task Dispatch(TUnit.Core.Enums.EventReceiverStage stage)
            {
                if (eventName == "start")
                    await orchestrator.InvokeTestStartEventReceiversAsync(context, CancellationToken.None, stage);
                else
                    await orchestrator.InvokeTestEndEventReceiversAsync(context, CancellationToken.None, stage);
            }
            await Dispatch(TUnit.Core.Enums.EventReceiverStage.Early);
            await Assert.That(calls).IsEqualTo(0);
            await Dispatch(TUnit.Core.Enums.EventReceiverStage.Late);
            await Assert.That(calls).IsEqualTo(2);
        }
        finally
        {
            context.RemoveFromRegistry();
            context.Dispose();
        }
    }

    [Test]
    [Arguments("start")]
    [Arguments("end")]
    [Arguments("skipped")]
    public async Task DelegateSubscriptionsDuringDispatchApplyToNextDispatch(string eventName)
    {
        var context = CreateContext(new OrdinaryObject(), []);
        var calls = new List<string>();
        var events = context.InternalEvents;
        var selected = eventName switch
        {
            "start" => events.OnTestStart ??= new(),
            "end" => events.OnTestEnd ??= new(),
            _ => events.OnTestSkipped ??= new()
        };
        selected += (_, _) =>
        {
            calls.Add("first");
            selected += (_, _) => { calls.Add("added"); return ValueTask.CompletedTask; };
            return ValueTask.CompletedTask;
        };
        selected += (_, _) => { calls.Add("last"); return ValueTask.CompletedTask; };
        try
        {
            var orchestrator = new EventReceiverOrchestrator(null!);
            async Task Dispatch()
            {
                if (eventName == "start")
                    await orchestrator.InvokeTestStartEventReceiversAsync(context, CancellationToken.None);
                else if (eventName == "end")
                    await orchestrator.InvokeTestEndEventReceiversAsync(context, CancellationToken.None);
                else
                    await orchestrator.InvokeTestSkippedEventReceiversAsync(context, CancellationToken.None);
            }

            await Dispatch();
            await Assert.That(string.Join(",", calls)).IsEqualTo("first,last");
            calls.Clear();
            await Dispatch();
            await Assert.That(string.Join(",", calls)).IsEqualTo("first,last,added");
        }
        finally
        {
            context.RemoveFromRegistry();
            context.Dispose();
        }
    }

    [Test]
    public async Task EndDelegateMutationPreservesRemainingHandlersAndCollectedFailures()
    {
        var context = CreateContext(new OrdinaryObject(), []);
        var failure = new InvalidOperationException("End handler failed");
        var lastRan = false;
        context.InternalEvents.OnTestEnd += (_, _) =>
        {
            context.InternalEvents.OnTestEnd += (_, _) => ValueTask.CompletedTask;
            throw failure;
        };
        context.InternalEvents.OnTestEnd += (_, _) => { lastRan = true; return ValueTask.CompletedTask; };
        var options = NSubstitute.Substitute.For<Microsoft.Testing.Platform.CommandLine.ICommandLineOptions>();
        options.TryGetOptionArgumentList(NSubstitute.Arg.Any<string>(), out NSubstitute.Arg.Any<string[]>()!)
            .Returns(call => { call[1] = new[] { "None" }; return true; });
        var logger = new TUnit.Engine.Logging.TUnitFrameworkLogger(null!, null!, null!, new LogLevelProvider(options));
        try
        {
            var orchestrator = new EventReceiverOrchestrator(logger);
            var exceptions = await orchestrator.InvokeTestEndEventReceiversAsync(context, CancellationToken.None);
            await Assert.That(lastRan).IsTrue();
            await Assert.That(exceptions.Count).IsEqualTo(1);
            await Assert.That(exceptions[0]).IsSameReferenceAs(failure);
        }
        finally
        {
            context.RemoveFromRegistry();
            context.Dispose();
        }
    }

    [Test]
    public async Task StartDelegateFailurePropagatesOriginalException()
    {
        var context = CreateContext(new OrdinaryObject(), []);
        var failure = new InvalidOperationException("Start handler failed");
        context.InternalEvents.OnTestStart += (_, _) => throw failure;
        try
        {
            var orchestrator = new EventReceiverOrchestrator(null!);
            var thrown = await Assert.That(async () =>
                await orchestrator.InvokeTestStartEventReceiversAsync(context, CancellationToken.None))
                .Throws<InvalidOperationException>();
            await Assert.That(thrown).IsSameReferenceAs(failure);
        }
        finally
        {
            context.RemoveFromRegistry();
            context.Dispose();
        }
    }

    [Test]
    public void OrdinaryObjectsDoNotParticipateInReceiverDeduplication()
    {
        var context = CreateContext(new OrdinaryObject(), [new OrdinaryAttribute()]);
        try
        {
            var orchestrator = new EventReceiverOrchestrator(null!);
            orchestrator.RegisterReceivers(context);
            orchestrator.RegisterClassInstanceReceiver(context);
        }
        finally
        {
            context.RemoveFromRegistry();
            context.Dispose();
        }
    }

    [Test]
    public async Task AttributeAndClassReceiversStillReceiveEvents()
    {
        var attribute = new ReceiverAttribute();
        var instance = new ReceiverAttribute();
        var context = CreateContext(null!, [attribute]);
        try
        {
            var orchestrator = new EventReceiverOrchestrator(null!);
            orchestrator.RegisterReceivers(context);
            context.Metadata.TestDetails.ClassInstance = instance;
            orchestrator.RegisterClassInstanceReceiver(context);
            orchestrator.RegisterClassInstanceReceiver(context);

            await orchestrator.InvokeTestStartEventReceiversAsync(context, CancellationToken.None);
            await Assert.That(attribute.Calls).IsEqualTo(1);
            await Assert.That(instance.Calls).IsEqualTo(1);
        }
        finally
        {
            context.RemoveFromRegistry();
            context.Dispose();
        }
    }

    [Test]
    public async Task PerTestReceiversDoNotParticipateInReceiverDeduplication()
    {
        // Attribute.GetHashCode/Equals reflect over fields; per-test receivers such as
        // [Arguments] must not be hashed, or registering one per test becomes quadratic.
        var attribute = new HashingForbiddenReceiverAttribute();
        var instance = new HashingForbiddenReceiverAttribute();
        var context = CreateContext(null!, [attribute]);
        try
        {
            var orchestrator = new EventReceiverOrchestrator(null!);
            orchestrator.RegisterReceivers(context);
            context.Metadata.TestDetails.ClassInstance = instance;
            orchestrator.RegisterClassInstanceReceiver(context);

            // Each per-test event type must still set its presence flag, or dispatch is skipped.
            await orchestrator.InvokeTestStartEventReceiversAsync(context, CancellationToken.None);
            var endExceptions = await orchestrator.InvokeTestEndEventReceiversAsync(context, CancellationToken.None);
            await orchestrator.InvokeTestSkippedEventReceiversAsync(context, CancellationToken.None);

            await Assert.That(endExceptions).IsEmpty();
            await Assert.That(attribute.StartCalls).IsEqualTo(1);
            await Assert.That(instance.StartCalls).IsEqualTo(1);
            await Assert.That(attribute.EndCalls).IsEqualTo(1);
            await Assert.That(instance.EndCalls).IsEqualTo(1);
            await Assert.That(attribute.SkippedCalls).IsEqualTo(1);
            await Assert.That(instance.SkippedCalls).IsEqualTo(1);
        }
        finally
        {
            context.RemoveFromRegistry();
            context.Dispose();
        }
    }

    private static TestContext CreateContext(object instance, Attribute[] attributes)
    {
        var current = TestContext.Current!;
        var context = new TestContext("Registration", current.ServiceProvider, current.ClassContext,
            new TestBuilderContext { TestMetadata = current.TestDetails.MethodMetadata }, CancellationToken.None);
        context.Metadata.TestDetails = new TestDetails(attributes)
        {
            TestId = context.Id, TestName = "Registration", ClassType = typeof(EventReceiverRegistrationTests),
            MethodName = "Registration", ClassInstance = instance, TestMethodArguments = [], TestClassArguments = [],
            MethodMetadata = current.TestDetails.MethodMetadata, ReturnType = typeof(void),
            AttributesByType = new Dictionary<Type, IReadOnlyList<Attribute>>()
        };
        return context;
    }

    private sealed class OrdinaryObject
    {
        public override int GetHashCode() => throw new InvalidOperationException("Not an event receiver");
    }

    private sealed class OrdinaryAttribute : Attribute
    {
        public override int GetHashCode() => throw new InvalidOperationException("Not an event receiver");
    }

    private sealed class HashingForbiddenReceiverAttribute : Attribute,
        ITestStartEventReceiver, ITestEndEventReceiver, ITestSkippedEventReceiver
    {
        public int StartCalls { get; private set; }
        public int EndCalls { get; private set; }
        public int SkippedCalls { get; private set; }
        public override int GetHashCode() => throw new InvalidOperationException("Per-test receivers should not be hashed");
        public override bool Equals(object? obj) => throw new InvalidOperationException("Per-test receivers should not be compared");
        public ValueTask OnTestStart(TestContext context)
        {
            StartCalls++;
            return ValueTask.CompletedTask;
        }
        public ValueTask OnTestEnd(TestContext context)
        {
            EndCalls++;
            return ValueTask.CompletedTask;
        }
        public ValueTask OnTestSkipped(TestContext context)
        {
            SkippedCalls++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ReceiverAttribute : Attribute, ITestStartEventReceiver
    {
        public int Calls { get; private set; }
        public ValueTask OnTestStart(TestContext context)
        {
            Calls++;
            return ValueTask.CompletedTask;
        }
    }
}
