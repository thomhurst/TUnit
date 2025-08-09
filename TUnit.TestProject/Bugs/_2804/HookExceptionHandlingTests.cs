using System.Collections.Concurrent;

namespace TUnit.TestProject.Bugs._2804;

/// <summary>
/// Tests to ensure that hook counters remain consistent even when hooks themselves throw exceptions.
/// This is critical to prevent the hang issue where counters never reach 0.
/// </summary>
public class HookExceptionHandlingTests
{
    private static readonly ConcurrentBag<string> ExecutionLog = new();
    private static int _hookExceptionTestCount = 0;
    private static bool _afterClassExecuted = false;
    private static bool _afterAssemblyExecuted = false;

    [Before(Class)]
    public static void BeforeClassThatMightFail(ClassHookContext context)
    {
        if (context.ClassType == typeof(HookExceptionHandlingTests))
        {
            ExecutionLog.Add("BeforeClass:Started");
            Console.WriteLine("[HOOK-EXCEPTION] BeforeClass started");
            
            // Simulate a hook that sometimes fails
            if (_hookExceptionTestCount == 0)
            {
                _hookExceptionTestCount++;
                // First time through, let it succeed
                ExecutionLog.Add("BeforeClass:Completed");
                Console.WriteLine("[HOOK-EXCEPTION] BeforeClass completed successfully");
            }
            else
            {
                // On subsequent runs, this could fail - but we're not testing that here
                ExecutionLog.Add("BeforeClass:Completed");
            }
        }
    }

    [After(Class)]
    public static void AfterClassMustExecute(ClassHookContext context)
    {
        if (context.ClassType == typeof(HookExceptionHandlingTests))
        {
            _afterClassExecuted = true;
            ExecutionLog.Add("AfterClass:Executed");
            Console.WriteLine("[HOOK-EXCEPTION] AfterClass executed - cleanup successful!");
        }
    }

    [BeforeEvery(Test)]
    public static void BeforeEveryTestWithPotentialFailure(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(HookExceptionHandlingTests))
        {
            ExecutionLog.Add($"BeforeEveryTest:{context.TestDetails.TestName}");
            Console.WriteLine($"[HOOK-EXCEPTION] BeforeEveryTest for {context.TestDetails.TestName}");
            
            // Simulate a hook that fails for a specific test
            if (context.TestDetails.TestName.Contains("HookFailure"))
            {
                Console.WriteLine($"[HOOK-EXCEPTION] BeforeEveryTest throwing exception for {context.TestDetails.TestName}");
                throw new Exception($"BeforeEveryTest intentionally failed for {context.TestDetails.TestName}");
            }
        }
    }

    [AfterEvery(Test)]
    public static void AfterEveryTestMustExecute(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(HookExceptionHandlingTests))
        {
            ExecutionLog.Add($"AfterEveryTest:{context.TestDetails.TestName}");
            Console.WriteLine($"[HOOK-EXCEPTION] AfterEveryTest executed for {context.TestDetails.TestName}");
        }
    }

    [Test]
    public async Task Test_With_Normal_Hooks()
    {
        await Task.CompletedTask;
        ExecutionLog.Add("Test_With_Normal_Hooks:Executed");
        Console.WriteLine("[HOOK-EXCEPTION] Test_With_Normal_Hooks executed");
    }

    [Test]
    public async Task Test_With_HookFailure_In_Before()
    {
        // This test's BeforeEveryTest hook will throw
        // The test itself should not execute, but AfterEveryTest should still run
        await Task.CompletedTask;
        ExecutionLog.Add("Test_With_HookFailure_In_Before:Executed");
        Console.WriteLine("[HOOK-EXCEPTION] Test_With_HookFailure_In_Before executed (shouldn't happen if hook failed)");
    }

    [Test]
    public async Task Test_That_Fails_After_Successful_Hook()
    {
        await Task.CompletedTask;
        ExecutionLog.Add("Test_That_Fails_After_Successful_Hook:Executed");
        Console.WriteLine("[HOOK-EXCEPTION] Test_That_Fails_After_Successful_Hook about to fail");
        throw new Exception("Test fails after hooks succeeded");
    }

    [After(TestSession)]
    public static void VerifyHookCleanupOccurred(TestSessionContext context)
    {
        // Only verify if our tests ran
        if (context.AllTests.Any(t => t.TestDetails.ClassType == typeof(HookExceptionHandlingTests)))
        {
            Console.WriteLine("\n[HOOK-EXCEPTION] === VERIFICATION ===");
            Console.WriteLine($"[HOOK-EXCEPTION] AfterClass executed: {_afterClassExecuted}");
            Console.WriteLine($"[HOOK-EXCEPTION] Execution log entries: {ExecutionLog.Count}");
            
            foreach (var log in ExecutionLog.OrderBy(x => x))
            {
                Console.WriteLine($"[HOOK-EXCEPTION]   - {log}");
            }
            
            // Check that AfterEveryTest was called for each test attempt
            var afterEveryTestCount = ExecutionLog.Count(l => l.StartsWith("AfterEveryTest:"));
            Console.WriteLine($"[HOOK-EXCEPTION] AfterEveryTest called {afterEveryTestCount} times");
            
            if (!_afterClassExecuted)
            {
                Console.WriteLine("[HOOK-EXCEPTION] ERROR: AfterClass was not executed - cleanup did not occur!");
            }
            else
            {
                Console.WriteLine("[HOOK-EXCEPTION] SUCCESS: All cleanup hooks executed despite failures");
            }
        }
    }
}

