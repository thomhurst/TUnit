using TUnit.Core;
using TUnit.Core.Lifecycle;
using TUnit.Core.Tracking;
using TUnit.Engine.Services;

namespace TUnit.Engine;

/// <summary>
/// Initializes test instances before execution.
/// Uses ObjectLifecycleManager as the single source of truth for all initialization.
/// </summary>
internal class TestInitializer
{
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;
    private readonly IObjectLifecycleManager _lifecycleManager;
    private readonly ObjectTracker _objectTracker;

    public TestInitializer(
        EventReceiverOrchestrator eventReceiverOrchestrator,
        IObjectLifecycleManager lifecycleManager,
        ObjectTracker objectTracker)
    {
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _lifecycleManager = lifecycleManager;
        _objectTracker = objectTracker;
    }

    public async ValueTask InitializeTest(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        var testContext = test.Context;
        var testClassInstance = testContext.Metadata.TestDetails.ClassInstance;

        _eventReceiverOrchestrator.RegisterReceivers(testContext, cancellationToken);

        // Track any new objects created during retries / initialization
        _objectTracker.TrackObjects(testContext);

        // Initialize tracked objects by level (deepest first)
        await InitializeTrackedObjects(testContext, cancellationToken);

        // Initialize property-injected objects (e.g., ClassDataSource values on properties)
        // This is where IAsyncInitializer.InitializeAsync() gets called
        await InitializePropertyInjectedObjects(testContext, cancellationToken);

        // Finally, ensure the test class instance is fully initialized
        // This handles property injection + IAsyncInitializer in one call
        await _lifecycleManager.EnsureInitializedAsync(
            testClassInstance,
            testContext.StateBag.Items,
            testContext.Metadata.TestDetails.MethodMetadata,
            testContext.InternalEvents,
            testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments,
            cancellationToken);
    }

    private async Task InitializePropertyInjectedObjects(TestContext testContext, CancellationToken cancellationToken)
    {
        var propertyValues = testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments;

        foreach (var kvp in propertyValues)
        {
            var value = kvp.Value;
            if (value != null)
            {
                await _lifecycleManager.EnsureInitializedAsync(
                    value,
                    testContext.StateBag.Items,
                    testContext.Metadata.TestDetails.MethodMetadata,
                    testContext.InternalEvents,
                    cancellationToken: cancellationToken);
            }
        }
    }

    private async Task InitializeTrackedObjects(TestContext testContext, CancellationToken cancellationToken)
    {
        // Initialize by level (deepest first), with objects at the same level in parallel
        var levels = testContext.TrackedObjects.Keys.OrderByDescending(level => level);

        foreach (var level in levels)
        {
            var objectsAtLevel = testContext.TrackedObjects[level];
            await Task.WhenAll(objectsAtLevel.Select(obj =>
                _lifecycleManager.EnsureInitializedAsync(
                    obj,
                    testContext.StateBag.Items,
                    testContext.Metadata.TestDetails.MethodMetadata,
                    testContext.InternalEvents,
                    cancellationToken: cancellationToken).AsTask()));
        }
    }
}
