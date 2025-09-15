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
        // Group tests by class first to ensure class-level NotInParallel constraints maintain class grouping
        var testsByClass = tests.GroupBy(x => x.Context.TestDetails.ClassType)
            .OrderBy(g => g.Max(t => t.Context.ExecutionPriority))
            .ThenBy(g => g.Min(t => (t.Context.ParallelConstraint as NotInParallelConstraint)?.Order ?? int.MaxValue))
            .ToList();

        var notInParallelList = new List<(AbstractExecutableTest Test, TestPriority Priority)>();
        var keyedNotInParallelList = new List<(AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, TestPriority Priority)>();
        var parallelTests = new List<AbstractExecutableTest>();
        var parallelGroups = new Dictionary<string, SortedDictionary<int, List<AbstractExecutableTest>>>();

        // Process each class group sequentially to maintain class ordering for NotInParallel tests
        foreach (var classGroup in testsByClass)
        {
            var classTests = classGroup.OrderBy(t => (t.Context.ParallelConstraint as NotInParallelConstraint)?.Order ?? int.MaxValue).ToList();

            foreach (var test in classTests)
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
        }

        // Group NotInParallel tests by class first, then sort within each class by priority
        var sortedNotInParallel = notInParallelList
            .GroupBy(t => t.Test.Context.TestDetails.ClassType)
            .OrderBy(g => g.Min(t => t.Priority))
            .SelectMany(g => g.OrderBy(t => t.Priority))
            .Select(t => t.Test)
            .ToArray();

        // Group keyed tests by class first, then sort within each class by priority
        var keyedArrays = keyedNotInParallelList
            .GroupBy(t => t.Test.Context.TestDetails.ClassType)
            .OrderBy(g => g.Min(t => t.Priority))
            .SelectMany(g => g.OrderBy(t => t.Priority))
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