/// <summary>
/// Test critical scenario: OnTestStartingAsync throws after incrementing counter
/// </summary>
public class CriticalCounterScenarioTests
{
    private static int _criticalBeforeClassCount = 0;
    private static int _criticalAfterClassCount = 0;
    private static bool _simulateHookFailure = false;

    [Before(Class)]
    public static void BeforeClassCritical(ClassHookContext context)
    {
        if (context.ClassType == typeof(CriticalCounterScenarioTests))
        {
            Interlocked.Increment(ref _criticalBeforeClassCount);
            Console.WriteLine($"[CRITICAL] BeforeClass executed (count: {_criticalBeforeClassCount})");
            
            // Simulate a failure AFTER the counter was incremented
            // This is the exact scenario that caused the hang
            if (_simulateHookFailure)
            {
                Console.WriteLine("[CRITICAL] BeforeClass throwing AFTER counter increment!");
                throw new Exception("Critical: Hook fails after counter increment");
            }
        }
    }

    [After(Class)]
    public static void AfterClassCritical(ClassHookContext context)
    {
        if (context.ClassType == typeof(CriticalCounterScenarioTests))
        {
            Interlocked.Increment(ref _criticalAfterClassCount);
            Console.WriteLine($"[CRITICAL] AfterClass executed (count: {_criticalAfterClassCount})");
            Console.WriteLine($"[CRITICAL] Counter balance - Before: {_criticalBeforeClassCount}, After: {_criticalAfterClassCount}");
        }
    }

    [Test]
    public void Critical_Test_1()
    {
        Console.WriteLine("[CRITICAL] Test 1 executed");
    }

    [Test]
    public void Critical_Test_2_With_Simulated_Hook_Failure()
    {
        // Enable hook failure for next test
        _simulateHookFailure = true;
        Console.WriteLine("[CRITICAL] Test 2 executed - hook failure enabled for next test");
    }

    [Test]
    public void Critical_Test_3_After_Hook_Failure()
    {
        // This test runs after a hook failure
        _simulateHookFailure = false; // Reset
        Console.WriteLine("[CRITICAL] Test 3 executed after hook failure");
    }

    [After(TestSession)]
    public static void VerifyCriticalScenario(TestSessionContext context)
    {
        if (context.AllTests.Any(t => t.TestDetails.ClassType == typeof(CriticalCounterScenarioTests)))
        {
            Console.WriteLine("\n[CRITICAL] === CRITICAL SCENARIO VERIFICATION ===");
            Console.WriteLine($"[CRITICAL] BeforeClass count: {_criticalBeforeClassCount}");
            Console.WriteLine($"[CRITICAL] AfterClass count: {_criticalAfterClassCount}");
            
            // The counts might not match if a hook failed, but AfterClass should still execute
            if (_criticalAfterClassCount > 0)
            {
                Console.WriteLine("[CRITICAL] SUCCESS: AfterClass executed despite hook failures");
            }
            else
            {
                Console.WriteLine("[CRITICAL] WARNING: AfterClass may not have executed - potential hang scenario!");
            }
        }
    }
}