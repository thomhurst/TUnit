using TUnit.Core;
using TUnit.Engine.Services;
using TUnit.Engine.Scheduling;

namespace TUnit.Engine.Tests.NotInParallelOrderTests;

[NotInParallel(Order = 3)]
public sealed class OrderTestA
{
    [Test]
    public Task TestA()
    {
        TestExecutionOrder.RecordExecution(nameof(OrderTestA));
        return Task.CompletedTask;
    }
}

[NotInParallel(Order = 1)]
public sealed class OrderTestB
{
    [Test]
    public Task TestB()
    {
        TestExecutionOrder.RecordExecution(nameof(OrderTestB));
        return Task.CompletedTask;
    }
}

[NotInParallel(Order = 2)]
public sealed class OrderTestC
{
    [Test]
    public Task TestC()
    {
        TestExecutionOrder.RecordExecution(nameof(OrderTestC));
        return Task.CompletedTask;
    }
}

public static class TestExecutionOrder
{
    private static readonly List<string> _executionOrder = new();

    public static void RecordExecution(string testName)
    {
        lock (_executionOrder)
        {
            _executionOrder.Add(testName);
        }
    }

    public static List<string> GetExecutionOrder()
    {
        lock (_executionOrder)
        {
            return new List<string>(_executionOrder);
        }
    }

    public static void Reset()
    {
        lock (_executionOrder)
        {
            _executionOrder.Clear();
        }
    }
}