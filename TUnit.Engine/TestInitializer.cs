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

    public async ValueTask InitializeTest(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        // Register event receivers
        _eventReceiverOrchestrator.RegisterReceivers(test.Context, cancellationToken);

        // Prepare test: inject properties, track objects, initialize (IAsyncInitializer)
        await _objectLifecycleService.PrepareTestAsync(test.Context, cancellationToken);
    }
}
