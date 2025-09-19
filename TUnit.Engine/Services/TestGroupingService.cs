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
        var orderedTests = tests
            .OrderByDescending(t => t.Context.ExecutionPriority)
            .ThenBy(x => x.Context.ClassContext?.ClassType?.FullName ?? string.Empty)
            .ThenBy(t => t.Context.ParallelConstraints.OfType<NotInParallelConstraint>().FirstOrDefault()?.Order ?? int.MaxValue);

        var notInParallelList = new List<(AbstractExecutableTest Test, TestPriority Priority)>();
        var keyedNotInParallelList = new List<(AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, TestPriority Priority)>();
        var parallelTests = new List<AbstractExecutableTest>();
        var parallelGroups = new Dictionary<string, SortedDictionary<int, List<AbstractExecutableTest>>>();
        var constrainedParallelGroups = new Dictionary<string, (List<AbstractExecutableTest> Unconstrained, List<(AbstractExecutableTest, IReadOnlyList<string>, TestPriority)> Keyed)>();

        // Process each class group sequentially to maintain class ordering for NotInParallel tests
        foreach (var test in orderedTests)
        {
            var constraints = test.Context.ParallelConstraints;
            
            // Handle tests with multiple constraints
            var parallelGroup = constraints.OfType<ParallelGroupConstraint>().FirstOrDefault();
            var notInParallel = constraints.OfType<NotInParallelConstraint>().FirstOrDefault();
            
            if (parallelGroup != null && notInParallel != null)
            {
                // Test has both ParallelGroup and NotInParallel constraints
                ProcessCombinedConstraints(test, parallelGroup, notInParallel, constrainedParallelGroups);
            }
            else if (parallelGroup != null)
            {
                // Only ParallelGroup constraint
                ProcessParallelGroupConstraint(test, parallelGroup, parallelGroups);
            }
            else if (notInParallel != null)
            {
                // Only NotInParallel constraint
                ProcessNotInParallelConstraint(test, notInParallel, notInParallelList, keyedNotInParallelList);
            }
            else
            {
                // No constraints - can run in parallel
                parallelTests.Add(test);
            }
        }

        // Sort NotInParallel tests by class first to maintain class grouping,
        // then by priority within each class
        var sortedNotInParallel = notInParallelList
            .OrderBy(t => t.Test.Context.ClassContext?.ClassType?.FullName ?? string.Empty)
            .ThenByDescending(t => t.Priority.Priority)
            .ThenBy(t => t.Priority.Order)
            .Select(t => t.Test)
            .ToArray();

        // Sort keyed tests similarly - class grouping first, then priority
        var keyedArrays = keyedNotInParallelList
            .OrderBy(t => t.Test.Context.ClassContext?.ClassType?.FullName ?? string.Empty)
            .ThenByDescending(t => t.Priority.Priority)
            .ThenBy(t => t.Priority.Order)
            .Select(t => (t.Test, t.ConstraintKeys, t.Priority.GetHashCode()))
            .ToArray();

        // Convert constrained parallel groups to the final format
        var finalConstrainedGroups = new Dictionary<string, GroupedConstrainedTests>();
        foreach (var kvp in constrainedParallelGroups)
        {
            var groupName = kvp.Key;
            var unconstrained = kvp.Value.Unconstrained;
            var keyed = kvp.Value.Keyed;
            
            var sortedKeyed = keyed
                .OrderBy(t => t.Item1.Context.ClassContext?.ClassType?.FullName ?? string.Empty)
                .ThenByDescending(t => t.Item3.Priority)
                .ThenBy(t => t.Item3.Order)
                .Select(t => (t.Item1, t.Item2, t.Item3.GetHashCode()))
                .ToArray();
                
            finalConstrainedGroups[groupName] = new GroupedConstrainedTests
            {
                UnconstrainedTests = unconstrained.ToArray(),
                KeyedTests = sortedKeyed
            };
        }
        
        var result = new GroupedTests
        {
            Parallel = parallelTests.ToArray(),
            NotInParallel = sortedNotInParallel,
            KeyedNotInParallel = keyedArrays,
            ParallelGroups = parallelGroups,
            ConstrainedParallelGroups = finalConstrainedGroups
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
            tests = [];
            orderGroups[constraint.Order] = tests;
        }

        tests.Add(test);
    }
    
    private static void ProcessCombinedConstraints(
        AbstractExecutableTest test,
        ParallelGroupConstraint parallelGroup,
        NotInParallelConstraint notInParallel,
        Dictionary<string, (List<AbstractExecutableTest> Unconstrained, List<(AbstractExecutableTest, IReadOnlyList<string>, TestPriority)> Keyed)> constrainedGroups)
    {
        if (!constrainedGroups.TryGetValue(parallelGroup.Group, out var group))
        {
            group = (new List<AbstractExecutableTest>(), new List<(AbstractExecutableTest, IReadOnlyList<string>, TestPriority)>());
            constrainedGroups[parallelGroup.Group] = group;
        }
        
        // Add to keyed tests within the parallel group
        var order = notInParallel.Order;
        var priority = test.Context.ExecutionPriority;
        var testPriority = new TestPriority(priority, order);
        
        if (notInParallel.NotInParallelConstraintKeys.Count > 0)
        {
            group.Keyed.Add((test, notInParallel.NotInParallelConstraintKeys, testPriority));
        }
        else
        {
            // NotInParallel without keys means sequential within the group
            group.Keyed.Add((test, new List<string> { "__global__" }, testPriority));
        }
    }
}
