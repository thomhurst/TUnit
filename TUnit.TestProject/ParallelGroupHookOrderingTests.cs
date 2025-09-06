using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

// This test verifies that Before/After Class hooks from different parallel groups
// execute sequentially and don't interfere with each other

[ParallelGroup("ParallelGroupHookTest_Group1")]
[EngineTest(ExpectedResult.Pass)]
public class ParallelGroupHookTest_ClassA
{
    internal static readonly ConcurrentQueue<string> ExecutionOrder = new();
    
    [Before(Class)]
    public static async Task BeforeClass()
    {
        ExecutionOrder.Enqueue($"ClassA.BeforeClass.Start");
        await Task.Delay(100); // Simulate some setup work
        ExecutionOrder.Enqueue($"ClassA.BeforeClass.End");
    }
    
    [After(Class)]
    public static async Task AfterClass()
    {
        ExecutionOrder.Enqueue($"ClassA.AfterClass.Start");
        await Task.Delay(100); // Simulate some cleanup work
        ExecutionOrder.Enqueue($"ClassA.AfterClass.End");
    }
    
    [Test]
    public async Task Test1()
    {
        ExecutionOrder.Enqueue($"ClassA.Test1");
        await Task.Delay(10);
    }
    
    [Test]
    public async Task Test2()
    {
        ExecutionOrder.Enqueue($"ClassA.Test2");
        await Task.Delay(10);
    }
}

[ParallelGroup("ParallelGroupHookTest_Group2")]
[EngineTest(ExpectedResult.Pass)]
public class ParallelGroupHookTest_ClassB
{
    [Before(Class)]
    public static async Task BeforeClass()
    {
        ParallelGroupHookTest_ClassA.ExecutionOrder.Enqueue($"ClassB.BeforeClass.Start");
        await Task.Delay(100); // Simulate some setup work
        ParallelGroupHookTest_ClassA.ExecutionOrder.Enqueue($"ClassB.BeforeClass.End");
    }
    
    [After(Class)]
    public static async Task AfterClass()
    {
        ParallelGroupHookTest_ClassA.ExecutionOrder.Enqueue($"ClassB.AfterClass.Start");
        await Task.Delay(100); // Simulate some cleanup work
        ParallelGroupHookTest_ClassA.ExecutionOrder.Enqueue($"ClassB.AfterClass.End");
    }
    
    [Test]
    public async Task Test1()
    {
        ParallelGroupHookTest_ClassA.ExecutionOrder.Enqueue($"ClassB.Test1");
        await Task.Delay(10);
    }
    
    [Test]
    public async Task Test2()
    {
        ParallelGroupHookTest_ClassA.ExecutionOrder.Enqueue($"ClassB.Test2");
        await Task.Delay(10);
    }
}

[ParallelGroup("ParallelGroupHookTest_Group3")]
[EngineTest(ExpectedResult.Pass)]
public class ParallelGroupHookTest_ClassC
{
    [Before(Class)]
    public static async Task BeforeClass()
    {
        ParallelGroupHookTest_ClassA.ExecutionOrder.Enqueue($"ClassC.BeforeClass.Start");
        await Task.Delay(100); // Simulate some setup work
        ParallelGroupHookTest_ClassA.ExecutionOrder.Enqueue($"ClassC.BeforeClass.End");
    }
    
    [After(Class)]
    public static async Task AfterClass()
    {
        ParallelGroupHookTest_ClassA.ExecutionOrder.Enqueue($"ClassC.AfterClass.Start");
        await Task.Delay(100); // Simulate some cleanup work
        ParallelGroupHookTest_ClassA.ExecutionOrder.Enqueue($"ClassC.AfterClass.End");
    }
    
    [Test]
    public async Task Test1()
    {
        ParallelGroupHookTest_ClassA.ExecutionOrder.Enqueue($"ClassC.Test1");
        await Task.Delay(10);
    }
}

