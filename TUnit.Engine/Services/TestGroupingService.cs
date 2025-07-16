using TUnit.Core;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

/// <summary>
/// Service responsible for grouping tests based on their parallel constraints
/// </summary>
internal interface ITestGroupingService
{
    Task<GroupedTests> GroupTestsByConstraintsAsync(IEnumerable<ExecutableTest> tests);
}

internal sealed class TestGroupingService : ITestGroupingService
{
    public Task<GroupedTests> GroupTestsByConstraintsAsync(IEnumerable<ExecutableTest> tests)
    {
        // Use collection directly if already materialized, otherwise create efficient list
        var allTests = tests as IReadOnlyList<ExecutableTest> ?? tests.ToList();
        var notInParallelQueue = new PriorityQueue<ExecutableTest, int>();
        var keyedNotInParallelQueues = new Dictionary<string, PriorityQueue<ExecutableTest, int>>();
        var parallelTests = new List<ExecutableTest>();
        var parallelGroups = new Dictionary<string, SortedDictionary<int, List<ExecutableTest>>>();

        foreach (var test in allTests)
        {
            var constraint = test.Context.ParallelConstraint;
            
            switch (constraint)
            {
                case NotInParallelConstraint notInParallel:
                    ProcessNotInParallelConstraint(test, notInParallel, notInParallelQueue, keyedNotInParallelQueues);
                    break;
                    
                case ParallelGroupConstraint parallelGroup:
                    ProcessParallelGroupConstraint(test, parallelGroup, parallelGroups);
                    break;
                    
                default:
                    parallelTests.Add(test);
                    break;
            }
        }

        var result = new GroupedTests
        {
            Parallel = parallelTests,
            NotInParallel = notInParallelQueue,
            KeyedNotInParallel = keyedNotInParallelQueues,
            ParallelGroups = parallelGroups
        };

        return Task.FromResult(result);
    }

    private static void ProcessNotInParallelConstraint(
        ExecutableTest test, 
        NotInParallelConstraint constraint,
        PriorityQueue<ExecutableTest, int> notInParallelQueue,
        Dictionary<string, PriorityQueue<ExecutableTest, int>> keyedQueues)
    {
        var order = constraint.Order;
        
        if (constraint.NotInParallelConstraintKeys.Count == 0)
        {
            notInParallelQueue.Enqueue(test, order);
        }
        else
        {
            foreach (var key in constraint.NotInParallelConstraintKeys)
            {
                if (!keyedQueues.TryGetValue(key, out var queue))
                {
                    queue = new PriorityQueue<ExecutableTest, int>();
                    keyedQueues[key] = queue;
                }
                queue.Enqueue(test, order);
            }
        }
    }

    private static void ProcessParallelGroupConstraint(
        ExecutableTest test,
        ParallelGroupConstraint constraint,
        Dictionary<string, SortedDictionary<int, List<ExecutableTest>>> parallelGroups)
    {
        if (!parallelGroups.TryGetValue(constraint.Group, out var orderGroups))
        {
            orderGroups = new SortedDictionary<int, List<ExecutableTest>>();
            parallelGroups[constraint.Group] = orderGroups;
        }

        if (!orderGroups.TryGetValue(constraint.Order, out var tests))
        {
            tests = new List<ExecutableTest>();
            orderGroups[constraint.Order] = tests;
        }

        tests.Add(test);
    }
}