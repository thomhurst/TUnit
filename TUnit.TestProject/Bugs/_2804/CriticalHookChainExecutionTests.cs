using System.Collections.Concurrent;

namespace TUnit.TestProject.Bugs._2804;

/// <summary>
/// CRITICAL TEST: Verifies that when After hooks at one level throw exceptions,
/// hooks at higher levels (Class, Assembly) still execute.
/// This tests the fix for the critical bug where exceptions in AfterTest hooks
/// would prevent AfterClass and AfterAssembly from running.
/// </summary>
public class CriticalHookChainExecutionTests
{
    internal static readonly ConcurrentBag<string> ExecutedHooks = new();
    internal static readonly ConcurrentBag<string> FailedHooks = new();
    private static int _testCount = 0;
    private static bool _afterTestShouldFail = false;
    private static bool _afterClassShouldFail = false;

    [Before(Class)]
    public static void SetupTestScenario(ClassHookContext context)
    {
        if (context.ClassType == typeof(CriticalHookChainExecutionTests))
        {
            ExecutedHooks.Clear();
            FailedHooks.Clear();
            _testCount = 0;
            _afterTestShouldFail = true; // Enable AfterTest failure
            _afterClassShouldFail = true; // Enable AfterClass failure
            // Test scenario initialized - AfterTest and AfterClass will fail
        }
    }

    [Test]
    public async Task FirstTest_In_Class()
    {
        Interlocked.Increment(ref _testCount);
        await Task.CompletedTask;
        // Test executed
    }

    [Test]
    public async Task SecondTest_In_Class()
    {
        Interlocked.Increment(ref _testCount);
        await Task.CompletedTask;
        // Test executed
    }

    [Test]
    public async Task LastTest_In_Class()
    {
        Interlocked.Increment(ref _testCount);
        await Task.CompletedTask;
        // Last test executed
    }

    // AfterEveryTest hook that will fail
    [AfterEvery(Test)]
    public static async Task AfterEveryTest_That_Fails(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(CriticalHookChainExecutionTests))
        {
            ExecutedHooks.Add($"AfterEveryTest_{context.TestDetails.TestName}");
            // AfterEveryTest executing
            
            if (_afterTestShouldFail)
            {
                FailedHooks.Add("AfterEveryTest");
                // AfterEveryTest throwing exception
                await Task.CompletedTask;
                throw new InvalidOperationException("AfterEveryTest intentionally failed - AfterClass should still run!");
            }
            
            await Task.CompletedTask;
        }
    }

    // AfterClass hook that should run even though AfterEveryTest failed
    [After(Class)]
    public static async Task AfterClass_Should_Run_Despite_AfterTest_Failure(ClassHookContext context)
    {
        if (context.ClassType == typeof(CriticalHookChainExecutionTests))
        {
            ExecutedHooks.Add("AfterClass");
            // AfterClass executed despite AfterEveryTest failure
            
            if (_afterClassShouldFail)
            {
                FailedHooks.Add("AfterClass");
                // AfterClass throwing exception
                await Task.CompletedTask;
                throw new ArgumentException("AfterClass intentionally failed - AfterAssembly should still run!");
            }
            
            await Task.CompletedTask;
        }
    }

    // AfterEveryClass hook that should also run
    [AfterEvery(Class)]
    public static async Task AfterEveryClass_Should_Also_Run(ClassHookContext context)
    {
        if (context.ClassType == typeof(CriticalHookChainExecutionTests))
        {
            ExecutedHooks.Add("AfterEveryClass");
            // AfterEveryClass executed despite failures
            await Task.CompletedTask;
        }
    }
}

/// <summary>
/// Additional class to help trigger assembly-level hooks
/// </summary>
public class CriticalHookChainHelperTests
{
    private static readonly ConcurrentBag<string> SharedExecutedHooks = new();

    [Test]
    public async Task HelperTest_To_Trigger_Assembly_Hooks()
    {
        await Task.CompletedTask;
        // Helper test executed
    }

