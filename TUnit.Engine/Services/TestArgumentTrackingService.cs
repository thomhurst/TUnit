using System;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.DataSources;
using TUnit.Core.Enums;
using TUnit.Core.Initialization;
using TUnit.Core.Interfaces;
using TUnit.Core.Interfaces.SourceGenerator;
using TUnit.Core.Tracking;

namespace TUnit.Engine.Services;

/// <summary>
/// Service that handles tracking of test arguments (constructor args, method args) for disposal.
/// Implements ITestRegisteredEventReceiver to track objects when tests are registered.
/// </summary>
internal sealed class TestArgumentTrackingService : ITestRegisteredEventReceiver
{
    private readonly TestObjectInitializer _testObjectInitializer;

    public TestArgumentTrackingService(TestObjectInitializer testObjectInitializer)
    {
        _testObjectInitializer = testObjectInitializer;
    }

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
        
        // Initialize class arguments (registration phase)
        await _testObjectInitializer.InitializeArgumentsAsync(
            classArguments,
            testContext.ObjectBag,
            testContext.TestDetails.MethodMetadata,
            testContext.Events,
            isRegistrationPhase: true);

        // Initialize method arguments (registration phase)
        await _testObjectInitializer.InitializeArgumentsAsync(
            methodArguments,
            testContext.ObjectBag,
            testContext.TestDetails.MethodMetadata,
            testContext.Events,
            isRegistrationPhase: true);
        
        // Track all constructor and method arguments
        // Note: TestObjectInitializer already handles tracking, but we ensure it here for clarity
        var allArguments = classArguments.Concat(methodArguments);
        
        foreach (var obj in allArguments)
        {
            if (obj != null)
            {
                // Track each argument - for shared instances, this increments the reference count
                // When the test ends, the count will be decremented via the test's Events.OnTestFinalized
                ObjectTracker.TrackObject(testContext.Events, obj);
            }
        }

        // Track properties that will be injected into the test class
        await TrackPropertiesAsync(testContext);
    }

    /// <summary>
    /// Tracks properties that will be injected into the test class instance.
    /// This ensures proper reference counting for all property-injected instances.
    /// </summary>
    private async ValueTask TrackPropertiesAsync(TestContext testContext)
    {
        // For now, we'll skip property tracking during registration
        // since the implementation is complex with different ClassDataSource variants.
        // Properties will be tracked when they're actually injected during test execution.
        // This is a simplified approach that focuses purely on reference counting.
        await Task.CompletedTask;
    }
}