using System.Collections.Concurrent;

namespace TUnit.TestProject.Bugs._2804;

public class GlobalHooksExecutionTest
{
    private static readonly ConcurrentBag<string> ExecutedHooks = new();
    
    [Test]
    public async Task VerifyAllGlobalHooksExecute()
    {
        // This test itself doesn't need to do much
        // The real test is whether the hooks execute
        await Task.CompletedTask;
        // Test executed - waiting for hooks
    }
}

public sealed class Issue2804TestHooks
{
    private static readonly ConcurrentBag<string> ExecutedHooks = new();
    
    // Test-level hooks
    [BeforeEvery(Test)]
    public static void BeforeEveryTest(TestContext context)
    {
        ExecutedHooks.Add("BeforeEvery(Test)");
        // BeforeEvery(Test) executed
    }
    
    [AfterEvery(Test)]
    public static void AfterEveryTest(TestContext context)
    {
        ExecutedHooks.Add("AfterEvery(Test)");
        // AfterEvery(Test) executed
    }
    
    [Before(Test)]
    public void BeforeTest(TestContext context)
    {
        ExecutedHooks.Add("Before(Test)");
        // Before(Test) executed
    }
    
    [After(Test)]
    public void AfterTest(TestContext context)
    {
        ExecutedHooks.Add("After(Test)");
        // After(Test) executed
    }
    
    // Class-level hooks
    [BeforeEvery(Class)]
    public static void BeforeEveryClass(ClassHookContext context)
    {
        ExecutedHooks.Add("BeforeEvery(Class)");
        // BeforeEvery(Class) executed
    }
    
    [AfterEvery(Class)]
    public static void AfterEveryClass(ClassHookContext context)
    {
        ExecutedHooks.Add("AfterEvery(Class)");
        // AfterEvery(Class) executed
    }
    
    [Before(Class)]
    public static void BeforeClass(ClassHookContext context)
    {
        ExecutedHooks.Add("Before(Class)");
        // Before(Class) executed
    }
    
    [After(Class)]
    public static void AfterClass(ClassHookContext context)
    {
        ExecutedHooks.Add("After(Class)");
        // After(Class) executed
    }
    
    // Assembly-level hooks
    [BeforeEvery(Assembly)]
    public static void BeforeEveryAssembly(AssemblyHookContext context)
    {
        ExecutedHooks.Add("BeforeEvery(Assembly)");
        // BeforeEvery(Assembly) executed
    }
    
    [AfterEvery(Assembly)]
    public static void AfterEveryAssembly(AssemblyHookContext context)
    {
        ExecutedHooks.Add("AfterEvery(Assembly)");
        // AfterEvery(Assembly) executed
    }
    
    [Before(Assembly)]
    public static void BeforeAssembly(AssemblyHookContext context)
    {
        ExecutedHooks.Add("Before(Assembly)");
        // Before(Assembly) executed
    }
    
    [After(Assembly)]
    public static void AfterAssembly(AssemblyHookContext context)
    {
        ExecutedHooks.Add("After(Assembly)");
        // After(Assembly) executed
    }
    
    // TestSession hooks
    [Before(TestSession)]
    public static void BeforeTestSession(TestSessionContext context)
    {
        ExecutedHooks.Add("Before(TestSession)");
        // Before(TestSession) executed
    }
    
    [After(TestSession)]
    public static void AfterTestSession(TestSessionContext context)
    {
        ExecutedHooks.Add("After(TestSession)");
        // After(TestSession) executed
    }
    
    [AfterEvery(TestSession)]
    public static void AfterEveryTestSession(TestSessionContext context)
    {
        ExecutedHooks.Add("AfterEvery(TestSession)");
        // AfterEvery(TestSession) executed
    }
    
    [BeforeEvery(TestSession)]
    public static void BeforeEveryTestSession(TestSessionContext context)
    {
        ExecutedHooks.Add("BeforeEvery(TestSession)");
        // BeforeEvery(TestSession) executed
    }
    
    // TestDiscovery hooks
    [Before(TestDiscovery)]
    public static void BeforeTestDiscovery(TestDiscoveryContext context)
    {
        ExecutedHooks.Add("Before(TestDiscovery)");
        // Before(TestDiscovery) executed
    }
    
    [After(TestDiscovery)]
    public static void AfterTestDiscovery(TestDiscoveryContext context)
    {
        ExecutedHooks.Add("After(TestDiscovery)");
        // After(TestDiscovery) executed
    }
    
    [BeforeEvery(TestDiscovery)]
    public static void BeforeEveryTestDiscovery(TestDiscoveryContext context)
    {
        ExecutedHooks.Add("BeforeEvery(TestDiscovery)");
        // BeforeEvery(TestDiscovery) executed
    }
    
    [AfterEvery(TestDiscovery)]
    public static void AfterEveryTestDiscovery(TestDiscoveryContext context)
    {
        ExecutedHooks.Add("AfterEvery(TestDiscovery)");
        // AfterEvery(TestDiscovery) executed
    }
}