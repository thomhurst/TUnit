using TUnit.Core.Executors;
using TUnit.TestProject.Attributes;
using TUnit.TestProject.TestExecutors;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class HookExecutorTests
{
    // Test Session Hooks
    [Before(TestSession)]
    [HookExecutor<CrossPlatformTestExecutor>]
    public static async Task BeforeTestSessionWithCustomExecutor(TestSessionContext context)
    {
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
        await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();

        var test = context.AllTests.FirstOrDefault(x =>
            x.TestDetails.TestName == nameof(VerifyBeforeTestSessionExecutorExecuted));
        if (test != null)
        {
            test.ObjectBag["BeforeTestSessionExecutorExecuted"] = true;
        }
    }

    [After(TestSession)]
    [HookExecutor<CrossPlatformTestExecutor>]
    public static async Task AfterTestSessionWithCustomExecutor(TestSessionContext context)
    {
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
        await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();
    }

    // Test Discovery Hooks
    [Before(TestDiscovery)]
    [HookExecutor<CrossPlatformTestExecutor>]
    public static async Task BeforeTestDiscoveryWithCustomExecutor(BeforeTestDiscoveryContext context)
    {
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
        await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();
    }

    [After(TestDiscovery)]
    [HookExecutor<CrossPlatformTestExecutor>]
    public static async Task AfterTestDiscoveryWithCustomExecutor(TestDiscoveryContext context)
    {
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
        await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();
    }

    // Assembly Hooks
    private static bool _beforeAssemblyExecutorExecuted;

    [Before(Assembly)]
    [HookExecutor<CrossPlatformTestExecutor>]
    public static async Task BeforeAssemblyWithCustomExecutor(AssemblyHookContext context)
    {
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
        await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();
        _beforeAssemblyExecutorExecuted = true;
    }

    [After(Assembly)]
    [HookExecutor<CrossPlatformTestExecutor>]
    public static async Task AfterAssemblyWithCustomExecutor(AssemblyHookContext context)
    {
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
        await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();
    }

    // Class Hooks
    private static bool _beforeClassExecutorExecuted;

    [Before(Class)]
    [HookExecutor<CrossPlatformTestExecutor>]
    public static async Task BeforeClassWithCustomExecutor(ClassHookContext context)
    {
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
        await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();
        _beforeClassExecutorExecuted = true;
    }

    [After(Class)]
    [HookExecutor<CrossPlatformTestExecutor>]
    public static async Task AfterClassWithCustomExecutor(ClassHookContext context)
    {
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
        await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();
    }

    // Test Hooks - Instance methods
    [Before(Test)]
    [HookExecutor<CrossPlatformTestExecutor>]
    public async Task BeforeTestWithCustomExecutor(TestContext context)
    {
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
        await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();
        context.ObjectBag["BeforeTestExecutorExecuted"] = true;
    }

    [After(Test)]
    [HookExecutor<CrossPlatformTestExecutor>]
    public async Task AfterTestWithCustomExecutor(TestContext context)
    {
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
        await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();
    }

    // Test Hooks - Static methods
    [BeforeEvery(Test)]
    [HookExecutor<CrossPlatformTestExecutor>]
    public static async Task BeforeEveryTestWithCustomExecutor(TestContext context)
    {
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
        await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();

        if (context.TestDetails.TestName == nameof(VerifyStaticTestHooksExecutorExecuted))
        {
            context.ObjectBag["BeforeEveryTestExecutorExecuted"] = true;
        }
    }

    [AfterEvery(Test)]
    [HookExecutor<CrossPlatformTestExecutor>]
    public static async Task AfterEveryTestWithCustomExecutor(TestContext context)
    {
        await Assert.That(Thread.CurrentThread.Name).IsEqualTo("CrossPlatformTestExecutor");
        await Assert.That(CrossPlatformTestExecutor.IsRunningInTestExecutor.Value).IsTrue();
    }

    // Tests to verify hooks executed
    [Test]
    public async Task VerifyBeforeTestSessionExecutorExecuted()
    {
        await Assert.That(TestContext.Current?.ObjectBag["BeforeTestSessionExecutorExecuted"]).IsEquatableOrEqualTo(true);
    }

    [Test]
    public async Task VerifyBeforeAssemblyExecutorExecuted()
    {
        await Assert.That(_beforeAssemblyExecutorExecuted).IsTrue();
    }

    [Test]
    public async Task VerifyBeforeClassExecutorExecuted()
    {
        await Assert.That(_beforeClassExecutorExecuted).IsTrue();
    }

    [Test]
    public async Task VerifyBeforeTestExecutorExecuted()
    {
        await Assert.That(TestContext.Current?.ObjectBag["BeforeTestExecutorExecuted"]).IsEquatableOrEqualTo(true);
    }

    [Test]
    public async Task VerifyStaticTestHooksExecutorExecuted()
    {
        await Assert.That(TestContext.Current?.ObjectBag["BeforeEveryTestExecutorExecuted"]).IsEquatableOrEqualTo(true);
    }
}
