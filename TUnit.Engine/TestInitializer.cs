using TUnit.Core;
using TUnit.Engine.Services;
#if NET
using System.Diagnostics;
#endif

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
        // Data source initialization runs before the test case span starts, so any spans it
        // creates (container startup, auth calls, connection pools, etc.) do not appear nested
        // inside the individual test's trace timeline. We briefly set Activity.Current to the
        // session span so those spans are parented there instead.
#if NET
        var sessionActivity = test.Context.ClassContext.AssemblyContext.TestSessionContext.Activity;
        var previousActivity = Activity.Current;
        if (sessionActivity is not null)
        {
            Activity.Current = sessionActivity;
        }
        try
        {
            await _objectLifecycleService.InitializeTestObjectsAsync(test.Context, cancellationToken);
        }
        finally
        {
            Activity.Current = previousActivity;
        }
#else
        await _objectLifecycleService.InitializeTestObjectsAsync(test.Context, cancellationToken);
#endif
    }
}
