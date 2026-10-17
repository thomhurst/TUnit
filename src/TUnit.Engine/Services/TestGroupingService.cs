#if NET
using System.Runtime.InteropServices;
#endif
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
        public int? HookRank { get; init; } // null unless an OrderTestsBy hook ranked the test
        public string ClassFullName { get; init; } // Cached to avoid repeated property access
        public int NotInParallelOrder { get; init; }
        public NotInParallelConstraint? NotInParallelConstraint { get; init; }
        public int Index { get; init; } // Input position: the final tie-break, since List<T>.Sort is unstable
    }

    public ValueTask<GroupedTests> GroupTestsByConstraintsAsync(IEnumerable<AbstractExecutableTest> tests)
    {
        // Hot path: trace disabled → fully synchronous, no state machine, no allocation
        // for a trace-message list. Only pay async cost when someone actually wants traces.
        if (!_logger.IsTraceEnabled)
        {
            return new ValueTask<GroupedTests>(GroupTestsByConstraintsCore(tests, traceMessages: null));
        }

        return GroupTestsByConstraintsWithTracingAsync(tests);
    }

    private async ValueTask<GroupedTests> GroupTestsByConstraintsWithTracingAsync(IEnumerable<AbstractExecutableTest> tests)
    {
        var traceMessages = new List<string>();
        var result = GroupTestsByConstraintsCore(tests, traceMessages);
        foreach (var msg in traceMessages)
        {
            await _logger.LogTraceAsync(msg).ConfigureAwait(false);
        }
        return result;
    }

    private static GroupedTests GroupTestsByConstraintsCore(IEnumerable<AbstractExecutableTest> tests, List<string>? traceMessages)
    {
        var testCount = tests is ICollection<AbstractExecutableTest> collection ? collection.Count : 0;
        var testsWithKeys = new List<(AbstractExecutableTest Test, TestSortKey Key)>(testCount > 0 ? testCount : 16);
        var hasParallelConstraints = false;
        var hasHookRanks = false;
        foreach (var test in tests)
        {
            NotInParallelConstraint? notInParallelConstraint = null;
            foreach (var constraint in test.Context.ParallelConstraints)
            {
                hasParallelConstraints = true;
                if (constraint is NotInParallelConstraint nip)
                {
                    notInParallelConstraint = nip;
                    break;
                }
            }

            var hookRank = test.Context.HookRank;
            hasHookRanks |= hookRank.HasValue;

            var key = new TestSortKey
            {
                ExecutionPriority = (int)test.Context.ExecutionPriority,
                HookRank = hookRank,
                ClassFullName = test.Context.ClassContext?.ClassType?.FullName ?? string.Empty,
                NotInParallelOrder = notInParallelConstraint?.Order ?? int.MaxValue,
                NotInParallelConstraint = notInParallelConstraint,
                Index = testsWithKeys.Count
            };
            testsWithKeys.Add((test, key));
        }

        testsWithKeys.Sort((a, b) =>
        {
            var priorityCompare = b.Key.ExecutionPriority.CompareTo(a.Key.ExecutionPriority);
            if (priorityCompare != 0) return priorityCompare;

            // Ranks are null unless an OrderTestsBy hook ran, so this only reorders hook-ranked runs.
            var rankCompare = Nullable.Compare(a.Key.HookRank, b.Key.HookRank);
            if (rankCompare != 0) return rankCompare;

            var classCompare = string.CompareOrdinal(a.Key.ClassFullName, b.Key.ClassFullName);
            if (classCompare != 0) return classCompare;

            var orderCompare = a.Key.NotInParallelOrder.CompareTo(b.Key.NotInParallelOrder);
            if (orderCompare != 0) return orderCompare;

            return a.Key.Index.CompareTo(b.Key.Index);
        });

        if (!hasParallelConstraints && traceMessages is null)
        {
            var orderedTests = new AbstractExecutableTest[testsWithKeys.Count];
            for (var i = 0; i < testsWithKeys.Count; i++)
            {
                orderedTests[i] = testsWithKeys[i].Test;
            }

            return new GroupedTests
            {
                Parallel = orderedTests,
                NotInParallel = [],
                KeyedNotInParallel = [],
                ParallelGroups = [],
                ConstrainedParallelGroups = []
            };
        }

        var estimatedCount = testsWithKeys.Count;
        var notInParallelList = new List<(AbstractExecutableTest Test, string ClassName, TestPriority Priority, int Index)>(estimatedCount / 4);
        var keyedNotInParallelList = new List<(AbstractExecutableTest Test, string ClassName, IReadOnlyList<string> ConstraintKeys, TestPriority Priority, int Index)>(estimatedCount / 8);
        var parallelTests = new List<AbstractExecutableTest>(estimatedCount / 2);
        var parallelGroups = new Dictionary<string, SortedDictionary<int, List<AbstractExecutableTest>>>(capacity: 8);
        var constrainedParallelGroups = new Dictionary<string, (List<AbstractExecutableTest> Unconstrained, List<(AbstractExecutableTest, string, IReadOnlyList<string>, TestPriority, int)> Keyed)>(capacity: 8);

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
                traceMessages?.Add($"Test '{test.TestId}': → ConstrainedParallelGroup '{parallelGroup.Group}' + NotInParallel{FormatParallelLimiterInfo(test)}");
                ProcessCombinedConstraints(test, sortKey.ClassFullName, sortKey.Index, parallelGroup, notInParallel, constrainedParallelGroups);
            }
            else if (parallelGroup != null)
            {
                traceMessages?.Add($"Test '{test.TestId}': → ParallelGroup '{parallelGroup.Group}'{FormatParallelLimiterInfo(test)}");
                ProcessParallelGroupConstraint(test, parallelGroup, parallelGroups);
            }
            else if (notInParallel != null)
            {
                if (traceMessages != null)
                {
                    var keys = notInParallel.NotInParallelConstraintKeys.Count > 0 ? $" (keys: {string.Join(", ", notInParallel.NotInParallelConstraintKeys)})" : "";
                    traceMessages.Add($"Test '{test.TestId}': → NotInParallel{keys}{FormatParallelLimiterInfo(test)}");
                }
                ProcessNotInParallelConstraint(test, sortKey.ClassFullName, sortKey.Index, notInParallel, notInParallelList, keyedNotInParallelList);
            }
            else
            {
                traceMessages?.Add($"Test '{test.TestId}': → Parallel (no constraints){FormatParallelLimiterInfo(test)}");
                parallelTests.Add(test);
            }
        }

        // Sequential buckets group by class before priority; a hook rank (null unless an OrderTestsBy
        // hook ran) takes precedence over that grouping, and ties keep the existing keys. Full ties fall
        // back to the input position so the order is deterministic (List<T>.Sort is unstable).
        notInParallelList.Sort((a, b) =>
        {
            var rankCompare = CompareHookRank(a.Test, b.Test);
            if (rankCompare != 0) return rankCompare;

            var classCompare = string.CompareOrdinal(a.ClassName, b.ClassName);
            if (classCompare != 0) return classCompare;

            var priorityCompare = b.Priority.Priority.CompareTo(a.Priority.Priority);
            if (priorityCompare != 0) return priorityCompare;

            var orderCompare = a.Priority.Order.CompareTo(b.Priority.Order);
            if (orderCompare != 0) return orderCompare;

            return a.Index.CompareTo(b.Index);
        });

        var sortedNotInParallel = new AbstractExecutableTest[notInParallelList.Count];
        for (int i = 0; i < notInParallelList.Count; i++)
        {
            sortedNotInParallel[i] = notInParallelList[i].Test;
        }

        // Keyed tests with different keys can overlap, so [ExecutionPriority] stays ahead of the hook rank,
        // as in the parallel sort. Within a priority the rank decides, then the class and Order as above.
        keyedNotInParallelList.Sort((a, b) =>
        {
            var priorityCompare = b.Priority.Priority.CompareTo(a.Priority.Priority);
            if (priorityCompare != 0) return priorityCompare;

            var rankCompare = CompareHookRank(a.Test, b.Test);
            if (rankCompare != 0) return rankCompare;

            var classCompare = string.CompareOrdinal(a.ClassName, b.ClassName);
            if (classCompare != 0) return classCompare;

            var orderCompare = a.Priority.Order.CompareTo(b.Priority.Order);
            if (orderCompare != 0) return orderCompare;

            return a.Index.CompareTo(b.Index);
        });

        var keyedArrays = new (AbstractExecutableTest, IReadOnlyList<string>, int)[keyedNotInParallelList.Count];
        for (int i = 0; i < keyedNotInParallelList.Count; i++)
        {
            var item = keyedNotInParallelList[i];
            keyedArrays[i] = (item.Test, item.ConstraintKeys, GetSchedulerPriority(hasHookRanks, item.Priority, i));
        }

        // Convert constrained parallel groups to the final format
        var finalConstrainedGroups = new Dictionary<string, GroupedConstrainedTests>(capacity: constrainedParallelGroups.Count);
        foreach (var kvp in constrainedParallelGroups)
        {
            var groupName = kvp.Key;
            var unconstrained = kvp.Value.Unconstrained;
            var keyed = kvp.Value.Keyed;

            // Same precedence as the keyed sort above: priority, then hook rank, then class and Order.
            keyed.Sort((a, b) =>
            {
                var priorityCompare = b.Item4.Priority.CompareTo(a.Item4.Priority);
                if (priorityCompare != 0) return priorityCompare;

                var rankCompare = CompareHookRank(a.Item1, b.Item1);
                if (rankCompare != 0) return rankCompare;

                var classCompare = string.CompareOrdinal(a.Item2, b.Item2);
                if (classCompare != 0) return classCompare;

                var orderCompare = a.Item4.Order.CompareTo(b.Item4.Order);
                if (orderCompare != 0) return orderCompare;

                return a.Item5.CompareTo(b.Item5);
            });

            var sortedKeyed = new (AbstractExecutableTest, IReadOnlyList<string>, int)[keyed.Count];
            for (int i = 0; i < keyed.Count; i++)
            {
                var item = keyed[i];
                sortedKeyed[i] = (item.Item1, item.Item3, GetSchedulerPriority(hasHookRanks, item.Item4, i));
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

        if (traceMessages != null)
        {
            traceMessages.Add("═══ Test Grouping Summary ═══");
            traceMessages.Add($"  Parallel (no constraints): {parallelTests.Count} tests");
            traceMessages.Add($"  ParallelGroups: {parallelGroups.Count} groups");
            traceMessages.Add($"  ConstrainedParallelGroups: {finalConstrainedGroups.Count} groups");
            traceMessages.Add($"  NotInParallel (global): {sortedNotInParallel.Length} tests");
            traceMessages.Add($"  KeyedNotInParallel: {keyedArrays.Length} tests");
            traceMessages.Add("════════════════════════════");
        }

        return result;
    }

    // Hook rank comparison shared by the sequential buckets. Ranks are null unless an OrderTestsBy hook
    // ran; unranked tests sort first, so runs without hooks keep their existing order.
    private static int CompareHookRank(AbstractExecutableTest a, AbstractExecutableTest b)
        => Nullable.Compare(a.Context.HookRank, b.Context.HookRank);

    // Contract with ConstraintKeyScheduler: it stably re-sorts keyed tests by this value (ascending, ties
    // by array position) and orders waiting candidates by it. When hooks ranked the run the list is already
    // in its final order, so passing each test's position keeps that order intact.
    private static int GetSchedulerPriority(bool hasHookRanks, TestPriority priority, int sortedIndex)
        => hasHookRanks ? sortedIndex : priority.GetHashCode();

    private static void ProcessNotInParallelConstraint(
        AbstractExecutableTest test,
        string className,
        int index,
        NotInParallelConstraint constraint,
        List<(AbstractExecutableTest Test, string ClassName, TestPriority Priority, int Index)> notInParallelList,
        List<(AbstractExecutableTest Test, string ClassName, IReadOnlyList<string> ConstraintKeys, TestPriority Priority, int Index)> keyedNotInParallelList)
    {
        var order = constraint.Order;
        var priority = test.Context.ExecutionPriority;
        var testPriority = new TestPriority(priority, order);

        if (constraint.NotInParallelConstraintKeys.Count == 0)
        {
            notInParallelList.Add((test, className, testPriority, index));
        }
        else
        {
            keyedNotInParallelList.Add((test, className, constraint.NotInParallelConstraintKeys, testPriority, index));
        }
    }

    private static void ProcessParallelGroupConstraint(
        AbstractExecutableTest test,
        ParallelGroupConstraint constraint,
        Dictionary<string, SortedDictionary<int, List<AbstractExecutableTest>>> parallelGroups)
    {
#if NET
        ref var orderGroups = ref CollectionsMarshal.GetValueRefOrAddDefault(parallelGroups, constraint.Group, out _);
        orderGroups ??= new SortedDictionary<int, List<AbstractExecutableTest>>();
#else
        if (!parallelGroups.TryGetValue(constraint.Group, out var orderGroups))
        {
            orderGroups = new SortedDictionary<int, List<AbstractExecutableTest>>();
            parallelGroups[constraint.Group] = orderGroups;
        }
#endif

        // SortedDictionary is not supported by CollectionsMarshal, so keep the two-lookup pattern here.
        if (!orderGroups.TryGetValue(constraint.Order, out var tests))
        {
            tests = [];
            orderGroups[constraint.Order] = tests;
        }

        tests.Add(test);
    }

    private static string FormatParallelLimiterInfo(AbstractExecutableTest test) =>
        test.Context.ParallelLimiter != null
            ? $" [ParallelLimiter: {test.Context.ParallelLimiter.GetType().Name} (limit: {test.Context.ParallelLimiter.Limit})]"
            : "";

    private static void ProcessCombinedConstraints(
        AbstractExecutableTest test,
        string className,
        int index,
        ParallelGroupConstraint parallelGroup,
        NotInParallelConstraint notInParallel,
        Dictionary<string, (List<AbstractExecutableTest> Unconstrained, List<(AbstractExecutableTest, string, IReadOnlyList<string>, TestPriority, int)> Keyed)> constrainedGroups)
    {
#if NET
        ref var group = ref CollectionsMarshal.GetValueRefOrAddDefault(constrainedGroups, parallelGroup.Group, out _);
        group.Unconstrained ??= [];
        group.Keyed ??= [];
#else
        if (!constrainedGroups.TryGetValue(parallelGroup.Group, out var group))
        {
            group = ([], []);
            constrainedGroups[parallelGroup.Group] = group;
        }
#endif

        // Add to keyed tests within the parallel group
        var order = notInParallel.Order;
        var priority = test.Context.ExecutionPriority;
        var testPriority = new TestPriority(priority, order);

        if (notInParallel.NotInParallelConstraintKeys.Count > 0)
        {
            group.Keyed.Add((test, className, notInParallel.NotInParallelConstraintKeys, testPriority, index));
        }
        else
        {
            // NotInParallel without keys means sequential within the group
            group.Keyed.Add((test, className, new List<string> { "__global__" }, testPriority, index));
        }
    }
}
