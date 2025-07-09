using TUnit.Core.Enums;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class PriorityTests
{
    private static readonly List<string> ExecutionOrder = [];
    private static readonly object Lock = new();

    [Test, ExecutionPriority(Priority.Critical)]
    public async Task CriticalPriority_Test()
    {
        lock (Lock)
        {
            ExecutionOrder.Add(nameof(CriticalPriority_Test));
        }
        await Task.Delay(100);
    }

    [Test, ExecutionPriority(Priority.High)]
    public async Task HighPriority_Test1()
    {
        lock (Lock)
        {
            ExecutionOrder.Add(nameof(HighPriority_Test1));
        }
        await Task.Delay(100);
    }

    [Test, ExecutionPriority(Priority.High)]
    public async Task HighPriority_Test2()
    {
        lock (Lock)
        {
            ExecutionOrder.Add(nameof(HighPriority_Test2));
        }
        await Task.Delay(100);
    }

    [Test, ExecutionPriority(Priority.Normal)]
    public async Task NormalPriority_Test()
    {
        lock (Lock)
        {
            ExecutionOrder.Add(nameof(NormalPriority_Test));
        }
        await Task.Delay(100);
    }

    [Test, ExecutionPriority(Priority.Low)]
    public async Task LowPriority_Test()
    {
        lock (Lock)
        {
            ExecutionOrder.Add(nameof(LowPriority_Test));
        }
        await Task.Delay(100);
    }

    [Test, ExecutionPriority(Priority.Low), NotInParallel(nameof(PriorityTests), Order = 99)]
    public async Task VerifyPriorityOrder()
    {
        await Task.Delay(500); // Give other tests time to complete
        
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