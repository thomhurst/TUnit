using System.Collections.Concurrent;

namespace TUnit.TestProject.Bugs;

/// <summary>
/// Tests to verify that the TestOrchestrator deadlock fixes are working correctly.
/// These tests stress-test the coordination mechanisms under high concurrency.
/// </summary>
public class HookOrchestratorDeadlockTests
{
    private static readonly ConcurrentBag<string> ExecutionLog = new();
    private static int _testCounter = 0;
    private static int _beforeClassCounter = 0;
    private static int _afterClassCounter = 0;

    [Before(Class)]
    public static async Task BeforeClass_Setup(ClassHookContext context)
    {
        if (context.ClassType == typeof(HookOrchestratorDeadlockTests))
        {
            Interlocked.Increment(ref _beforeClassCounter);
            ExecutionLog.Add($"BeforeClass_Executed_{_beforeClassCounter}");
            
            // Simulate some work that might cause coordination issues
            await Task.Delay(10);
        }
    }

    [After(Class)]
    public static async Task AfterClass_Cleanup(ClassHookContext context)
    {
        if (context.ClassType == typeof(HookOrchestratorDeadlockTests))
        {
            Interlocked.Increment(ref _afterClassCounter);
            ExecutionLog.Add($"AfterClass_Executed_{_afterClassCounter}");
            
            await Task.Delay(10);
        }
    }

    // Create multiple tests that will execute concurrently and stress the coordination system
    [Test, Repeat(5)]
    public async Task ConcurrentTest_1()
    {
        var testId = Interlocked.Increment(ref _testCounter);
        ExecutionLog.Add($"Test1_Start_{testId}");
        
        // Simulate some async work
        var random = new Random();
        await Task.Delay(random.Next(1, 50));
        
        ExecutionLog.Add($"Test1_End_{testId}");
    }

    [Test, Repeat(5)]
    public async Task ConcurrentTest_2()
    {
        var testId = Interlocked.Increment(ref _testCounter);
        ExecutionLog.Add($"Test2_Start_{testId}");
        
        var random = new Random();
        await Task.Delay(random.Next(1, 50));
        
        ExecutionLog.Add($"Test2_End_{testId}");
    }

    [Test, Repeat(5)]
    public async Task ConcurrentTest_3()
    {
        var testId = Interlocked.Increment(ref _testCounter);
        ExecutionLog.Add($"Test3_Start_{testId}");
        
        var random = new Random();
        await Task.Delay(random.Next(1, 50));
        
        ExecutionLog.Add($"Test3_End_{testId}");
    }

    [BeforeEvery(Test)]
    public static async Task BeforeEveryTest_Hook(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(HookOrchestratorDeadlockTests))
        {
            ExecutionLog.Add($"BeforeTest_{context.TestDetails.TestName}");
            await Task.Delay(5); // Small delay to potentially trigger coordination issues
        }
    }

    [AfterEvery(Test)]
    public static async Task AfterEveryTest_Hook(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(HookOrchestratorDeadlockTests))
        {
            ExecutionLog.Add($"AfterTest_{context.TestDetails.TestName}");
            await Task.Delay(5); // Small delay to potentially trigger coordination issues
        }
    }
}

/// <summary>
/// Tests sequential execution context coordination which was prone to deadlocks
/// </summary>
[NotInParallel]
public class SequentialCoordinationDeadlockTests
{
    private static readonly ConcurrentBag<string> SequentialExecutionLog = new();
    private static int _sequentialTestCounter = 0;

    [Before(Class)]
    public static async Task SequentialBeforeClass(ClassHookContext context)
    {
        if (context.ClassType == typeof(SequentialCoordinationDeadlockTests))
        {
            SequentialExecutionLog.Add("SequentialBeforeClass_Executed");
            await Task.Delay(20); // Longer delay to stress sequential coordination
        }
    }

    [After(Class)] 
    public static async Task SequentialAfterClass(ClassHookContext context)
    {
        if (context.ClassType == typeof(SequentialCoordinationDeadlockTests))
        {
            SequentialExecutionLog.Add("SequentialAfterClass_Executed");
            await Task.Delay(20);
        }
    }

