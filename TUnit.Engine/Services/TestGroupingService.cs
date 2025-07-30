using TUnit.Core;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

/// <summary>
/// Service responsible for grouping tests based on their parallel constraints
/// </summary>
internal interface ITestGroupingService
{
    ValueTask<GroupedTests> GroupTestsByConstraintsAsync(IEnumerable<AbstractExecutableTest> tests);
}

internal sealed class TestGroupingService : ITestGroupingService
{
    public ValueTask<GroupedTests> GroupTestsByConstraintsAsync(IEnumerable<AbstractExecutableTest> tests)
    {
        // Use collection directly if already materialized, otherwise create efficient list
        var allTests = tests as IReadOnlyList<AbstractExecutableTest> ?? tests.ToList();
        var notInParallelQueue = new PriorityQueue<AbstractExecutableTest, int>();
        var keyedNotInParallelQueues = new Dictionary<string, PriorityQueue<AbstractExecutableTest, int>>();
        var parallelTests = new List<AbstractExecutableTest>();
        var parallelGroups = new Dictionary<string, SortedDictionary<int, List<AbstractExecutableTest>>>();

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

        return new ValueTask<GroupedTests>(result);
    }

    private static void ProcessNotInParallelConstraint(
        AbstractExecutableTest test, 
        NotInParallelConstraint constraint,
        PriorityQueue<AbstractExecutableTest, int> notInParallelQueue,
        Dictionary<string, PriorityQueue<AbstractExecutableTest, int>> keyedQueues)
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
                    queue = new PriorityQueue<AbstractExecutableTest, int>();
                    keyedQueues[key] = queue;
                }
                queue.Enqueue(test, order);
            }
        }
    }

    private static void ProcessParallelGroupConstraint(
        AbstractExecutableTest test,
        ParallelGroupConstraint constraint,
        Dictionary<string, SortedDictionary<int, List<AbstractExecutableTest>>> parallelGroups)
    {
        if (!parallelGroups.TryGetValue(constraint.Group, out var orderGroups))
        {
            orderGroups = new SortedDictionary<int, List<AbstractExecutableTest>>();
            parallelGroups[constraint.Group] = orderGroups;
        }

        if (!orderGroups.TryGetValue(constraint.Order, out var tests))
        {
            tests =
            [
            ];
            orderGroups[constraint.Order] = tests;
        }

        tests.Add(test);
    }
}