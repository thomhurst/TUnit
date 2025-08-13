using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

// This test verifies that NotInParallel tests are grouped by class
// and executed sequentially within each class before moving to the next class

// Test classes for NotInParallel grouping
[NotInParallel]
[EngineTest(ExpectedResult.Pass)]
public class NotInParallelClassGroupingTests_ClassA
{
    internal static readonly ConcurrentQueue<string> ExecutionOrder = new();
    
    [Test, NotInParallel(Order = 1)]
    public async Task Test1()
    {
        ExecutionOrder.Enqueue($"ClassA.Test1");
        await Task.Delay(10);
    }

    [Test, NotInParallel(Order = 2)]
    public async Task Test2()
    {
        ExecutionOrder.Enqueue($"ClassA.Test2");
        await Task.Delay(10);
    }

    [Test, NotInParallel(Order = 3)]
    public async Task Test3()
    {
        ExecutionOrder.Enqueue($"ClassA.Test3");
        await Task.Delay(10);
    }
}

[NotInParallel]
[EngineTest(ExpectedResult.Pass)]
public class NotInParallelClassGroupingTests_ClassB
{
    [Test, NotInParallel(Order = 1)]
    public async Task Test1()
    {
        NotInParallelClassGroupingTests_ClassA.ExecutionOrder.Enqueue($"ClassB.Test1");
        await Task.Delay(10);
    }

    [Test, NotInParallel(Order = 2)]
    public async Task Test2()
    {
        NotInParallelClassGroupingTests_ClassA.ExecutionOrder.Enqueue($"ClassB.Test2");
        await Task.Delay(10);
    }
}

[NotInParallel]
[EngineTest(ExpectedResult.Pass)]
public class NotInParallelClassGroupingTests_ClassC
{
    [Test, NotInParallel(Order = 1)]
    public async Task Test1()
    {
        NotInParallelClassGroupingTests_ClassA.ExecutionOrder.Enqueue($"ClassC.Test1");
        await Task.Delay(10);
    }

    [Test, NotInParallel(Order = 2)]
    public async Task Test2()
    {
        NotInParallelClassGroupingTests_ClassA.ExecutionOrder.Enqueue($"ClassC.Test2");
        await Task.Delay(10);
    }

    [Test, NotInParallel(Order = 3)]
    public async Task Test3()
    {
        NotInParallelClassGroupingTests_ClassA.ExecutionOrder.Enqueue($"ClassC.Test3");
        await Task.Delay(10);
    }
}

// Verification test that runs last
[EngineTest(ExpectedResult.Pass)]
public class NotInParallelClassGroupingTests_Verify
{
    [Test, NotInParallel(Order = int.MaxValue)]
    public async Task VerifyClassGrouping()
    {
        // Allow time for all tests to complete
        await Task.Delay(200);

        var order = NotInParallelClassGroupingTests_ClassA.ExecutionOrder.ToList();
        
        // We should have 8 test executions (3 from ClassA, 2 from ClassB, 3 from ClassC)
        await Assert.That(order).HasCount(8);

        // Verify that all tests from one class complete before another class starts
        var classSequence = new List<string>();
        string? lastClass = null;
        
        foreach (var execution in order)
        {
            var className = execution.Split('.')[0];
            if (className != lastClass)
            {
                classSequence.Add(className);
                lastClass = className;
            }
        }

        // Each class should appear exactly once in the sequence
        // (meaning no interleaving of classes)
        await Assert.That(classSequence.Distinct().Count()).IsEqualTo(3);
        await Assert.That(classSequence).HasCount(3);

        // Verify test order within each class
        var classATests = order.Where(o => o.StartsWith("ClassA.")).ToList();
        var classBTests = order.Where(o => o.StartsWith("ClassB.")).ToList();
        var classCTests = order.Where(o => o.StartsWith("ClassC.")).ToList();

        // Check ClassA test order
        await Assert.That(classATests).HasCount(3);
        await Assert.That(classATests[0]).IsEqualTo("ClassA.Test1");
        await Assert.That(classATests[1]).IsEqualTo("ClassA.Test2");
        await Assert.That(classATests[2]).IsEqualTo("ClassA.Test3");

        // Check ClassB test order
        await Assert.That(classBTests).HasCount(2);
        await Assert.That(classBTests[0]).IsEqualTo("ClassB.Test1");
        await Assert.That(classBTests[1]).IsEqualTo("ClassB.Test2");

        // Check ClassC test order
        await Assert.That(classCTests).HasCount(3);
        await Assert.That(classCTests[0]).IsEqualTo("ClassC.Test1");
        await Assert.That(classCTests[1]).IsEqualTo("ClassC.Test2");
        await Assert.That(classCTests[2]).IsEqualTo("ClassC.Test3");
    }
}