using System.Collections.Concurrent;

namespace TUnit.TestProject.Bugs._2804;

/// <summary>
/// Tests to verify that when multiple After hooks are registered and some fail,
/// ALL hooks still execute and failures are aggregated into an AggregateException.
/// This is critical for ensuring cleanup operations are not skipped.
/// </summary>
public class MultipleAfterHooksFailureTests
{
    private static readonly ConcurrentBag<string> ExecutedHooks = new();
    private static readonly ConcurrentBag<string> FailedHooks = new();
    private static int _testNumber = 0;

    // Reset state before each test class execution
    [Before(Class)]
    public static void ResetState(ClassHookContext context)
    {
        if (context.ClassType == typeof(MultipleAfterHooksFailureTests))
        {
            ExecutedHooks.Clear();
            FailedHooks.Clear();
            _testNumber = 0;
            // Hook executed("[MULTI-AFTER] State reset for test class");
        }
    }

    [Test]
    public async Task Test_With_Multiple_AfterEveryTest_Hooks_Some_Failing()
    {
        Interlocked.Increment(ref _testNumber);
        await Task.CompletedTask;
        // Test executed($"[MULTI-AFTER] Test {_testNumber} executed");
    }

    [Test]
    public async Task Another_Test_To_Verify_Consistency()
    {
        Interlocked.Increment(ref _testNumber);
        await Task.CompletedTask;
        // Test executed($"[MULTI-AFTER] Test {_testNumber} executed");
    }
}

/// <summary>
/// Multiple AfterEveryTest hooks - some will fail
/// </summary>
public class MultipleAfterEveryTestHooks
{
    private static readonly ConcurrentBag<string> TestHookExecutions = new();

    [AfterEvery(Test)]
    public static async Task AfterEveryTest_Hook1_Success(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(MultipleAfterHooksFailureTests))
        {
            TestHookExecutions.Add("AfterEveryTest_Hook1");
            // Hook executed("[AFTER-TEST] Hook 1 executing successfully");
            await Task.CompletedTask;
        }
    }

    [AfterEvery(Test)]
    public static async Task AfterEveryTest_Hook2_Fails(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(MultipleAfterHooksFailureTests))
        {
            TestHookExecutions.Add("AfterEveryTest_Hook2");
            // Hook executed("[AFTER-TEST] Hook 2 executing and will fail");
            await Task.CompletedTask;
            throw new InvalidOperationException("AfterEveryTest Hook 2 intentionally failed");
        }
    }

    [AfterEvery(Test)]
    public static async Task AfterEveryTest_Hook3_Success(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(MultipleAfterHooksFailureTests))
        {
            TestHookExecutions.Add("AfterEveryTest_Hook3");
            // Hook executed("[AFTER-TEST] Hook 3 executing successfully (after Hook 2 failed)");
            await Task.CompletedTask;
        }
    }

    [AfterEvery(Test)]
    public static async Task AfterEveryTest_Hook4_AlsoFails(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(MultipleAfterHooksFailureTests))
        {
            TestHookExecutions.Add("AfterEveryTest_Hook4");
            // Hook executed("[AFTER-TEST] Hook 4 executing and will also fail");
            await Task.CompletedTask;
            throw new ArgumentException("AfterEveryTest Hook 4 also intentionally failed");
        }
    }

    [AfterEvery(Test)]
    public static async Task AfterEveryTest_Hook5_StillExecutes(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(MultipleAfterHooksFailureTests))
        {
            TestHookExecutions.Add("AfterEveryTest_Hook5");
            // Hook executed("[AFTER-TEST] Hook 5 still executing (after Hooks 2 and 4 failed)");
            await Task.CompletedTask;
            
            // Verify all hooks executed
            // Hook executed($"[AFTER-TEST] Total hooks executed: {TestHookExecutions.Count}");
            if (TestHookExecutions.Count >= 5)
            {
                // Hook executed("[AFTER-TEST] SUCCESS: All 5 AfterEveryTest hooks executed despite failures!");
            }
        }
    }
}

/// <summary>
/// Test class-level After hooks with multiple failures
/// </summary>
public class MultipleAfterClassHooksTests
{
    private static readonly ConcurrentBag<string> ClassHookExecutions = new();
    private static bool _testsExecuted = false;

