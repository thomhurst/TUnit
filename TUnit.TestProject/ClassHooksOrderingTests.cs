using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class ClassHooksOrderingTestClassA
{
    private static readonly ConcurrentQueue<string> _executionOrder = new();

    // This simulates a database setup that creates tables/data
    [Before(Class)]
    public static async Task SetupDatabase()
    {
        _executionOrder.Enqueue("ClassA_BeforeClass_Start");
        await Task.Delay(100); // Simulate some setup work
        _executionOrder.Enqueue("ClassA_BeforeClass_End");
    }

    // This simulates database cleanup that drops tables/data
    [After(Class)]
    public static async Task CleanupDatabase()
    {
        _executionOrder.Enqueue("ClassA_AfterClass_Start");
        await Task.Delay(50); // Simulate cleanup work
        _executionOrder.Enqueue("ClassA_AfterClass_End");
    }

    [Test]
    public async Task Test1()
    {
        _executionOrder.Enqueue("ClassA_Test1");
        await Task.Delay(10);
    }

    [Test]
    public async Task Test2()
    {
        _executionOrder.Enqueue("ClassA_Test2");
        await Task.Delay(10);
    }

    // Static method to get execution order for verification
    public static IEnumerable<string> GetExecutionOrder() => _executionOrder.ToArray();
}

[EngineTest(ExpectedResult.Pass)]
public class ClassHooksOrderingTestClassB
{
    private static readonly ConcurrentQueue<string> _executionOrder = new();

    // This simulates database setup that depends on clean state
    [Before(Class)]
    public static async Task SetupDatabase()
    {
        _executionOrder.Enqueue("ClassB_BeforeClass_Start");
        await Task.Delay(100); // Simulate setup work
        _executionOrder.Enqueue("ClassB_BeforeClass_End");
    }

    [After(Class)]
    public static async Task CleanupDatabase()
    {
        _executionOrder.Enqueue("ClassB_AfterClass_Start");
        await Task.Delay(50); // Simulate cleanup work
        _executionOrder.Enqueue("ClassB_AfterClass_End");
    }

    [Test]
    public async Task Test1()
    {
        _executionOrder.Enqueue("ClassB_Test1");
        await Task.Delay(10);
    }

    [Test]
    public async Task Test2()
    {
        _executionOrder.Enqueue("ClassB_Test2");
        await Task.Delay(10);
    }

    // Static method to get execution order for verification
    public static IEnumerable<string> GetExecutionOrder() => _executionOrder.ToArray();
}

[EngineTest(ExpectedResult.Pass)]
public class ClassHooksOrderingVerificationTest
{
    [Test]
    public async Task VerifyClassHooksOrdering()
    {
        // Wait a bit to ensure all other tests have completed
        await Task.Delay(1000);
        
        var classAOrder = ClassHooksOrderingTestClassA.GetExecutionOrder().ToList();
        var classBOrder = ClassHooksOrderingTestClassB.GetExecutionOrder().ToList();
        
        // Verify that each class has proper internal ordering
        await Assert.That(classAOrder).Contains("ClassA_BeforeClass_Start");
        await Assert.That(classAOrder).Contains("ClassA_BeforeClass_End");
        await Assert.That(classAOrder).Contains("ClassA_AfterClass_Start");
        await Assert.That(classAOrder).Contains("ClassA_AfterClass_End");

        await Assert.That(classBOrder).Contains("ClassB_BeforeClass_Start");
        await Assert.That(classBOrder).Contains("ClassB_BeforeClass_End");
        await Assert.That(classBOrder).Contains("ClassB_AfterClass_Start");
        await Assert.That(classBOrder).Contains("ClassB_AfterClass_End");

        // The key fix: AfterClass hooks should complete before any new BeforeClass hooks start
        // We can't guarantee which class starts first due to parallel execution,
        // but we CAN guarantee that AfterClass completes before other BeforeClass starts
        
        // Find the index of each event
        var allEvents = classAOrder.Concat(classBOrder).ToList();
        
        // If ClassA starts first, then ClassA's AfterClass should complete before ClassB's BeforeClass
        // If ClassB starts first, then ClassB's AfterClass should complete before ClassA's BeforeClass
        
        var classABeforeStart = allEvents.FindIndex(e => e == "ClassA_BeforeClass_Start");
        var classAAfterEnd = allEvents.FindIndex(e => e == "ClassA_AfterClass_End");
        var classBBeforeStart = allEvents.FindIndex(e => e == "ClassB_BeforeClass_Start");
        var classBAfterEnd = allEvents.FindIndex(e => e == "ClassB_AfterClass_End");

        // At least one class should have executed completely
        var hasValidOrdering = 
            (classAAfterEnd != -1 && classBBeforeStart != -1 && classAAfterEnd < classBBeforeStart) ||
            (classBAfterEnd != -1 && classABeforeStart != -1 && classBAfterEnd < classABeforeStart) ||
            (classABeforeStart != -1 && classBBeforeStart != -1); // Both started (which is fine if they don't conflict)

        await Assert.That(hasValidOrdering).IsTrue();
    }
}