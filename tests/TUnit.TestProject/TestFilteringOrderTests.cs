using System.Collections.Concurrent;
using TUnit.Core.Enums;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

internal static class TestFilteringOrderSelection
{
    // The ordering assertions expect every test in the class to run, so the hooks only order a run that holds
    // exactly that class. A single-test filter, or a whole-project run, leaves the default order untouched.
    public static bool IsWholeClass<TClass>(TestFilteringContext context, int testCount)
        => context.AllTests.Count == testCount
           && context.AllTests.All(t => t.Metadata.TestDetails.ClassType == typeof(TClass));
}

public static class TestFilteringOrderHooks
{
    public static bool OrderApplied;
    public static readonly ConcurrentQueue<string> ExecutedTests = new();

    [Before(TestFiltering)]
    public static void ReverseOrder(TestFilteringContext context)
    {
        // Hook ordering changes scheduling for the whole run, so only order when the run is
        // filtered to this class (as the engine test does).
        if (!TestFilteringOrderSelection.IsWholeClass<TestFilteringOrderTests>(context, testCount: 3))
        {
            return;
        }

        // Reverse alphabetical: the opposite of the classes' [NotInParallel(Order)] values.
        context.OrderTestsBy(t => t.Metadata.TestDetails.TestName, Comparer<string>.Create((x, y) => string.CompareOrdinal(y, x)));
        OrderApplied = true;
    }
}

[EngineTest(ExpectedResult.Pass)]
public class TestFilteringOrderTests
{
    [Test, NotInParallel(Order = 1)]
    public Task Order_A() => RecordAndAssert(nameof(Order_A), [nameof(Order_C), nameof(Order_B), nameof(Order_A)]);

    [Test, NotInParallel(Order = 2)]
    public Task Order_B() => RecordAndAssert(nameof(Order_B), [nameof(Order_C), nameof(Order_B)]);

    [Test, NotInParallel(Order = 3)]
    public Task Order_C() => RecordAndAssert(nameof(Order_C), [nameof(Order_C)]);

    private static async Task RecordAndAssert(string name, string[] expectedSoFar)
    {
        TestFilteringOrderHooks.ExecutedTests.Enqueue(name);

        if (TestFilteringOrderHooks.OrderApplied)
        {
            await Assert.That(TestFilteringOrderHooks.ExecutedTests.ToArray()).IsEquivalentTo(expectedSoFar, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        }
    }
}

public static class TestFilteringRankHooks
{
    public static bool RankApplied;
    public static readonly ConcurrentQueue<string> ExecutedTests = new();

    [Before(TestFiltering)]
    public static void RunSecondGroupFirst(TestFilteringContext context)
    {
        // Only rank when the run is filtered to this class (as the engine test does).
        if (!TestFilteringOrderSelection.IsWholeClass<TestFilteringRankTests>(context, testCount: 4))
        {
            return;
        }

        // Coarse key: the "Second" group starts first. Within each group [NotInParallel(Order)] still decides.
        context.OrderTestsBy(t => t.Metadata.TestDetails.TestName.StartsWith("Second") ? 0 : 1);
        RankApplied = true;
    }
}

[EngineTest(ExpectedResult.Pass)]
public class TestFilteringRankTests
{
    // Declared out of Order on purpose: within a rank, [NotInParallel(Order)] decides, not declaration order.
    [Test, NotInParallel(Order = 4)]
    public Task Second_B() => RecordAndAssert(nameof(Second_B), [nameof(Second_A), nameof(Second_B)]);

    [Test, NotInParallel(Order = 3)]
    public Task Second_A() => RecordAndAssert(nameof(Second_A), [nameof(Second_A)]);

    [Test, NotInParallel(Order = 2)]
    public Task First_B() => RecordAndAssert(nameof(First_B), [nameof(Second_A), nameof(Second_B), nameof(First_A), nameof(First_B)]);

    [Test, NotInParallel(Order = 1)]
    public Task First_A() => RecordAndAssert(nameof(First_A), [nameof(Second_A), nameof(Second_B), nameof(First_A)]);

