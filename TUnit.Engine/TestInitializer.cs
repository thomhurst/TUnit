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
        // Data source initialization now runs inside the test case span, so any spans it
        // creates (container startup, auth calls, connection pools, etc.) will appear nested
        // inside the test's trace timeline via the "data source initialization" child activity
        // created by TestExecutor.
        await _objectLifecycleService.InitializeTestObjectsAsync(test.Context, cancellationToken);
    }
}