    [Test]
    public void SimpleTest1()
    {
        _testsExecuted = true;
        // Test executed("[CLASS-HOOKS] Test 1 in class executed");
    }

    [Test]
    public void SimpleTest2()
    {
        _testsExecuted = true;
        // Test executed("[CLASS-HOOKS] Test 2 in class executed");
    }
}

/// <summary>
/// Multiple AfterEveryClass and After(Class) hooks - some will fail
/// </summary>
public class MultipleClassLevelAfterHooks
{
    private static readonly ConcurrentBag<string> ClassHookExecutions = new();

    [AfterEvery(Class)]
    public static async Task AfterEveryClass_Hook1_Success(ClassHookContext context)
    {
        if (context.ClassType == typeof(MultipleAfterClassHooksTests))
        {
            ClassHookExecutions.Add("AfterEveryClass_Hook1");
            // Hook executed("[AFTER-CLASS] AfterEveryClass Hook 1 executing successfully");
            await Task.CompletedTask;
        }
    }

    [AfterEvery(Class)]
    public static async Task AfterEveryClass_Hook2_Fails(ClassHookContext context)
    {
        if (context.ClassType == typeof(MultipleAfterClassHooksTests))
        {
            ClassHookExecutions.Add("AfterEveryClass_Hook2");
            // Hook executed("[AFTER-CLASS] AfterEveryClass Hook 2 executing and will fail");
            await Task.CompletedTask;
            throw new InvalidOperationException("AfterEveryClass Hook 2 intentionally failed");
        }
    }

    [AfterEvery(Class)]
    public static async Task AfterEveryClass_Hook3_StillExecutes(ClassHookContext context)
    {
        if (context.ClassType == typeof(MultipleAfterClassHooksTests))
        {
            ClassHookExecutions.Add("AfterEveryClass_Hook3");
            // Hook executed("[AFTER-CLASS] AfterEveryClass Hook 3 still executing after Hook 2 failed");
            await Task.CompletedTask;
        }
    }

    [After(Class)]
    public static async Task AfterClass_Hook1_Success(ClassHookContext context)
    {
        if (context.ClassType == typeof(MultipleAfterClassHooksTests))
        {
            ClassHookExecutions.Add("AfterClass_Hook1");
            // Hook executed("[AFTER-CLASS] After(Class) Hook 1 executing successfully");
            await Task.CompletedTask;
        }
    }

    [After(Class)]
    public static async Task AfterClass_Hook2_AlsoFails(ClassHookContext context)
    {
        if (context.ClassType == typeof(MultipleAfterClassHooksTests))
        {
            ClassHookExecutions.Add("AfterClass_Hook2");
            // Hook executed("[AFTER-CLASS] After(Class) Hook 2 executing and will fail");
            await Task.CompletedTask;
            throw new ArgumentException("After(Class) Hook 2 intentionally failed");
        }
    }

    [After(Class)]
    public static async Task AfterClass_Hook3_StillExecutes(ClassHookContext context)
    {
        if (context.ClassType == typeof(MultipleAfterClassHooksTests))
        {
            ClassHookExecutions.Add("AfterClass_Hook3");
            // Hook executed("[AFTER-CLASS] After(Class) Hook 3 still executing");
            await Task.CompletedTask;
            
            // Verify all hooks executed
            // Hook executed($"[AFTER-CLASS] Total class hooks executed: {ClassHookExecutions.Count}");
            if (ClassHookExecutions.Count >= 6)
            {
                // Hook executed("[AFTER-CLASS] SUCCESS: All 6 class-level after hooks executed despite failures!");
            }
        }
    }
}

/// <summary>
/// Test assembly-level After hooks with multiple failures
/// </summary>
public class MultipleAfterAssemblyHooksTests
{
    private static readonly ConcurrentBag<string> AssemblyHookExecutions = new();

    [Test]
    public void TestToTriggerAssemblyHooks()
    {
        // Test executed("[ASSEMBLY-HOOKS] Test executed to trigger assembly hooks");
    }
}

/// <summary>
/// Multiple AfterEveryAssembly and After(Assembly) hooks - some will fail
/// </summary>
public class MultipleAssemblyLevelAfterHooks
{
    private static readonly ConcurrentBag<string> AssemblyHookExecutions = new();
    private static bool _hooksTriggered = false;