    private static async Task RecordAndAssert(string name, string[] expectedSoFar)
    {
        TestFilteringRankHooks.ExecutedTests.Enqueue(name);

        if (TestFilteringRankHooks.RankApplied)
        {
            await Assert.That(TestFilteringRankHooks.ExecutedTests.ToArray()).IsEquivalentTo(expectedSoFar, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        }
    }
}

public static class TestFilteringKeyedOrderHooks
{
    public static bool OrderApplied;
    public static readonly ConcurrentQueue<string> ExecutedTests = new();

    [Before(TestFiltering)]
    public static void ReverseOrder(TestFilteringContext context)
    {
        // Only order when the run is filtered to this class (as the engine test does).
        if (!TestFilteringOrderSelection.IsWholeClass<TestFilteringKeyedOrderTests>(context, testCount: 3))
        {
            return;
        }

        // Keyed [NotInParallel("key")] tests go through the constraint-key scheduler, not the global sequential
        // list, so this pins that ranks reach it too: reverse alphabetical, the opposite of the Order values.
        context.OrderTestsBy(t => t.Metadata.TestDetails.TestName, Comparer<string>.Create((x, y) => string.CompareOrdinal(y, x)));
        OrderApplied = true;
    }
}

[EngineTest(ExpectedResult.Pass)]
public class TestFilteringKeyedOrderTests
{
    [Test, NotInParallel("TestFilteringKeyedOrder", Order = 1)]
    public Task Keyed_A() => RecordAndAssert(nameof(Keyed_A), [nameof(Keyed_C), nameof(Keyed_B), nameof(Keyed_A)]);

    [Test, NotInParallel("TestFilteringKeyedOrder", Order = 2)]
    public Task Keyed_B() => RecordAndAssert(nameof(Keyed_B), [nameof(Keyed_C), nameof(Keyed_B)]);

    [Test, NotInParallel("TestFilteringKeyedOrder", Order = 3)]
    public Task Keyed_C() => RecordAndAssert(nameof(Keyed_C), [nameof(Keyed_C)]);

    private static async Task RecordAndAssert(string name, string[] expectedSoFar)
    {
        TestFilteringKeyedOrderHooks.ExecutedTests.Enqueue(name);

        if (TestFilteringKeyedOrderHooks.OrderApplied)
        {
            await Assert.That(TestFilteringKeyedOrderHooks.ExecutedTests.ToArray()).IsEquivalentTo(expectedSoFar, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        }
    }
}

public static class TestFilteringKeyedPriorityHooks
{
    public static bool OrderApplied;
    public static readonly ConcurrentQueue<string> ExecutedTests = new();

    [Before(TestFiltering)]
    public static void RankLowPriorityFirst(TestFilteringContext context)
    {
        // Only order when the run is filtered to this class (as the engine test does).
        if (!TestFilteringOrderSelection.IsWholeClass<TestFilteringKeyedPriorityTests>(context, testCount: 2))
        {
            return;
        }

        // Ranks the Low test first. Keyed tests can overlap, so [ExecutionPriority] must still win.
        context.OrderTestsBy(t => t.Metadata.TestDetails.TestName.StartsWith("Low") ? 0 : 1);
        OrderApplied = true;
    }
}

[EngineTest(ExpectedResult.Pass)]
public class TestFilteringKeyedPriorityTests
{
    [Test, NotInParallel("TestFilteringKeyedPriority"), ExecutionPriority(Priority.Low)]
    public Task Low_Priority() => RecordAndAssert(nameof(Low_Priority), [nameof(High_Priority), nameof(Low_Priority)]);

    [Test, NotInParallel("TestFilteringKeyedPriority"), ExecutionPriority(Priority.High)]
    public Task High_Priority() => RecordAndAssert(nameof(High_Priority), [nameof(High_Priority)]);

    private static async Task RecordAndAssert(string name, string[] expectedSoFar)
    {
        TestFilteringKeyedPriorityHooks.ExecutedTests.Enqueue(name);

        if (TestFilteringKeyedPriorityHooks.OrderApplied)
        {
            await Assert.That(TestFilteringKeyedPriorityHooks.ExecutedTests.ToArray()).IsEquivalentTo(expectedSoFar, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        }
    }
}
