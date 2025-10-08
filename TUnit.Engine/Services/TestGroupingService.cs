using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.Logging;
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
    private readonly TUnitFrameworkLogger _logger;

    public TestGroupingService(TUnitFrameworkLogger logger)
    {
        _logger = logger;
    }

    private struct TestSortKey
    {
        public int ExecutionPriority { get; init; }
        public string ClassFullName { get; init; } // Cached to avoid repeated property access
        public int NotInParallelOrder { get; init; }
        public NotInParallelConstraint? NotInParallelConstraint { get; init; }
    }
    
    public async ValueTask<GroupedTests> GroupTestsByConstraintsAsync(IEnumerable<AbstractExecutableTest> tests)
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
                ClassFullName = test.Context.ClassContext?.ClassType?.FullName ?? string.Empty,
                NotInParallelOrder = notInParallelConstraint?.Order ?? int.MaxValue,
                NotInParallelConstraint = notInParallelConstraint
            };
            testsWithKeys.Add((test, key));
        }
        
        testsWithKeys.Sort((a, b) =>
        {
            var priorityCompare = b.Key.ExecutionPriority.CompareTo(a.Key.ExecutionPriority);
            if (priorityCompare != 0) return priorityCompare;

            var classCompare = string.CompareOrdinal(a.Key.ClassFullName, b.Key.ClassFullName);
            if (classCompare != 0) return classCompare;

            return a.Key.NotInParallelOrder.CompareTo(b.Key.NotInParallelOrder);
        });

        var notInParallelList = new List<(AbstractExecutableTest Test, string ClassName, TestPriority Priority)>();
        var keyedNotInParallelList = new List<(AbstractExecutableTest Test, string ClassName, IReadOnlyList<string> ConstraintKeys, TestPriority Priority)>();
        var parallelTests = new List<AbstractExecutableTest>();
        var parallelGroups = new Dictionary<string, SortedDictionary<int, List<AbstractExecutableTest>>>(capacity: 16);
        var constrainedParallelGroups = new Dictionary<string, (List<AbstractExecutableTest> Unconstrained, List<(AbstractExecutableTest, string, IReadOnlyList<string>, TestPriority)> Keyed)>(capacity: 16);

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

            // Log parallel limiter if present
            var parallelLimiterInfo = test.Context.ParallelLimiter != null
                ? $" [ParallelLimiter: {test.Context.ParallelLimiter.GetType().Name} (limit: {test.Context.ParallelLimiter.Limit})]"
                : "";

            if (parallelGroup != null && notInParallel != null)
            {
                // Test has both ParallelGroup and NotInParallel constraints
                await _logger.LogDebugAsync($"Test '{test.TestId}': → ConstrainedParallelGroup '{parallelGroup.Group}' + NotInParallel{parallelLimiterInfo}").ConfigureAwait(false);
                ProcessCombinedConstraints(test, sortKey.ClassFullName, parallelGroup, notInParallel, constrainedParallelGroups);
            }
            else if (parallelGroup != null)
            {
                // Only ParallelGroup constraint
                await _logger.LogDebugAsync($"Test '{test.TestId}': → ParallelGroup '{parallelGroup.Group}'{parallelLimiterInfo}").ConfigureAwait(false);
                ProcessParallelGroupConstraint(test, parallelGroup, parallelGroups);
            }
            else if (notInParallel != null)
            {
                // Only NotInParallel constraint
                var keys = notInParallel.NotInParallelConstraintKeys.Count > 0 ? $" (keys: {string.Join(", ", notInParallel.NotInParallelConstraintKeys)})" : "";
                await _logger.LogDebugAsync($"Test '{test.TestId}': → NotInParallel{keys}{parallelLimiterInfo}").ConfigureAwait(false);
                ProcessNotInParallelConstraint(test, sortKey.ClassFullName, notInParallel, notInParallelList, keyedNotInParallelList);
            }
            else
            {
                // No constraints - can run in parallel
                await _logger.LogDebugAsync($"Test '{test.TestId}': → Parallel (no constraints){parallelLimiterInfo}").ConfigureAwait(false);
                parallelTests.Add(test);
            }
        }

        notInParallelList.Sort((a, b) =>
        {
            var classCompare = string.CompareOrdinal(a.ClassName, b.ClassName);
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
            var classCompare = string.CompareOrdinal(a.ClassName, b.ClassName);
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
        var finalConstrainedGroups = new Dictionary<string, GroupedConstrainedTests>(capacity: constrainedParallelGroups.Count);
        foreach (var kvp in constrainedParallelGroups)
        {
            var groupName = kvp.Key;
            var unconstrained = kvp.Value.Unconstrained;
            var keyed = kvp.Value.Keyed;
            
            keyed.Sort((a, b) =>
            {
                var classCompare = string.CompareOrdinal(a.Item2, b.Item2);
                if (classCompare != 0) return classCompare;

                var priorityCompare = b.Item4.Priority.CompareTo(a.Item4.Priority);
                if (priorityCompare != 0) return priorityCompare;

                return a.Item4.Order.CompareTo(b.Item4.Order);
            });

            var sortedKeyed = new (AbstractExecutableTest, IReadOnlyList<string>, int)[keyed.Count];
            for (int i = 0; i < keyed.Count; i++)
            {
                var item = keyed[i];
                sortedKeyed[i] = (item.Item1, item.Item3, item.Item4.GetHashCode());
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

        // Log summary of test categorization
        await _logger.LogDebugAsync("═══ Test Grouping Summary ═══").ConfigureAwait(false);
        await _logger.LogDebugAsync($"  Parallel (no constraints): {parallelTests.Count} tests").ConfigureAwait(false);
        await _logger.LogDebugAsync($"  ParallelGroups: {parallelGroups.Count} groups").ConfigureAwait(false);
        await _logger.LogDebugAsync($"  ConstrainedParallelGroups: {finalConstrainedGroups.Count} groups").ConfigureAwait(false);
        await _logger.LogDebugAsync($"  NotInParallel (global): {sortedNotInParallel.Length} tests").ConfigureAwait(false);
        await _logger.LogDebugAsync($"  KeyedNotInParallel: {keyedArrays.Length} tests").ConfigureAwait(false);
        await _logger.LogDebugAsync("════════════════════════════").ConfigureAwait(false);

        return result;
    }

    private static void ProcessNotInParallelConstraint(
        AbstractExecutableTest test,
        string className,
        NotInParallelConstraint constraint,
        List<(AbstractExecutableTest Test, string ClassName, TestPriority Priority)> notInParallelList,
        List<(AbstractExecutableTest Test, string ClassName, IReadOnlyList<string> ConstraintKeys, TestPriority Priority)> keyedNotInParallelList)
    {
        var order = constraint.Order;
        var priority = test.Context.ExecutionPriority;
        var testPriority = new TestPriority(priority, order);

        if (constraint.NotInParallelConstraintKeys.Count == 0)
        {
            notInParallelList.Add((test, className, testPriority));
        }
        else
        {
            // Add test only once with all its constraint keys
            keyedNotInParallelList.Add((test, className, constraint.NotInParallelConstraintKeys, testPriority));
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
        string className,
        ParallelGroupConstraint parallelGroup,
        NotInParallelConstraint notInParallel,
        Dictionary<string, (List<AbstractExecutableTest> Unconstrained, List<(AbstractExecutableTest, string, IReadOnlyList<string>, TestPriority)> Keyed)> constrainedGroups)
    {
        if (!constrainedGroups.TryGetValue(parallelGroup.Group, out var group))
        {
            group = (new List<AbstractExecutableTest>(), new List<(AbstractExecutableTest, string, IReadOnlyList<string>, TestPriority)>());
            constrainedGroups[parallelGroup.Group] = group;
        }

        // Add to keyed tests within the parallel group
        var order = notInParallel.Order;
        var priority = test.Context.ExecutionPriority;
        var testPriority = new TestPriority(priority, order);

        if (notInParallel.NotInParallelConstraintKeys.Count > 0)
        {
            group.Keyed.Add((test, className, notInParallel.NotInParallelConstraintKeys, testPriority));
        }
        else
        {
            // NotInParallel without keys means sequential within the group
            group.Keyed.Add((test, className, new List<string> { "__global__" }, testPriority));
        }
    }
}
