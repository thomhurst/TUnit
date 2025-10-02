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
        var testClassInstance = test.Context.TestDetails.ClassInstance;

        await _propertyInjectionService.InjectPropertiesIntoObjectAsync(
            testClassInstance,
            test.Context.ObjectBag,
            test.Context.TestDetails.MethodMetadata,
            test.Context.Events);

        await _eventReceiverOrchestrator.InitializeAllEligibleObjectsAsync(test.Context, cancellationToken).ConfigureAwait(false);

        // Shouldn't retrack already tracked objects, but will track any new ones created during retries / initialization
        _objectTracker.TrackObjects(test.Context);

        await InitializeTrackedObjects(test.Context);
    }

    private async Task InitializeTrackedObjects(TestContext testContext)
    {
        // Initialize in reverse order (most nested first)
        foreach (var obj in testContext.TrackedObjects.Reverse())
        {
            await ObjectInitializer.InitializeAsync(obj);
        }

        // Finally, ensure the test class itself is initialized
        await ObjectInitializer.InitializeAsync(testContext.TestDetails.ClassInstance);
    }
}
