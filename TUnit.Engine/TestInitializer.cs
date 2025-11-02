using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Tracking;
using TUnit.Engine.Extensions;
using TUnit.Engine.Services;

namespace TUnit.Engine;

internal class TestInitializer
{
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;
    private readonly PropertyInjectionService _propertyInjectionService;
    private readonly ObjectTracker _objectTracker;

    public TestInitializer(EventReceiverOrchestrator eventReceiverOrchestrator,
        PropertyInjectionService propertyInjectionService,
        ObjectTracker objectTracker)
    {
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _propertyInjectionService = propertyInjectionService;
        _objectTracker = objectTracker;
    }

    public async Task InitializeTest(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        var testClassInstance = test.Context.Metadata.TestDetails.ClassInstance;

        await _propertyInjectionService.InjectPropertiesIntoObjectAsync(
            testClassInstance,
            test.Context.StateBag.Items,
            test.Context.Metadata.TestDetails.MethodMetadata,
            test.Context.Events);

        _eventReceiverOrchestrator.RegisterReceivers(test.Context, cancellationToken);

        // Shouldn't retrack already tracked objects, but will track any new ones created during retries / initialization
        _objectTracker.TrackObjects(test.Context);

        await InitializeTrackedObjects(test.Context, cancellationToken);
    }

    private async Task InitializeTrackedObjects(TestContext testContext, CancellationToken cancellationToken)
    {
        // Initialize by level (deepest first), with objects at the same level in parallel
        var levels = testContext.TrackedObjects.Keys.OrderByDescending(level => level);

        foreach (var level in levels)
        {
            var objectsAtLevel = testContext.TrackedObjects[level];
            await Task.WhenAll(objectsAtLevel.Select(obj => ObjectInitializer.InitializeAsync(obj, cancellationToken).AsTask()));
        }

        // Finally, ensure the test class itself is initialized
        await ObjectInitializer.InitializeAsync(testContext.Metadata.TestDetails.ClassInstance, cancellationToken);
    }
}