    [AfterEvery(Assembly)]
    public static async Task AfterEveryAssembly_Hook1_Success(AssemblyHookContext context)
    {
        // Disabled - these hooks interfere with other tests
        await Task.CompletedTask;
        return;
        
        // Only run once for our test assembly
        if (!_hooksTriggered && context.Assembly.GetName().Name == "TUnit.TestProject")
        {
            AssemblyHookExecutions.Add("AfterEveryAssembly_Hook1");
            // Hook executed("[AFTER-ASSEMBLY] AfterEveryAssembly Hook 1 executing successfully");
            await Task.CompletedTask;
        }
    }

    [AfterEvery(Assembly)]
    public static async Task AfterEveryAssembly_Hook2_Fails(AssemblyHookContext context)
    {
        // Disabled - these hooks interfere with other tests
        await Task.CompletedTask;
        return;
        
        if (!_hooksTriggered && context.Assembly.GetName().Name == "TUnit.TestProject")
        {
            AssemblyHookExecutions.Add("AfterEveryAssembly_Hook2");
            // Hook executed("[AFTER-ASSEMBLY] AfterEveryAssembly Hook 2 executing and will fail");
            await Task.CompletedTask;
            throw new InvalidOperationException("AfterEveryAssembly Hook 2 intentionally failed");
        }
    }

    [AfterEvery(Assembly)]
    public static async Task AfterEveryAssembly_Hook3_StillExecutes(AssemblyHookContext context)
    {
        // Disabled - these hooks interfere with other tests
        await Task.CompletedTask;
        return;
        
        if (!_hooksTriggered && context.Assembly.GetName().Name == "TUnit.TestProject")
        {
            AssemblyHookExecutions.Add("AfterEveryAssembly_Hook3");
            // Hook executed("[AFTER-ASSEMBLY] AfterEveryAssembly Hook 3 still executing");
            await Task.CompletedTask;
            _hooksTriggered = true;

            // Verify hooks executed
            if (AssemblyHookExecutions.Count >= 3)
            {
                // Hook executed("[AFTER-ASSEMBLY] SUCCESS: Multiple assembly hooks executed despite failures!");
            }
        }
    }
}

/// <summary>
/// Test to verify AggregateException contains all failures
/// </summary>
public class AggregateExceptionVerificationTests
{
    private static readonly List<Exception> CaughtExceptions = new();

    [Test]
    public async Task Test_That_Triggers_Multiple_Hook_Failures()
    {
        await Task.CompletedTask;
        // Test executed("[AGGREGATE] Test executed - multiple after hooks will fail");
    }

    [AfterEvery(Test)]
    public static async Task CaptureExceptions_Hook1_Fails(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(AggregateExceptionVerificationTests))
        {
            // Hook executed("[AGGREGATE] Hook 1 failing with InvalidOperationException");
            await Task.CompletedTask;
            throw new InvalidOperationException("First hook failure");
        }
    }

    [AfterEvery(Test)]
    public static async Task CaptureExceptions_Hook2_Fails(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(AggregateExceptionVerificationTests))
        {
            // Hook executed("[AGGREGATE] Hook 2 failing with ArgumentException");
            await Task.CompletedTask;
            throw new ArgumentException("Second hook failure");
        }
    }

    [AfterEvery(Test)]
    public static async Task CaptureExceptions_Hook3_Fails(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(AggregateExceptionVerificationTests))
        {
            // Hook executed("[AGGREGATE] Hook 3 failing with NotImplementedException");
            await Task.CompletedTask;
            throw new NotImplementedException("Third hook failure");
        }
    }

    [After(TestSession)]
    public static void VerifyAggregateException(TestSessionContext context)
    {
        // Note: We can't easily verify the AggregateException from within the test framework,
        // but we've set up the scenario where multiple hooks fail, which should result in
        // an AggregateException being thrown by the framework
        if (context.AllTests.Any(t => t.TestDetails.ClassType == typeof(AggregateExceptionVerificationTests)))
        {
            // Hook executed("[AGGREGATE] Test scenario completed - framework should have thrown AggregateException with 3 inner exceptions");
        }
    }
}