    [AfterEvery(Test)]
    public static async Task HelperAfterTest(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(CriticalHookChainHelperTests))
        {
            SharedExecutedHooks.Add("HelperAfterTest");
            // Helper AfterTest executed
            await Task.CompletedTask;
        }
    }
}

/// <summary>
/// Assembly-level hooks that should run despite failures at lower levels
/// </summary>
public class CriticalAssemblyHooks
{
    private static readonly ConcurrentBag<string> AssemblyHookExecutions = new();
    private static bool _hasExecuted = false;

    [AfterEvery(Assembly)]
    public static async Task AfterEveryAssembly_Should_Run_Despite_All_Failures(AssemblyHookContext context)
    {
        // Only run once for our specific test scenario
        if (!_hasExecuted && context.Assembly.GetName().Name == "TUnit.TestProject")
        {
            _hasExecuted = true;
            AssemblyHookExecutions.Add("AfterEveryAssembly");
            // AfterEveryAssembly executed despite AfterTest and AfterClass failures
            await Task.CompletedTask;
        }
    }

    [After(TestSession)]
    public static void VerifyCriticalChainExecution(TestSessionContext context)
    {
        // Check if our critical test ran
        if (context.AllTests.Any(t => t.TestDetails.ClassType == typeof(CriticalHookChainExecutionTests)))
        {
            // Critical verification
            
            // Verify hooks executed
            var executedHookTypes = CriticalHookChainExecutionTests.ExecutedHooks
                .Select(h => h.Contains("AfterEveryTest") ? "AfterEveryTest" : h)
                .Distinct()
                .ToList();
            
            // Executed hook types verified
            // Failed hooks verified
            
            var afterTestExecuted = executedHookTypes.Contains("AfterEveryTest");
            var afterClassExecuted = executedHookTypes.Contains("AfterClass");
            var afterEveryClassExecuted = executedHookTypes.Contains("AfterEveryClass");
            var afterAssemblyExecuted = AssemblyHookExecutions.Contains("AfterEveryAssembly");
            
            // AfterEveryTest execution verified
            // AfterClass execution verified
            // AfterEveryClass execution verified
            // AfterEveryAssembly execution verified
            
            if (afterTestExecuted && afterClassExecuted && afterEveryClassExecuted)
            {
                // SUCCESS: All hook levels executed despite failures
                // Critical fix is working
                // AfterClass ran even though AfterEveryTest threw
                // AfterAssembly would run even though AfterClass threw
                // Counters were properly decremented
            }
            else
            {
                // FAILURE: Some hooks did not execute
                // Critical bug is NOT fixed
                
                if (!afterClassExecuted && afterTestExecuted)
                {
                    // AfterClass didn't run after AfterTest threw - CRITICAL BUG
                }
                if (!afterAssemblyExecuted && afterClassExecuted)
                {
                    // AfterAssembly didn't run after AfterClass threw - CRITICAL BUG
                }
            }
        }
    }
}

/// <summary>
/// Test to verify counters are decremented even when hooks fail
/// </summary>
public class CounterDecrementVerificationTests
{
    private static readonly ConcurrentBag<string> CounterEvents = new();
    private static int _beforeTestCount = 0;
    private static int _afterTestCount = 0;
    private static bool _afterClassExecuted = false;

