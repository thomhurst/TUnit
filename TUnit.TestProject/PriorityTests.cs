using TUnit.Core.Enums;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(PriorityTests))]  // Add class-level to ensure sequential execution
public class PriorityTests
{
    private static readonly List<string> ExecutionOrder = [];
    private static readonly object Lock = new();

    [Test, ExecutionPriority(Priority.Critical)]
    public async Task CriticalPriority_Test()
    {
        Console.WriteLine($"[Execution] Starting {nameof(CriticalPriority_Test)}");
        lock (Lock)
        {
            ExecutionOrder.Add(nameof(CriticalPriority_Test));
            Console.WriteLine($"[Execution] Added {nameof(CriticalPriority_Test)} at position {ExecutionOrder.Count - 1}");
        }
        await Task.Delay(100);
    }

    [Test, ExecutionPriority(Priority.High)]
    public async Task HighPriority_Test1()
    {
        Console.WriteLine($"[Execution] Starting {nameof(HighPriority_Test1)}");
        lock (Lock)
        {
            ExecutionOrder.Add(nameof(HighPriority_Test1));
            Console.WriteLine($"[Execution] Added {nameof(HighPriority_Test1)} at position {ExecutionOrder.Count - 1}");
        }
        await Task.Delay(100);
    }

    [Test, ExecutionPriority(Priority.High)]
    public async Task HighPriority_Test2()
    {
        Console.WriteLine($"[Execution] Starting {nameof(HighPriority_Test2)}");
        lock (Lock)
        {
            ExecutionOrder.Add(nameof(HighPriority_Test2));
            Console.WriteLine($"[Execution] Added {nameof(HighPriority_Test2)} at position {ExecutionOrder.Count - 1}");
        }
        await Task.Delay(100);
    }

    [Test, ExecutionPriority(Priority.Normal)]
    public async Task NormalPriority_Test()
    {
        Console.WriteLine($"[Execution] Starting {nameof(NormalPriority_Test)}");
        lock (Lock)
        {
            ExecutionOrder.Add(nameof(NormalPriority_Test));
            Console.WriteLine($"[Execution] Added {nameof(NormalPriority_Test)} at position {ExecutionOrder.Count - 1}");
        }
        await Task.Delay(100);
    }

    [Test, ExecutionPriority(Priority.Low)]
    public async Task LowPriority_Test()
    {
        Console.WriteLine($"[Execution] Starting {nameof(LowPriority_Test)}");
        lock (Lock)
        {
            ExecutionOrder.Add(nameof(LowPriority_Test));
            Console.WriteLine($"[Execution] Added {nameof(LowPriority_Test)} at position {ExecutionOrder.Count - 1}");
        }
        await Task.Delay(100);
    }

    [Test, ExecutionPriority(Priority.Low)]
    public async Task VerifyPriorityOrder()
    {
        Console.WriteLine($"[Execution] Starting {nameof(VerifyPriorityOrder)}");
        await Task.Delay(500); // Give other tests time to complete
        
        lock (Lock)
        {
            Console.WriteLine($"[Execution] Final order: {string.Join(", ", ExecutionOrder)}");
        }
        
        // Critical should execute first
        await Assert.That(ExecutionOrder.First()).IsEqualTo(nameof(CriticalPriority_Test));
        
        // High priority tests should execute before Normal and Low
        var highPriorityIndex1 = ExecutionOrder.IndexOf(nameof(HighPriority_Test1));
        var highPriorityIndex2 = ExecutionOrder.IndexOf(nameof(HighPriority_Test2));
        var normalPriorityIndex = ExecutionOrder.IndexOf(nameof(NormalPriority_Test));
        var lowPriorityIndex = ExecutionOrder.IndexOf(nameof(LowPriority_Test));
        
        await Assert.That(highPriorityIndex1).IsLessThan(normalPriorityIndex);
        await Assert.That(highPriorityIndex2).IsLessThan(normalPriorityIndex);
        await Assert.That(normalPriorityIndex).IsLessThan(lowPriorityIndex);
    }
}