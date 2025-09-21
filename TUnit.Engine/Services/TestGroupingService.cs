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
    private struct TestSortKey
    {
        public int ExecutionPriority { get; init; }
        public string? ClassFullName { get; init; }
        public int NotInParallelOrder { get; init; }
        public NotInParallelConstraint? NotInParallelConstraint { get; init; }
    }
    
    public ValueTask<GroupedTests> GroupTestsByConstraintsAsync(IEnumerable<AbstractExecutableTest> tests)
    {
        var testsWithKeys = new List<(AbstractExecutableTest Test, TestSortKey Key)>();
        foreach (var test in tests)
        {
            NotInParallelConstraint? notInParallelConstraint = null;
            foreach (var constraint in test.Context.ParallelConstraints)
            {
                if (constraint is NotInParallelConstraint nip)
                {
                    notInParallelConstraint = nip;
                    break;
                }
            }
            
            var key = new TestSortKey
            {
                ExecutionPriority = (int)test.Context.ExecutionPriority,
                ClassFullName = test.Context.ClassContext?.ClassType?.FullName,
                NotInParallelOrder = notInParallelConstraint?.Order ?? int.MaxValue,
                NotInParallelConstraint = notInParallelConstraint
            };
            testsWithKeys.Add((test, key));
        }
        
        testsWithKeys.Sort((a, b) =>
        {
            var priorityCompare = b.Key.ExecutionPriority.CompareTo(a.Key.ExecutionPriority);
            if (priorityCompare != 0) return priorityCompare;
            
            var classCompare = string.CompareOrdinal(a.Key.ClassFullName ?? string.Empty, b.Key.ClassFullName ?? string.Empty);
            if (classCompare != 0) return classCompare;
            
            return a.Key.NotInParallelOrder.CompareTo(b.Key.NotInParallelOrder);
        });

        var notInParallelList = new List<(AbstractExecutableTest Test, TestPriority Priority)>();
        var keyedNotInParallelList = new List<(AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, TestPriority Priority)>();
        var parallelTests = new List<AbstractExecutableTest>();
        var parallelGroups = new Dictionary<string, SortedDictionary<int, List<AbstractExecutableTest>>>();
        var constrainedParallelGroups = new Dictionary<string, (List<AbstractExecutableTest> Unconstrained, List<(AbstractExecutableTest, IReadOnlyList<string>, TestPriority)> Keyed)>();

        foreach (var (test, sortKey) in testsWithKeys)
        {
            var constraints = test.Context.ParallelConstraints;
            ParallelGroupConstraint? parallelGroup = null;
            foreach (var constraint in constraints)
            {
                if (constraint is ParallelGroupConstraint pg)
                {
                    parallelGroup = pg;
                    break;
                }
            }
            var notInParallel = sortKey.NotInParallelConstraint;
            
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

        notInParallelList.Sort((a, b) =>
        {
            var classA = a.Test.Context.ClassContext?.ClassType?.FullName ?? string.Empty;
            var classB = b.Test.Context.ClassContext?.ClassType?.FullName ?? string.Empty;
            var classCompare = string.CompareOrdinal(classA, classB);
            if (classCompare != 0) return classCompare;
            
            var priorityCompare = b.Priority.Priority.CompareTo(a.Priority.Priority);
            if (priorityCompare != 0) return priorityCompare;
            
            return a.Priority.Order.CompareTo(b.Priority.Order);
        });
        
        var sortedNotInParallel = new AbstractExecutableTest[notInParallelList.Count];
        for (int i = 0; i < notInParallelList.Count; i++)
        {
            sortedNotInParallel[i] = notInParallelList[i].Test;
        }

        keyedNotInParallelList.Sort((a, b) =>
        {
            var classA = a.Test.Context.ClassContext?.ClassType?.FullName ?? string.Empty;
            var classB = b.Test.Context.ClassContext?.ClassType?.FullName ?? string.Empty;
            var classCompare = string.CompareOrdinal(classA, classB);
            if (classCompare != 0) return classCompare;
            
            var priorityCompare = b.Priority.Priority.CompareTo(a.Priority.Priority);
            if (priorityCompare != 0) return priorityCompare;
            
            return a.Priority.Order.CompareTo(b.Priority.Order);
        });
        
        var keyedArrays = new (AbstractExecutableTest, IReadOnlyList<string>, int)[keyedNotInParallelList.Count];
        for (int i = 0; i < keyedNotInParallelList.Count; i++)
        {
            var item = keyedNotInParallelList[i];
            keyedArrays[i] = (item.Test, item.ConstraintKeys, item.Priority.GetHashCode());
        }

        // Convert constrained parallel groups to the final format
        var finalConstrainedGroups = new Dictionary<string, GroupedConstrainedTests>();
        foreach (var kvp in constrainedParallelGroups)
        {
            var groupName = kvp.Key;
            var unconstrained = kvp.Value.Unconstrained;
            var keyed = kvp.Value.Keyed;
            
            keyed.Sort((a, b) =>
            {
                var classA = a.Item1.Context.ClassContext?.ClassType?.FullName ?? string.Empty;
                var classB = b.Item1.Context.ClassContext?.ClassType?.FullName ?? string.Empty;
                var classCompare = string.CompareOrdinal(classA, classB);
                if (classCompare != 0) return classCompare;
                
                var priorityCompare = b.Item3.Priority.CompareTo(a.Item3.Priority);
                if (priorityCompare != 0) return priorityCompare;
                
                return a.Item3.Order.CompareTo(b.Item3.Order);
            });
            
            var sortedKeyed = new (AbstractExecutableTest, IReadOnlyList<string>, int)[keyed.Count];
            for (int i = 0; i < keyed.Count; i++)
            {
                var item = keyed[i];
                sortedKeyed[i] = (item.Item1, item.Item2, item.Item3.GetHashCode());
            }
                
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
