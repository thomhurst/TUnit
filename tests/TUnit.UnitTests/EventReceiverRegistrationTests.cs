using TUnit.Core.Interfaces;
using TUnit.Engine.Services;

namespace TUnit.UnitTests;

public class EventReceiverRegistrationTests
{
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
