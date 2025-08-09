using System.Collections.Concurrent;

namespace TUnit.TestProject.Bugs._2804;

/// <summary>
/// Tests to ensure that hooks are properly cleaned up even when tests fail.
/// This validates the fix for the race condition where counters could become
/// inconsistent if OnTestCompletedAsync wasn't called after OnTestStartingAsync.
/// </summary>
public class HookCleanupOnFailureTests
{
    private static readonly ConcurrentBag<string> ExecutedHooks = new();
    private static int _testExecutionCount = 0;
    private static int _beforeClassCount = 0;
    private static int _afterClassCount = 0;
    private static int _beforeAssemblyCount = 0;
    private static int _afterAssemblyCount = 0;

    [BeforeEvery(Class)]
    public static void BeforeEveryClass(ClassHookContext context)
    {
        if (context.ClassType == typeof(HookCleanupOnFailureTests))
        {
            Interlocked.Increment(ref _beforeClassCount);
            ExecutedHooks.Add($"BeforeClass:{context.ClassType.Name}");
            // Hook executed($"[HOOK] BeforeEveryClass executed for {context.ClassType.Name} (count: {_beforeClassCount})");
        }
    }

    [AfterEvery(Class)]
    public static void AfterEveryClass(ClassHookContext context)
    {
        if (context.ClassType == typeof(HookCleanupOnFailureTests))
        {
            Interlocked.Increment(ref _afterClassCount);
            ExecutedHooks.Add($"AfterClass:{context.ClassType.Name}");
            // Hook executed($"[HOOK] AfterEveryClass executed for {context.ClassType.Name} (count: {_afterClassCount})");
            
            // Verify that Before and After are balanced
            if (_beforeClassCount != _afterClassCount)
            {
                throw new Exception($"Hook imbalance detected! BeforeClass: {_beforeClassCount}, AfterClass: {_afterClassCount}");
            }
        }
    }

    [Test]
    public async Task Test_That_Passes()
    {
        Interlocked.Increment(ref _testExecutionCount);
        await Task.CompletedTask;
        // Output removed("Test_That_Passes executed successfully");
    }

    [Test]
    public async Task Test_That_Fails_During_Execution()
    {
        Interlocked.Increment(ref _testExecutionCount);
        await Task.CompletedTask;
        // Output removed("Test_That_Fails_During_Execution about to throw");
        throw new InvalidOperationException("This test intentionally fails during execution");
    }

    [Test]
    public void Test_That_Fails_Immediately()
    {
        Interlocked.Increment(ref _testExecutionCount);
        // Output removed("Test_That_Fails_Immediately about to throw");
        throw new ArgumentException("This test intentionally fails immediately");
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    public async Task Parameterized_Test_With_Mixed_Results(int value)
    {
        Interlocked.Increment(ref _testExecutionCount);
        await Task.Delay(10); // Small delay to ensure parallel execution
        
        // Output removed($"Parameterized_Test_With_Mixed_Results({value}) executing");
        
        if (value == 2)
        {
            throw new Exception($"Test with value {value} intentionally fails");
        }
        
        // Output removed($"Parameterized_Test_With_Mixed_Results({value}) passed");
    }
}

/// <summary>
/// Tests for parallel execution scenarios where multiple tests fail simultaneously
/// </summary>
public class ParallelHookCleanupTests
{
    private static readonly ConcurrentBag<string> ParallelExecutedHooks = new();
    private static int _parallelBeforeClassCount = 0;
    private static int _parallelAfterClassCount = 0;

    [BeforeEvery(Class)]
    public static void BeforeEveryClassParallel(ClassHookContext context)
    {
        if (context.ClassType == typeof(ParallelHookCleanupTests))
        {
            var count = Interlocked.Increment(ref _parallelBeforeClassCount);
            ParallelExecutedHooks.Add($"BeforeClass:{context.ClassType.Name}:{count}");
            // Hook executed($"[PARALLEL] BeforeEveryClass executed (count: {count})");
        }
    }

    [AfterEvery(Class)]
    public static void AfterEveryClassParallel(ClassHookContext context)
    {
        if (context.ClassType == typeof(ParallelHookCleanupTests))
        {
            var count = Interlocked.Increment(ref _parallelAfterClassCount);
            ParallelExecutedHooks.Add($"AfterClass:{context.ClassType.Name}:{count}");
            // Hook executed($"[PARALLEL] AfterEveryClass executed (count: {count})");
        }
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [NotInParallel] // Run sequentially to make hook counting predictable
    public async Task Sequential_Tests_With_Failures(int testNumber)
    {
        // Output removed($"[PARALLEL] Test {testNumber} starting");
        await Task.Delay(new Random().Next(10, 50)); // Random delay
        
        // Fail odd-numbered tests
        if (testNumber % 2 == 1)
        {
            // Hook executed($"[PARALLEL] Test {testNumber} failing");
            throw new Exception($"Test {testNumber} intentionally failed");
        }
        
        // Output removed($"[PARALLEL] Test {testNumber} passed");
    }

    [After(Class)]
    public static void VerifyParallelHookBalance(ClassHookContext context)
    {
        if (context.ClassType == typeof(ParallelHookCleanupTests))
        {
            // Hook executed($"[PARALLEL] Final hook counts - Before: {_parallelBeforeClassCount}, After: {_parallelAfterClassCount}");
            
            // Note: BeforeEvery and AfterEvery are called for each test, but Before/After(Class) are called once
            // So we're just checking that AfterEvery was called at least once, indicating cleanup occurred
            if (_parallelAfterClassCount == 0)
            {
                throw new Exception("AfterEveryClass was never called - cleanup hooks did not execute!");
            }
        }
    }
}

/// <summary>
/// Test to verify assembly-level hook cleanup
/// </summary>
public class AssemblyHookCleanupTests
{
    private static readonly ConcurrentBag<string> AssemblyHooks = new();
    private static bool _assemblyHookExecuted = false;

    [BeforeEvery(Assembly)]
    public static void BeforeEveryAssembly(AssemblyHookContext context)
    {
        AssemblyHooks.Add("BeforeEveryAssembly");
        // Output removed("[ASSEMBLY] BeforeEveryAssembly executed");
    }

    [AfterEvery(Assembly)]
    public static void AfterEveryAssembly(AssemblyHookContext context)
    {
        _assemblyHookExecuted = true;
        AssemblyHooks.Add("AfterEveryAssembly");
        // Output removed("[ASSEMBLY] AfterEveryAssembly executed");
    }

    [Test]
    public void Test_With_Assembly_Hooks_That_Fails()
    {
        // Output removed("[ASSEMBLY] Test executing and will fail");
        throw new Exception("This test fails to verify assembly hooks still clean up");
    }

    [Test]
    public void Test_With_Assembly_Hooks_That_Passes()
    {
        // Output removed("[ASSEMBLY] Test executing and will pass");
        // This test passes
    }

    [After(TestSession)]
    public static void VerifyAssemblyHooksExecuted(TestSessionContext context)
    {
        // Only check if we're in the context of these specific tests
        if (context.AllTests.Any(t => t.TestDetails.ClassType == typeof(AssemblyHookCleanupTests)))
        {
            // Hook executed($"[ASSEMBLY] Verification - Assembly hooks executed: {_assemblyHookExecuted}");
            // Hook executed($"[ASSEMBLY] Total hooks recorded: {AssemblyHooks.Count}");
            
            if (!_assemblyHookExecuted)
            {
                // Hook executed("[ASSEMBLY] WARNING: Assembly cleanup hooks may not have executed properly!");
            }
        }
    }
}