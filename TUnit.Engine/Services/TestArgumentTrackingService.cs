using TUnit.Core;
using TUnit.Core.Initialization;
using TUnit.Core.Interfaces;
using TUnit.Core.Tracking;

namespace TUnit.Engine.Services;

/// <summary>
/// Service that handles tracking of test arguments (constructor args, method args) for disposal.
/// Implements ITestRegisteredEventReceiver to track objects when tests are registered.
/// </summary>
internal sealed class TestArgumentTrackingService : ITestRegisteredEventReceiver
{
    public int Order => int.MinValue; // Run first to ensure tracking happens before other event receivers

    /// <summary>
    /// Called when a test is registered. This is the correct time to track constructor and method arguments
    /// for shared instances, as per the ObjectTracker reference counting approach.
    /// </summary>
    public async ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        var testContext = context.TestContext;
        var classArguments = testContext.TestDetails.TestClassArguments;
        var methodArguments = testContext.TestDetails.TestMethodArguments;
        
        // Use centralized TestObjectInitializer for all initialization
        // Initialize class arguments
        await TestObjectInitializer.InitializeArgumentsAsync(
            classArguments,
            testContext.ObjectBag,
            testContext.TestDetails.MethodMetadata,
            testContext.Events);
        
        // Initialize method arguments
        await TestObjectInitializer.InitializeArgumentsAsync(
            methodArguments,
            testContext.ObjectBag,
            testContext.TestDetails.MethodMetadata,
            testContext.Events);
        
        // Track all constructor and method arguments
        // Note: TestObjectInitializer already handles tracking, but we ensure it here for clarity
        var allArguments = classArguments.Concat(methodArguments);
        
        foreach (var obj in allArguments)
        {
            if (obj != null)
            {
                // Track each argument - for shared instances, this increments the reference count
                // When the test ends, the count will be decremented via the test's Events.OnDispose
                ObjectTracker.TrackObject(testContext.Events, obj);
            }
        }
    }
}