using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Executors;

namespace TUnit.TestProject;

[UnconditionalSuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class HookExecutorTests
{
    // Test Session Hooks
    [Before(TestSession)]
    [HookExecutor<STAThreadExecutor>]
    public static async Task BeforeTestSessionWithSTA(TestSessionContext context)
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
        
        var test = context.AllTests.FirstOrDefault(x =>
            x.TestDetails.TestName == nameof(VerifyBeforeTestSessionSTAExecuted));
        test?.ObjectBag.Add("BeforeTestSessionSTAExecuted", true);
    }

    [After(TestSession)]
    [HookExecutor<STAThreadExecutor>]
    public static async Task AfterTestSessionWithSTA(TestSessionContext context)
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    // Test Discovery Hooks
    [Before(TestDiscovery)]
    [HookExecutor<STAThreadExecutor>]
    public static async Task BeforeTestDiscoveryWithSTA(BeforeTestDiscoveryContext context)
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [After(TestDiscovery)]
    [HookExecutor<STAThreadExecutor>]
    public static async Task AfterTestDiscoveryWithSTA(TestDiscoveryContext context)
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    // Assembly Hooks
    private static bool _beforeAssemblySTAExecuted;
    
    [Before(Assembly)]
    [HookExecutor<STAThreadExecutor>]
    public static async Task BeforeAssemblyWithSTA(AssemblyHookContext context)
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
        _beforeAssemblySTAExecuted = true;
    }

    [After(Assembly)]
    [HookExecutor<STAThreadExecutor>]
    public static async Task AfterAssemblyWithSTA(AssemblyHookContext context)
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    // Class Hooks
    private static bool _beforeClassSTAExecuted;
    
    [Before(Class)]
    [HookExecutor<STAThreadExecutor>]
    public static async Task BeforeClassWithSTA(ClassHookContext context)
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
        _beforeClassSTAExecuted = true;
    }

    [After(Class)]
    [HookExecutor<STAThreadExecutor>]
    public static async Task AfterClassWithSTA(ClassHookContext context)
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    // Test Hooks - Instance methods
    [Before(Test)]
    [HookExecutor<STAThreadExecutor>]
    public async Task BeforeTestWithSTA(TestContext context)
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
        context.ObjectBag.Add("BeforeTestSTAExecuted", true);
    }

    [After(Test)]
    [HookExecutor<STAThreadExecutor>]
    public async Task AfterTestWithSTA(TestContext context)
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    // Test Hooks - Static methods
    [BeforeEvery(Test)]
    [HookExecutor<STAThreadExecutor>]
    public static async Task BeforeEveryTestWithSTA(TestContext context)
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
        
        if (context.TestDetails.TestName == nameof(VerifyStaticTestHooksSTAExecuted))
        {
            context.ObjectBag.Add("BeforeEveryTestSTAExecuted", true);
        }
    }

    [AfterEvery(Test)]
    [HookExecutor<STAThreadExecutor>]
    public static async Task AfterEveryTestWithSTA(TestContext context)
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    // Tests to verify hooks executed
    [Test]
    public async Task VerifyBeforeTestSessionSTAExecuted()
    {
        await Assert.That(TestContext.Current?.ObjectBag["BeforeTestSessionSTAExecuted"]).IsEquatableOrEqualTo(true);
    }

    [Test]
    public async Task VerifyBeforeAssemblySTAExecuted()
    {
        await Assert.That(_beforeAssemblySTAExecuted).IsTrue();
    }

    [Test]
    public async Task VerifyBeforeClassSTAExecuted()
    {
        await Assert.That(_beforeClassSTAExecuted).IsTrue();
    }

    [Test]
    public async Task VerifyBeforeTestSTAExecuted()
    {
        await Assert.That(TestContext.Current?.ObjectBag["BeforeTestSTAExecuted"]).IsEquatableOrEqualTo(true);
    }

    [Test]
    public async Task VerifyStaticTestHooksSTAExecuted()
    {
        await Assert.That(TestContext.Current?.ObjectBag["BeforeEveryTestSTAExecuted"]).IsEquatableOrEqualTo(true);
    }
}