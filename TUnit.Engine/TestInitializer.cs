using TUnit.Core;
using TUnit.Core.Tracking;
using TUnit.Engine.Extensions;
using TUnit.Engine.Services;

namespace TUnit.Engine;

internal class TestInitializer
{
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;
    private readonly ObjectInitializationService _objectInitializationService;
    private readonly PropertyInjectionService _propertyInjectionService;
    private readonly ObjectTracker _objectTracker;

    public TestInitializer(EventReceiverOrchestrator eventReceiverOrchestrator,
        ObjectInitializationService objectInitializationService,
        PropertyInjectionService propertyInjectionService,
        ObjectTracker objectTracker)
    {
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _objectInitializationService = objectInitializationService;
        _propertyInjectionService = propertyInjectionService;
        _objectTracker = objectTracker;
    }

    public async Task InitializeTest(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        var testClassInstance = test.Context.TestDetails.ClassInstance;

        // Step 1: Inject properties into test class instance
        // This sets pre-resolved properties that were created during registration phase
        await _propertyInjectionService.InjectPropertiesIntoObjectAsync(
            testClassInstance,
            test.Context.ObjectBag,
            test.Context.TestDetails.MethodMetadata,
            test.Context.Events);

        // Step 2: Initialize test method arguments
        // Method arguments were already registered (property injection + tracking) during test discovery
        // Now we need to call IAsyncInitializer on them before test execution
        var methodArguments = test.Context.TestDetails.TestMethodArguments;
        if (methodArguments != null && methodArguments.Length > 0)
        {
            var argumentTasks = methodArguments
                .Where(arg => arg != null)
                .Select(arg => _objectInitializationService.InitializeAsync(arg!))
                .ToArray();
            await Task.WhenAll(argumentTasks);
        }

        // Step 3: Call IAsyncInitializer on test class (execution-phase initialization)
        await _objectInitializationService.InitializeAsync(testClassInstance);

        // Step 4: Initialize and register all eligible objects including event receivers
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
    }
}