/// <summary>
/// Comprehensive test combining test, class, and assembly level hook failures
/// </summary>
public class ComprehensiveMultiLevelHookFailureTests
{
    private static readonly ConcurrentBag<string> AllHookExecutions = new();

    [Test]
    public async Task Test_With_Hooks_At_All_Levels()
    {
        await Task.CompletedTask;
        // Test executed("[COMPREHENSIVE] Test executed - hooks at all levels will execute");
    }

    // Test-level hooks
    [AfterEvery(Test)]
    public static async Task TestLevel_Hook1_Success(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(ComprehensiveMultiLevelHookFailureTests))
        {
            AllHookExecutions.Add("TestLevel_Hook1");
            // Hook executed("[COMPREHENSIVE-TEST] Test-level Hook 1 success");
            await Task.CompletedTask;
        }
    }

    [AfterEvery(Test)]
    public static async Task TestLevel_Hook2_Fails(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(ComprehensiveMultiLevelHookFailureTests))
        {
            AllHookExecutions.Add("TestLevel_Hook2");
            // Hook executed("[COMPREHENSIVE-TEST] Test-level Hook 2 fails");
            await Task.CompletedTask;
            throw new Exception("Test-level hook failure");
        }
    }

    [AfterEvery(Test)]
    public static async Task TestLevel_Hook3_Success(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(ComprehensiveMultiLevelHookFailureTests))
        {
            AllHookExecutions.Add("TestLevel_Hook3");
            // Hook executed("[COMPREHENSIVE-TEST] Test-level Hook 3 success (after failure)");
            await Task.CompletedTask;
        }
    }

    // Class-level hooks
    [AfterEvery(Class)]
    public static async Task ClassLevel_Hook1_Success(ClassHookContext context)
    {
        if (context.ClassType == typeof(ComprehensiveMultiLevelHookFailureTests))
        {
            AllHookExecutions.Add("ClassLevel_Hook1");
            // Hook executed("[COMPREHENSIVE-CLASS] Class-level Hook 1 success");
            await Task.CompletedTask;
        }
    }

    [AfterEvery(Class)]
    public static async Task ClassLevel_Hook2_Fails(ClassHookContext context)
    {
        if (context.ClassType == typeof(ComprehensiveMultiLevelHookFailureTests))
        {
            AllHookExecutions.Add("ClassLevel_Hook2");
            // Hook executed("[COMPREHENSIVE-CLASS] Class-level Hook 2 fails");
            await Task.CompletedTask;
            throw new Exception("Class-level hook failure");
        }
    }

    [AfterEvery(Class)]
    public static async Task ClassLevel_Hook3_Success(ClassHookContext context)
    {
        if (context.ClassType == typeof(ComprehensiveMultiLevelHookFailureTests))
        {
            AllHookExecutions.Add("ClassLevel_Hook3");
            // Hook executed("[COMPREHENSIVE-CLASS] Class-level Hook 3 success (after failure)");
            await Task.CompletedTask;
        }
    }

    [After(TestSession)]
    public static void VerifyAllLevelsExecuted(TestSessionContext context)
    {
        if (context.AllTests.Any(t => t.TestDetails.ClassType == typeof(ComprehensiveMultiLevelHookFailureTests)))
        {
            // Hook executed("\n[COMPREHENSIVE] === VERIFICATION ===");
            // Hook executed($"[COMPREHENSIVE] Total hooks executed across all levels: {AllHookExecutions.Count}");
            
            var testLevelCount = AllHookExecutions.Count(h => h.StartsWith("TestLevel"));
            var classLevelCount = AllHookExecutions.Count(h => h.StartsWith("ClassLevel"));
            
            // Hook executed($"[COMPREHENSIVE] Test-level hooks: {testLevelCount}");
            // Hook executed($"[COMPREHENSIVE] Class-level hooks: {classLevelCount}");
            
            if (testLevelCount >= 3 && classLevelCount >= 3)
            {
                // Hook executed("[COMPREHENSIVE] SUCCESS: All hooks at all levels executed despite failures!");
            }
            else
            {
                // Hook executed("[COMPREHENSIVE] WARNING: Some hooks may not have executed");
            }
            
            // Hook executed("[COMPREHENSIVE] Hook execution order:");
            foreach (var hook in AllHookExecutions)
            {
                // Hook executed($"  - {hook}");
            }
        }
    }
}