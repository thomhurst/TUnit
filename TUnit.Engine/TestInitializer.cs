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

    public void PrepareTest(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        // Register event receivers
        _eventReceiverOrchestrator.RegisterReceivers(test.Context, cancellationToken);

        // Prepare test: set cached property values on the instance
        // Does NOT call IAsyncInitializer - that is deferred until after BeforeClass hooks
        _objectLifecycleService.PrepareTest(test.Context);
    }

    public async ValueTask InitializeTestObjectsAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        // Initialize test objects (IAsyncInitializer) - called after BeforeClass hooks
        await _objectLifecycleService.InitializeTestObjectsAsync(test.Context, cancellationToken);
    }
}
