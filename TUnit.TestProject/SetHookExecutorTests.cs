using TUnit.Core.Executors;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;
using TUnit.TestProject.TestExecutors;

namespace TUnit.TestProject;

/// <summary>
/// Attribute that sets both test and hook executors - mimics the user's [Dispatch] attribute from issue #2666
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SetBothExecutorsAttribute : Attribute, ITestRegisteredEventReceiver
{
    public int Order => 0;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        // Set both test and hook executors to use the same custom executor
        // This is the key feature - users can now wrap all methods (test + hooks) in their custom dispatcher
        var customExecutor = new CrossPlatformTestExecutor();
        context.SetTestExecutor(customExecutor);
        context.SetHookExecutor(customExecutor);

        return default;
    }
}

/// <summary>
/// Tests demonstrating SetHookExecutor functionality for issue #2666
/// Users can use an attribute that calls context.SetHookExecutor() to wrap both tests and their hooks in the same executor
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[SetBothExecutors] // This attribute sets both executors
public class SetHookExecutorTests
{
    private static bool _beforeTestHookExecutedWithCustomExecutor;
    private static bool _afterTestHookExecutedWithCustomExecutor;

    [Before(Test)]
    public async Task BeforeTestHook(TestContext context)
    {
        // This hook should execute with the custom executor set by the attribute
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
        await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();
        _beforeTestHookExecutedWithCustomExecutor = true;
    }

    [After(Test)]
    public async Task AfterTestHook(TestContext context)
    {
        // This hook should execute with the custom executor set by the attribute
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
        await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();
        _afterTestHookExecutedWithCustomExecutor = true;
    }

    [Test]
    public async Task Test_ExecutesInCustomExecutor()
    {
        // Test should execute in custom executor
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
        await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();
    }

    [Test]
    public async Task Test_HooksAlsoExecuteInCustomExecutor()
    {
        // Verify that the hooks executed in the custom executor
        await Assert.That(_beforeTestHookExecutedWithCustomExecutor).IsTrue();

        // After hook will be verified by its own assertions
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
    }
}

/// <summary>
/// Tests demonstrating SetHookExecutor with static hooks
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[SetBothExecutors] // This attribute sets both executors
public class SetHookExecutorWithStaticHooksTests
{
    [BeforeEvery(Test)]
    public static async Task BeforeEveryTest(TestContext context)
    {
        // This static hook is GLOBAL and runs for ALL tests in the assembly
        // Only run assertions for tests in SetHookExecutorWithStaticHooksTests class
        if (context.Metadata.TestDetails.ClassType == typeof(SetHookExecutorWithStaticHooksTests))
        {
            // This static hook should execute with the custom executor when CustomHookExecutor is set
            await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
            await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();
            context.StateBag.Items["BeforeEveryExecuted"] = true;
        }
    }

    [AfterEvery(Test)]
    public static async Task AfterEveryTest(TestContext context)
    {
        // This static hook is GLOBAL and runs for ALL tests in the assembly
        // Only run assertions for tests in SetHookExecutorWithStaticHooksTests class
        if (context.Metadata.TestDetails.ClassType == typeof(SetHookExecutorWithStaticHooksTests))
        {
            // This static hook should execute with the custom executor when CustomHookExecutor is set
            await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
            await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();
        }
    }

    [Test]
    public async Task Test_StaticHooksExecuteInCustomExecutor()
    {
        // Verify the BeforeEvery hook ran
        await Assert.That(TestContext.Current?.StateBag.Items["BeforeEveryExecuted"]).IsEquatableOrEqualTo(true);

        // Test itself runs in custom executor
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
    }
}

/// <summary>
/// Tests demonstrating SetHookExecutor affects disposal execution - Issue #3918
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[SetBothExecutors] // This attribute sets both executors
public class DisposalWithHookExecutorTests : IAsyncDisposable
{
    private static bool _disposalExecutedInCustomExecutor;

    [Test]
    public async Task Test_ExecutesInCustomExecutor()
    {
        // Test should execute in custom executor
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
        await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();
    }

    public ValueTask DisposeAsync()
    {
        // Verify disposal runs in the custom executor
        _disposalExecutedInCustomExecutor =
            Thread.CurrentThread.Name == "CrossPlatformTestExecutor" &&
            CrossPlatformTestExecutor.IsRunningInTestExecutor.Value;
        return default;
    }

    [After(Class)]
    public static async Task VerifyDisposalRanInCustomExecutor(ClassHookContext context)
    {
        await Assert.That(_disposalExecutedInCustomExecutor).IsTrue();
    }
}
