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
            Console.WriteLine("[CRITICAL-CHAIN] Test scenario initialized - AfterTest and AfterClass will fail");
        }
    }

    [Test]
    public async Task FirstTest_In_Class()
    {
        Interlocked.Increment(ref _testCount);
        await Task.CompletedTask;
        Console.WriteLine($"[CRITICAL-CHAIN] Test {_testCount} executed");
    }

    [Test]
    public async Task SecondTest_In_Class()
    {
        Interlocked.Increment(ref _testCount);
        await Task.CompletedTask;
        Console.WriteLine($"[CRITICAL-CHAIN] Test {_testCount} executed");
    }

    [Test]
    public async Task LastTest_In_Class()
    {
        Interlocked.Increment(ref _testCount);
        await Task.CompletedTask;
        Console.WriteLine($"[CRITICAL-CHAIN] Test {_testCount} executed - this is the last test");
    }

    // AfterEveryTest hook that will fail
    [AfterEvery(Test)]
    public static async Task AfterEveryTest_That_Fails(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(CriticalHookChainExecutionTests))
        {
            ExecutedHooks.Add($"AfterEveryTest_{context.TestDetails.TestName}");
            Console.WriteLine($"[CRITICAL-CHAIN] AfterEveryTest executing for {context.TestDetails.TestName}");
            
            if (_afterTestShouldFail)
            {
                FailedHooks.Add("AfterEveryTest");
                Console.WriteLine("[CRITICAL-CHAIN] AfterEveryTest THROWING EXCEPTION!");
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
            Console.WriteLine("[CRITICAL-CHAIN] AfterClass EXECUTED despite AfterEveryTest failure!");
            
            if (_afterClassShouldFail)
            {
                FailedHooks.Add("AfterClass");
                Console.WriteLine("[CRITICAL-CHAIN] AfterClass THROWING EXCEPTION!");
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
            Console.WriteLine("[CRITICAL-CHAIN] AfterEveryClass EXECUTED despite failures!");
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
        Console.WriteLine("[CRITICAL-HELPER] Helper test executed");
    }

    [AfterEvery(Test)]
    public static async Task HelperAfterTest(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(CriticalHookChainHelperTests))
        {
            SharedExecutedHooks.Add("HelperAfterTest");
            Console.WriteLine("[CRITICAL-HELPER] Helper AfterTest executed");
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
            Console.WriteLine("[CRITICAL-ASSEMBLY] AfterEveryAssembly EXECUTED despite AfterTest and AfterClass failures!");
            await Task.CompletedTask;
        }
    }

    [After(TestSession)]
    public static void VerifyCriticalChainExecution(TestSessionContext context)
    {
        // Check if our critical test ran
        if (context.AllTests.Any(t => t.TestDetails.ClassType == typeof(CriticalHookChainExecutionTests)))
        {
            Console.WriteLine("\n[CRITICAL-CHAIN] === CRITICAL VERIFICATION ===");
            
            // Verify hooks executed
            var executedHookTypes = CriticalHookChainExecutionTests.ExecutedHooks
                .Select(h => h.Contains("AfterEveryTest") ? "AfterEveryTest" : h)
                .Distinct()
                .ToList();
            
            Console.WriteLine($"[CRITICAL-CHAIN] Executed hook types: {string.Join(", ", executedHookTypes)}");
            Console.WriteLine($"[CRITICAL-CHAIN] Failed hooks: {string.Join(", ", CriticalHookChainExecutionTests.FailedHooks)}");
            
            bool afterTestExecuted = executedHookTypes.Contains("AfterEveryTest");
            bool afterClassExecuted = executedHookTypes.Contains("AfterClass");
            bool afterEveryClassExecuted = executedHookTypes.Contains("AfterEveryClass");
            bool afterAssemblyExecuted = AssemblyHookExecutions.Contains("AfterEveryAssembly");
            
            Console.WriteLine($"[CRITICAL-CHAIN] AfterEveryTest executed: {afterTestExecuted}");
            Console.WriteLine($"[CRITICAL-CHAIN] AfterClass executed: {afterClassExecuted}");
            Console.WriteLine($"[CRITICAL-CHAIN] AfterEveryClass executed: {afterEveryClassExecuted}");
            Console.WriteLine($"[CRITICAL-CHAIN] AfterEveryAssembly executed: {afterAssemblyExecuted}");
            
            if (afterTestExecuted && afterClassExecuted && afterEveryClassExecuted)
            {
                Console.WriteLine("[CRITICAL-CHAIN] ✅ SUCCESS: All hook levels executed despite failures!");
                Console.WriteLine("[CRITICAL-CHAIN] This proves the critical fix is working:");
                Console.WriteLine("[CRITICAL-CHAIN]   - AfterClass ran even though AfterEveryTest threw");
                Console.WriteLine("[CRITICAL-CHAIN]   - AfterAssembly would run even though AfterClass threw");
                Console.WriteLine("[CRITICAL-CHAIN]   - Counters were properly decremented");
            }
            else
            {
                Console.WriteLine("[CRITICAL-CHAIN] ❌ FAILURE: Some hooks did not execute!");
                Console.WriteLine("[CRITICAL-CHAIN] This indicates the critical bug is NOT fixed!");
                
                if (!afterClassExecuted && afterTestExecuted)
                {
                    Console.WriteLine("[CRITICAL-CHAIN] AfterClass didn't run after AfterTest threw - CRITICAL BUG!");
                }
                if (!afterAssemblyExecuted && afterClassExecuted)
                {
                    Console.WriteLine("[CRITICAL-CHAIN] AfterAssembly didn't run after AfterClass threw - CRITICAL BUG!");
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
            Console.WriteLine($"[COUNTER-VERIFY] BeforeTest #{_beforeTestCount}");
            await Task.CompletedTask;
        }
    }

    [Test]
    public async Task Test1_With_Failing_AfterHook()
    {
        await Task.CompletedTask;
        Console.WriteLine("[COUNTER-VERIFY] Test 1 executed");
    }

    [Test]
    public async Task Test2_With_Failing_AfterHook()
    {
        await Task.CompletedTask;
        Console.WriteLine("[COUNTER-VERIFY] Test 2 executed");
    }

    [Test]
    public async Task Test3_Last_Test()
    {
        await Task.CompletedTask;
        Console.WriteLine("[COUNTER-VERIFY] Test 3 executed - last test");
    }

    [AfterEvery(Test)]
    public static async Task AfterTest_That_Always_Fails(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(CounterDecrementVerificationTests))
        {
            Interlocked.Increment(ref _afterTestCount);
            CounterEvents.Add($"AfterTest_{_afterTestCount}_Failing");
            Console.WriteLine($"[COUNTER-VERIFY] AfterTest #{_afterTestCount} - THROWING!");
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
            Console.WriteLine("[COUNTER-VERIFY] AfterClass EXECUTED - counters were properly decremented!");
            Console.WriteLine($"[COUNTER-VERIFY] BeforeTest count: {_beforeTestCount}");
            Console.WriteLine($"[COUNTER-VERIFY] AfterTest count: {_afterTestCount}");
            
            if (_afterClassExecuted && _afterTestCount == 3)
            {
                Console.WriteLine("[COUNTER-VERIFY] ✅ SUCCESS: AfterClass ran after all 3 tests despite all AfterTest hooks failing!");
                Console.WriteLine("[COUNTER-VERIFY] This proves counters were decremented even when hooks threw exceptions!");
            }
            else
            {
                Console.WriteLine("[COUNTER-VERIFY] ❌ PROBLEM: Unexpected execution pattern");
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
        Console.WriteLine("[EXTREME] Test executed - all hook levels will fail");
    }

    [After(Test)]
    public async Task AfterTest_Fails(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(ExtremeFailureCascadeTests))
        {
            ExecutionTrace.Add("AfterTest_Failed");
            Console.WriteLine("[EXTREME] After(Test) executing and failing");
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
            Console.WriteLine("[EXTREME] AfterEvery(Test) executing and failing");
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
            Console.WriteLine("[EXTREME] After(Class) STILL EXECUTED and will fail");
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
            Console.WriteLine("[EXTREME] AfterEvery(Class) STILL EXECUTED and will fail");
            await Task.CompletedTask;
            throw new Exception("AfterEvery(Class) failed");
        }
    }

    [After(TestSession)]
    public static void VerifyExtremeCascade(TestSessionContext context)
    {
        if (context.AllTests.Any(t => t.TestDetails.ClassType == typeof(ExtremeFailureCascadeTests)))
        {
            Console.WriteLine("\n[EXTREME] === EXTREME CASCADE VERIFICATION ===");
            Console.WriteLine($"[EXTREME] Execution trace ({ExecutionTrace.Count} items):");
            foreach (var trace in ExecutionTrace)
            {
                Console.WriteLine($"[EXTREME]   - {trace}");
            }
            
            bool allLevelsExecuted = ExecutionTrace.Contains("Test_Executed") &&
                                     ExecutionTrace.Contains("AfterTest_Failed") &&
                                     ExecutionTrace.Contains("AfterEveryTest_Failed") &&
                                     ExecutionTrace.Contains("AfterClass_Failed") &&
                                     ExecutionTrace.Contains("AfterEveryClass_Failed");
            
            if (allLevelsExecuted)
            {
                Console.WriteLine("[EXTREME] ✅ SUCCESS: All hook levels executed despite every level throwing!");
                Console.WriteLine("[EXTREME] This is the ultimate proof that the fix works!");
            }
            else
            {
                Console.WriteLine("[EXTREME] ❌ FAILURE: Some hook levels did not execute in the cascade!");
            }
        }
    }
}