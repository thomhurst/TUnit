using TUnit.Core;
using TUnit.Core.PropertyInjection;

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
    public ValueTask RegisterTestArgumentsAsync(TestContext testContext, CancellationToken cancellationToken = default)
    {
        var testDetails = testContext.Metadata.TestDetails;
        if (testDetails.TestClassArguments.Length == 0 &&
            testDetails.TestMethodArguments.Length == 0 &&
            !PropertyInjectionCache.GetOrCreatePlan(testDetails.ClassType).HasProperties)
        {
            // Nothing to register, so no user code runs here — skip the AsyncLocal write.
            return default;
        }

        TestContext.Current = testContext;

        return RegisterTestArgumentsCoreAsync(testContext, testDetails, cancellationToken);
    }

    private static bool HasInjectableArgument(object?[] arguments)
    {
        foreach (var argument in arguments)
        {
            if (argument is not null && PropertyInjectionCache.HasInjectableProperties(argument.GetType()))
            {
                return true;
            }
        }

        return false;
    }

    private async ValueTask RegisterTestArgumentsCoreAsync(
        TestContext testContext,
        TestDetails testDetails,
        CancellationToken cancellationToken)
    {
        var classArguments = testDetails.TestClassArguments;
        var methodArguments = testDetails.TestMethodArguments;

        // Argument registration only injects properties, so it is a no-op unless some argument has
        // injectable properties. Checking first avoids materializing the per-test state bag
        // (a ConcurrentDictionary) and events container for plain arguments such as [Arguments] values.
        if (HasInjectableArgument(classArguments) || HasInjectableArgument(methodArguments))
        {
            // Register class arguments (property injection during registration)
            await _objectLifecycleService.RegisterArgumentsAsync(
                classArguments,
                testContext.StateBag.Items,
                testDetails.MethodMetadata,
                testContext.InternalEvents,
                cancellationToken).ConfigureAwait(false);

            // Register method arguments
            await _objectLifecycleService.RegisterArgumentsAsync(
                methodArguments,
                testContext.StateBag.Items,
                testDetails.MethodMetadata,
                testContext.InternalEvents,
                cancellationToken).ConfigureAwait(false);
        }

        // Register the test for tracking (inject properties and track objects for disposal)
        await _objectLifecycleService.RegisterTestAsync(testContext, cancellationToken).ConfigureAwait(false);
    }
}
