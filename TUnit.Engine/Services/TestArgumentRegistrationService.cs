using TUnit.Core;

namespace TUnit.Engine.Services;

/// <summary>
/// Internal service that handles registration of test arguments during test discovery.
/// Not a user-extensibility point - called directly by TestBuilder.
/// Simplified to use ObjectLifecycleService for all object registration.
/// </summary>
internal sealed class TestArgumentRegistrationService
{
    private readonly ObjectLifecycleService _objectLifecycleService;

    public TestArgumentRegistrationService(ObjectLifecycleService objectLifecycleService)
    {
        _objectLifecycleService = objectLifecycleService;
    }

    /// <summary>
    /// Called when a test is registered. Registers constructor and method arguments
    /// for proper reference counting and disposal tracking.
    /// Property values are resolved lazily during test execution (not during discovery).
    /// </summary>
    public async ValueTask RegisterTestArgumentsAsync(TestContext testContext)
    {
        TestContext.Current = testContext;

        var classArguments = testContext.Metadata.TestDetails.TestClassArguments;
        var methodArguments = testContext.Metadata.TestDetails.TestMethodArguments;

        // Register class arguments (property injection during registration)
        await _objectLifecycleService.RegisterArgumentsAsync(
            classArguments,
            testContext.StateBag.Items,
            testContext.Metadata.TestDetails.MethodMetadata,
            testContext.InternalEvents);

        // Register method arguments
        await _objectLifecycleService.RegisterArgumentsAsync(
            methodArguments,
            testContext.StateBag.Items,
            testContext.Metadata.TestDetails.MethodMetadata,
            testContext.InternalEvents);

        // Register the test for tracking (inject properties and track objects for disposal)
        await _objectLifecycleService.RegisterTestAsync(testContext);
    }
}