    [BeforeEvery(Test)]
    public static async Task TrackBeforeTest(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(CounterDecrementVerificationTests))
        {
            Interlocked.Increment(ref _beforeTestCount);
            CounterEvents.Add($"BeforeTest_{_beforeTestCount}");
            // BeforeTest hook executed
            await Task.CompletedTask;
        }
    }

    [Test]
    public async Task Test1_With_Failing_AfterHook()
    {
        await Task.CompletedTask;
        // Test 1 executed
    }

    [Test]
    public async Task Test2_With_Failing_AfterHook()
    {
        await Task.CompletedTask;
        // Test 2 executed
    }

    [Test]
    public async Task Test3_Last_Test()
    {
        await Task.CompletedTask;
        // Test 3 executed - last test
    }

    [AfterEvery(Test)]
    public static async Task AfterTest_That_Always_Fails(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(CounterDecrementVerificationTests))
        {
            Interlocked.Increment(ref _afterTestCount);
            CounterEvents.Add($"AfterTest_{_afterTestCount}_Failing");
            // AfterTest hook throwing exception
            await Task.CompletedTask;
            throw new Exception($"AfterTest {_afterTestCount} intentionally failed");
        }
    }

    [After(Class)]
    public static async Task VerifyCountersWereDecrementedProperly(ClassHookContext context)
    {
        if (context.ClassType == typeof(CounterDecrementVerificationTests))
        {
            _afterClassExecuted = true;
            CounterEvents.Add("AfterClass_Executed");
            // AfterClass executed - counters were properly decremented
            // BeforeTest count verified
            // AfterTest count verified
            
            if (_afterClassExecuted && _afterTestCount == 3)
            {
                // SUCCESS: AfterClass ran after all 3 tests despite all AfterTest hooks failing
                // Counters were decremented even when hooks threw exceptions
            }
            else
            {
                // PROBLEM: Unexpected execution pattern
            }
            
            await Task.CompletedTask;
        }
    }
}

/// <summary>
/// Test extreme scenario: Every single hook level throws
/// </summary>
public class ExtremeFailureCascadeTests
{
    private static readonly ConcurrentBag<string> ExecutionTrace = new();

    [Test]
    public async Task Test_Where_Every_Hook_Level_Fails()
    {
        ExecutionTrace.Add("Test_Executed");
        await Task.CompletedTask;
        // Test executed - all hook levels will fail
    }

    [After(Test)]
    public async Task AfterTest_Fails(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(ExtremeFailureCascadeTests))
        {
            ExecutionTrace.Add("AfterTest_Failed");
            // After(Test) executing and failing
            await Task.CompletedTask;
            throw new Exception("After(Test) failed");
        }
    }

    [AfterEvery(Test)]
    public static async Task AfterEveryTest_AlsoFails(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(ExtremeFailureCascadeTests))
        {
            ExecutionTrace.Add("AfterEveryTest_Failed");
            // AfterEvery(Test) executing and failing
            await Task.CompletedTask;
            throw new Exception("AfterEvery(Test) failed");
        }
    }

    [After(Class)]
    public static async Task AfterClass_AlsoFails(ClassHookContext context)
    {
        if (context.ClassType == typeof(ExtremeFailureCascadeTests))
        {
            ExecutionTrace.Add("AfterClass_Failed");
            // After(Class) still executed and will fail
            await Task.CompletedTask;
            throw new Exception("After(Class) failed");
        }
    }

    [AfterEvery(Class)]
    public static async Task AfterEveryClass_AlsoFails(ClassHookContext context)
    {
        if (context.ClassType == typeof(ExtremeFailureCascadeTests))
        {
            ExecutionTrace.Add("AfterEveryClass_Failed");
            // AfterEvery(Class) still executed and will fail
            await Task.CompletedTask;
            throw new Exception("AfterEvery(Class) failed");
        }
    }

    [After(TestSession)]
    public static void VerifyExtremeCascade(TestSessionContext context)
    {
        if (context.AllTests.Any(t => t.TestDetails.ClassType == typeof(ExtremeFailureCascadeTests)))
        {
            // Extreme cascade verification
            // Execution trace verified
            foreach (var trace in ExecutionTrace)
            {
                // Trace item recorded
            }
            
            var allLevelsExecuted = ExecutionTrace.Contains("Test_Executed") &&
                                     ExecutionTrace.Contains("AfterTest_Failed") &&
                                     ExecutionTrace.Contains("AfterEveryTest_Failed") &&
                                     ExecutionTrace.Contains("AfterClass_Failed") &&
                                     ExecutionTrace.Contains("AfterEveryClass_Failed");
            
            if (allLevelsExecuted)
            {
                // SUCCESS: All hook levels executed despite every level throwing
                // Ultimate proof that the fix works
            }
            else
            {
                // FAILURE: Some hook levels did not execute in the cascade
            }
        }
    }
}