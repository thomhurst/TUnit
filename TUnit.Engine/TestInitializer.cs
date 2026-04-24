using TUnit.Core;
using TUnit.Engine.Services;

namespace TUnit.Engine;

/// <summary>
/// Simplified test initializer that delegates to ObjectLifecycleService.
/// Follows Single Responsibility Principle - only coordinates test initialization.
/// </summary>
internal class TestInitializer
{
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;
    private readonly ObjectLifecycleService _objectLifecycleService;

    public TestInitializer(
        EventReceiverOrchestrator eventReceiverOrchestrator,
        ObjectLifecycleService objectLifecycleService)
    {
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _objectLifecycleService = objectLifecycleService;
    }

    public void PrepareTest(AbstractExecutableTest test)
    {
        // Register the freshly created ClassInstance as an event receiver. The initial
        // registration happens before instance creation, so re-iterating the full
        // eligible-event-object set would duplicate work — only the ClassInstance is new.
        _eventReceiverOrchestrator.RegisterClassInstanceReceiver(test.Context);

        // Prepare test: set cached property values on the instance.
        // Does NOT call IAsyncInitializer - that is deferred until after BeforeClass hooks.
        _objectLifecycleService.PrepareTest(test.Context);
    }

    public async ValueTask InitializeTestObjectsAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        // ObjectLifecycleService creates per-object initialization spans with scope-aware
        // parent activity selection. Shared objects (PerSession/PerAssembly/PerClass) are
        // parented under session/assembly/class activities; per-test objects and the test
        // class itself are parented under the test case activity.
        await _objectLifecycleService.InitializeTestObjectsAsync(test.Context, cancellationToken);
    }
}
