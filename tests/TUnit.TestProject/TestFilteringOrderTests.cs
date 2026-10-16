using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public static class TestFilteringOrderHooks
{
    public static bool OrderApplied;
    public static readonly ConcurrentQueue<string> ExecutedTests = new();

    [Before(TestFiltering)]
    public static void ReverseOrder(TestFilteringContext context)
    {
        // Hook ordering changes scheduling for the whole run, so only order when the run is
        // filtered to this class (as the engine test does).
        if (context.AllTests.Count == 0
            || context.AllTests.Any(t => t.Metadata.TestDetails.ClassType != typeof(TestFilteringOrderTests)))
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
        if (context.AllTests.Count == 0
            || context.AllTests.Any(t => t.Metadata.TestDetails.ClassType != typeof(TestFilteringRankTests)))
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
