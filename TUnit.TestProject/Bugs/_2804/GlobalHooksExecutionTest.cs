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
        Console.WriteLine("Test executed - waiting for hooks");
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
        Console.WriteLine($"[HOOK] BeforeEvery(Test) executed for {context.TestDetails.TestName}");
    }
    
    [AfterEvery(Test)]
    public static void AfterEveryTest(TestContext context)
    {
        ExecutedHooks.Add("AfterEvery(Test)");
        Console.WriteLine($"[HOOK] AfterEvery(Test) executed for {context.TestDetails.TestName}");
    }
    
    [Before(Test)]
    public void BeforeTest(TestContext context)
    {
        ExecutedHooks.Add("Before(Test)");
        Console.WriteLine($"[HOOK] Before(Test) executed for {context.TestDetails.TestName}");
    }
    
    [After(Test)]
    public void AfterTest(TestContext context)
    {
        ExecutedHooks.Add("After(Test)");
        Console.WriteLine($"[HOOK] After(Test) executed for {context.TestDetails.TestName}");
    }
    
    // Class-level hooks
    [BeforeEvery(Class)]
    public static void BeforeEveryClass(ClassHookContext context)
    {
        ExecutedHooks.Add("BeforeEvery(Class)");
        Console.WriteLine($"[HOOK] BeforeEvery(Class) executed for {context.ClassType.FullName}");
    }
    
    [AfterEvery(Class)]
    public static void AfterEveryClass(ClassHookContext context)
    {
        ExecutedHooks.Add("AfterEvery(Class)");
        Console.WriteLine($"[HOOK] AfterEvery(Class) executed for {context.ClassType.FullName}");
    }
    
    [Before(Class)]
    public static void BeforeClass(ClassHookContext context)
    {
        ExecutedHooks.Add("Before(Class)");
        Console.WriteLine($"[HOOK] Before(Class) executed for {context.ClassType.FullName}");
    }
    
    [After(Class)]
    public static void AfterClass(ClassHookContext context)
    {
        ExecutedHooks.Add("After(Class)");
        Console.WriteLine($"[HOOK] After(Class) executed for {context.ClassType.FullName}");
    }
    
    // Assembly-level hooks
    [BeforeEvery(Assembly)]
    public static void BeforeEveryAssembly(AssemblyHookContext context)
    {
        ExecutedHooks.Add("BeforeEvery(Assembly)");
        Console.WriteLine($"[HOOK] BeforeEvery(Assembly) executed for {context.Assembly.GetName().Name}");
    }
    
    [AfterEvery(Assembly)]
    public static void AfterEveryAssembly(AssemblyHookContext context)
    {
        ExecutedHooks.Add("AfterEvery(Assembly)");
        Console.WriteLine($"[HOOK] AfterEvery(Assembly) executed for {context.Assembly.GetName().Name}");
    }
    
    [Before(Assembly)]
    public static void BeforeAssembly(AssemblyHookContext context)
    {
        ExecutedHooks.Add("Before(Assembly)");
        Console.WriteLine($"[HOOK] Before(Assembly) executed for {context.Assembly.GetName().Name}");
    }
    
    [After(Assembly)]
    public static void AfterAssembly(AssemblyHookContext context)
    {
        ExecutedHooks.Add("After(Assembly)");
        Console.WriteLine($"[HOOK] After(Assembly) executed for {context.Assembly.GetName().Name}");
    }
    
    // TestSession hooks
    [Before(TestSession)]
    public static void BeforeTestSession(TestSessionContext context)
    {
        ExecutedHooks.Add("Before(TestSession)");
        Console.WriteLine($"[HOOK] Before(TestSession) executed with {context.AllTests.Count()} tests");
    }
    
    [After(TestSession)]
    public static void AfterTestSession(TestSessionContext context)
    {
        ExecutedHooks.Add("After(TestSession)");
        Console.WriteLine($"[HOOK] After(TestSession) executed with {context.AllTests.Count()} tests");
        
        // Log summary at the end
        Console.WriteLine("\n=== HOOK EXECUTION SUMMARY ===");
        Console.WriteLine($"Total hooks executed: {ExecutedHooks.Count}");
        
        var groupedHooks = ExecutedHooks.GroupBy(h => h)
            .OrderBy(g => g.Key);
        
        foreach (var hookGroup in groupedHooks)
        {
            Console.WriteLine($"  {hookGroup.Key}: {hookGroup.Count()} time(s)");
        }
        
        // Check if critical hooks are missing
        var expectedHooks = new[]
        {
            "BeforeEvery(Test)", "AfterEvery(Test)",
            "Before(Test)", "After(Test)",
            "BeforeEvery(Class)", "AfterEvery(Class)",
            "Before(Class)", "After(Class)",
            "BeforeEvery(Assembly)", "AfterEvery(Assembly)",
            "Before(Assembly)", "After(Assembly)",
            "Before(TestSession)", "After(TestSession)"
        };
        
        var missingHooks = expectedHooks.Where(h => !ExecutedHooks.Contains(h)).ToList();
        
        if (missingHooks.Any())
        {
            Console.WriteLine("\n⚠️ WARNING: The following hooks did NOT execute:");
            foreach (var hook in missingHooks)
            {
                Console.WriteLine($"  ❌ {hook}");
            }
        }
        else
        {
            Console.WriteLine("\n✅ All expected hooks executed successfully!");
        }
    }
    
    [AfterEvery(TestSession)]
    public static void AfterEveryTestSession(TestSessionContext context)
    {
        ExecutedHooks.Add("AfterEvery(TestSession)");
        Console.WriteLine($"[HOOK] AfterEvery(TestSession) executed with {context.AllTests.Count()} tests");
    }
    
    [BeforeEvery(TestSession)]
    public static void BeforeEveryTestSession(TestSessionContext context)
    {
        ExecutedHooks.Add("BeforeEvery(TestSession)");
        Console.WriteLine($"[HOOK] BeforeEvery(TestSession) executed with {context.AllTests.Count()} tests");
    }
    
    // TestDiscovery hooks
    [Before(TestDiscovery)]
    public static void BeforeTestDiscovery(TestDiscoveryContext context)
    {
        ExecutedHooks.Add("Before(TestDiscovery)");
        Console.WriteLine($"[HOOK] Before(TestDiscovery) executed with {context.AllTests.Count()} tests");
    }
    
    [After(TestDiscovery)]
    public static void AfterTestDiscovery(TestDiscoveryContext context)
    {
        ExecutedHooks.Add("After(TestDiscovery)");
        Console.WriteLine($"[HOOK] After(TestDiscovery) executed with {context.AllTests.Count()} tests");
    }
    
    [BeforeEvery(TestDiscovery)]
    public static void BeforeEveryTestDiscovery(TestDiscoveryContext context)
    {
        ExecutedHooks.Add("BeforeEvery(TestDiscovery)");
        Console.WriteLine($"[HOOK] BeforeEvery(TestDiscovery) executed with {context.AllTests.Count()} tests");
    }
    
    [AfterEvery(TestDiscovery)]
    public static void AfterEveryTestDiscovery(TestDiscoveryContext context)
    {
        ExecutedHooks.Add("AfterEvery(TestDiscovery)");
        Console.WriteLine($"[HOOK] AfterEvery(TestDiscovery) executed with {context.AllTests.Count()} tests");
    }
}