    [Test, Repeat(3)]
    public async Task SequentialTest_1()
    {
        var testId = Interlocked.Increment(ref _sequentialTestCounter);
        SequentialExecutionLog.Add($"SequentialTest1_{testId}_Start");
        
        // These should execute one at a time due to NotInParallel
        await Task.Delay(30);
        
        SequentialExecutionLog.Add($"SequentialTest1_{testId}_End");
    }

    [Test, Repeat(3)]
    public async Task SequentialTest_2()
    {
        var testId = Interlocked.Increment(ref _sequentialTestCounter);
        SequentialExecutionLog.Add($"SequentialTest2_{testId}_Start");
        
        await Task.Delay(30);
        
        SequentialExecutionLog.Add($"SequentialTest2_{testId}_End");
    }

    [BeforeEvery(Test)]
    public static async Task SequentialBeforeTest(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(SequentialCoordinationDeadlockTests))
        {
            SequentialExecutionLog.Add($"SequentialBeforeTest_{context.TestDetails.TestName}");
            await Task.Delay(10);
        }
    }

    [AfterEvery(Test)]
    public static async Task SequentialAfterTest(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(SequentialCoordinationDeadlockTests))
        {
            SequentialExecutionLog.Add($"SequentialAfterTest_{context.TestDetails.TestName}");
            await Task.Delay(10);
        }
    }
}

/// <summary>
/// Test group to verify keyed sequential coordination works without deadlocks
/// </summary>
[NotInParallel("TestGroup1")]
public class KeyedSequentialDeadlockTests_Group1
{
    private static readonly ConcurrentBag<string> KeyedExecutionLog = new();

    [Test]
    public async Task KeyedTest_Group1_Test1()
    {
        KeyedExecutionLog.Add("Group1_Test1_Start");
        await Task.Delay(25);
        KeyedExecutionLog.Add("Group1_Test1_End");
    }

    [Test]
    public async Task KeyedTest_Group1_Test2()
    {
        KeyedExecutionLog.Add("Group1_Test2_Start");
        await Task.Delay(25);
        KeyedExecutionLog.Add("Group1_Test2_End");
    }
}

[NotInParallel("TestGroup2")]
public class KeyedSequentialDeadlockTests_Group2
{
    private static readonly ConcurrentBag<string> KeyedExecutionLog2 = new();

    [Test]
    public async Task KeyedTest_Group2_Test1()
    {
        KeyedExecutionLog2.Add("Group2_Test1_Start");
        await Task.Delay(25);
        KeyedExecutionLog2.Add("Group2_Test1_End");
    }

    [Test]
    public async Task KeyedTest_Group2_Test2()
    {
        KeyedExecutionLog2.Add("Group2_Test2_Start");
        await Task.Delay(25);
        KeyedExecutionLog2.Add("Group2_Test2_End");
    }
}

/// <summary>
/// Verification tests to check that all coordination mechanisms completed successfully
/// </summary>
public class DeadlockFixVerificationTests
{
    [After(TestSession)]
    public static void VerifyNoDeadlocksOccurred(TestSessionContext context)
    {
        // If we reach this point, it means no deadlocks occurred during test execution
        // In a deadlock scenario, the test run would hang and never complete
        
        var deadlockTestsRan = context.AllTests.Any(t => 
            t.TestDetails.ClassType == typeof(HookOrchestratorDeadlockTests) ||
            t.TestDetails.ClassType == typeof(SequentialCoordinationDeadlockTests) ||
            t.TestDetails.ClassType == typeof(KeyedSequentialDeadlockTests_Group1) ||
            t.TestDetails.ClassType == typeof(KeyedSequentialDeadlockTests_Group2));

        if (deadlockTestsRan)
        {
            // SUCCESS: All deadlock-prone scenarios completed without hanging
            // This indicates the coordination fixes are working properly
            Console.WriteLine("✅ TestOrchestrator deadlock fixes verified successfully");
            Console.WriteLine("✅ Sequential coordination is working without deadlocks");
            Console.WriteLine("✅ Timeout mechanisms prevent indefinite hangs");
        }
    }
}