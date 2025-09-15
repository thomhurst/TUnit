using TUnit.Core;
using TUnit.Engine.Models;
using TUnit.Engine.Scheduling;

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
        var notInParallelList = new List<(AbstractExecutableTest Test, TestPriority Priority)>();
        var keyedNotInParallelList = new List<(AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, TestPriority Priority)>();
        var parallelTests = new List<AbstractExecutableTest>();
        var parallelGroups = new Dictionary<string, SortedDictionary<int, List<AbstractExecutableTest>>>();

        foreach (var test in allTests)
        {
            var constraint = test.Context.ParallelConstraint;
            
            switch (constraint)
            {
                case NotInParallelConstraint notInParallel:
                    ProcessNotInParallelConstraint(test, notInParallel, notInParallelList, keyedNotInParallelList);
                    break;
                    
                case ParallelGroupConstraint parallelGroup:
                    ProcessParallelGroupConstraint(test, parallelGroup, parallelGroups);
                    break;
                    
                default:
                    parallelTests.Add(test);
                    break;
            }
        }

        // Sort the NotInParallel tests by priority and extract just the tests
        notInParallelList.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        var sortedNotInParallel = notInParallelList.Select(t => t.Test).ToArray();
        
        // Sort keyed tests by priority and convert to array
        keyedNotInParallelList.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        var keyedArrays = keyedNotInParallelList
            .Select(t => (t.Test, t.ConstraintKeys, t.Priority.GetHashCode()))
            .ToArray();
        
        // Convert parallel groups to array of tuples
        var parallelGroupArrays = parallelGroups.Select(kvp =>
        {
            var orderedTests = kvp.Value.Select(orderKvp => 
                (orderKvp.Key, orderKvp.Value.ToArray())
            ).ToArray();
            return (kvp.Key, orderedTests);
        }).ToArray();

        var result = new GroupedTests
        {
            Parallel = parallelTests.ToArray(),
            NotInParallel = sortedNotInParallel,
            KeyedNotInParallel = keyedArrays,
            ParallelGroups = parallelGroupArrays
        };

        return new ValueTask<GroupedTests>(result);
    }

    private static void ProcessNotInParallelConstraint(
        AbstractExecutableTest test, 
        NotInParallelConstraint constraint,
        List<(AbstractExecutableTest Test, TestPriority Priority)> notInParallelList,
        List<(AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, TestPriority Priority)> keyedNotInParallelList)
    {
        var order = constraint.Order;
        var priority = test.Context.ExecutionPriority;
        var testPriority = new TestPriority(priority, order);
        
        
        if (constraint.NotInParallelConstraintKeys.Count == 0)
        {
            notInParallelList.Add((test, testPriority));
        }
        else
        {
            // Add test only once with all its constraint keys
            keyedNotInParallelList.Add((test, constraint.NotInParallelConstraintKeys, testPriority));
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