// Verification test that runs after all parallel groups
[EngineTest(ExpectedResult.Pass)]
public class ParallelGroupHookTest_Verify
{
    [Test]
    public async Task VerifyHookExecutionIsSequential()
    {
        // Wait a bit to ensure all tests have completed
        await Task.Delay(1000);
        
        var events = ParallelGroupHookTest_ClassA.ExecutionOrder.ToArray();
        
        // Find hook event indices
        var beforeClassStartA = Array.FindIndex(events, e => e == "ClassA.BeforeClass.Start");
        var beforeClassEndA = Array.FindIndex(events, e => e == "ClassA.BeforeClass.End");
        var beforeClassStartB = Array.FindIndex(events, e => e == "ClassB.BeforeClass.Start");
        var beforeClassEndB = Array.FindIndex(events, e => e == "ClassB.BeforeClass.End");
        var beforeClassStartC = Array.FindIndex(events, e => e == "ClassC.BeforeClass.Start");
        var beforeClassEndC = Array.FindIndex(events, e => e == "ClassC.BeforeClass.End");
        
        var afterClassStartA = Array.FindIndex(events, e => e == "ClassA.AfterClass.Start");
        var afterClassEndA = Array.FindIndex(events, e => e == "ClassA.AfterClass.End");
        var afterClassStartB = Array.FindIndex(events, e => e == "ClassB.AfterClass.Start");
        var afterClassEndB = Array.FindIndex(events, e => e == "ClassB.AfterClass.End");
        var afterClassStartC = Array.FindIndex(events, e => e == "ClassC.AfterClass.Start");
        var afterClassEndC = Array.FindIndex(events, e => e == "ClassC.AfterClass.End");
        
        // Validate that all hooks were found
        await Assert.That(beforeClassStartA).IsGreaterThanOrEqualTo(0).With("ClassA.BeforeClass.Start not found");
        await Assert.That(beforeClassEndA).IsGreaterThanOrEqualTo(0).With("ClassA.BeforeClass.End not found");
        await Assert.That(beforeClassStartB).IsGreaterThanOrEqualTo(0).With("ClassB.BeforeClass.Start not found");
        await Assert.That(beforeClassEndB).IsGreaterThanOrEqualTo(0).With("ClassB.BeforeClass.End not found");
        await Assert.That(beforeClassStartC).IsGreaterThanOrEqualTo(0).With("ClassC.BeforeClass.Start not found");
        await Assert.That(beforeClassEndC).IsGreaterThanOrEqualTo(0).With("ClassC.BeforeClass.End not found");
        
        // Verify BeforeClass hooks are sequential (one completes before next starts)
        // We need to check that no BeforeClass hook starts before a previous one ends
        var beforeClassStarts = new[] { 
            (beforeClassStartA, "A"), 
            (beforeClassStartB, "B"), 
            (beforeClassStartC, "C") 
        }.Where(x => x.Item1 >= 0).OrderBy(x => x.Item1).ToArray();
        
        var beforeClassEnds = new[] { 
            (beforeClassEndA, "A"), 
            (beforeClassEndB, "B"), 
            (beforeClassEndC, "C") 
        }.Where(x => x.Item1 >= 0).OrderBy(x => x.Item1).ToArray();
        
        // Check that BeforeClass hooks don't overlap
        for (int i = 0; i < beforeClassStarts.Length - 1; i++)
        {
            var currentStart = beforeClassStarts[i];
            var nextStart = beforeClassStarts[i + 1];
            var currentEnd = beforeClassEnds.FirstOrDefault(x => x.Item2 == currentStart.Item2);
            
            await Assert.That(currentEnd.Item1).IsLessThan(nextStart.Item1)
                .With($"BeforeClass hook for Class{currentStart.Item2} should complete before Class{nextStart.Item2} starts");
        }
        
        // Similar check for AfterClass hooks
        if (afterClassStartA >= 0 && afterClassStartB >= 0)
        {
            var afterClassStarts = new[] { 
                (afterClassStartA, "A"), 
                (afterClassStartB, "B"), 
                (afterClassStartC, "C") 
            }.Where(x => x.Item1 >= 0).OrderBy(x => x.Item1).ToArray();
            
            var afterClassEnds = new[] { 
                (afterClassEndA, "A"), 
                (afterClassEndB, "B"), 
                (afterClassEndC, "C") 
            }.Where(x => x.Item1 >= 0).OrderBy(x => x.Item1).ToArray();
            
            // Check that AfterClass hooks don't overlap
            for (int i = 0; i < afterClassStarts.Length - 1; i++)
            {
                var currentStart = afterClassStarts[i];
                var nextStart = afterClassStarts[i + 1];
                var currentEnd = afterClassEnds.FirstOrDefault(x => x.Item2 == currentStart.Item2);
                
                await Assert.That(currentEnd.Item1).IsLessThan(nextStart.Item1)
                    .With($"AfterClass hook for Class{currentStart.Item2} should complete before Class{nextStart.Item2} starts");
            }
        }
    }
}