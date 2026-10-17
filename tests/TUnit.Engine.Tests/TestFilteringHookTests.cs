using Shouldly;
using TrxTools.TrxParser;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// [Before/After(TestFiltering)] and [After(TestDiscovery)] hooks that exclude, skip and order tests
/// (https://github.com/thomhurst/TUnit/issues/4788).
/// </summary>
public class TestFilteringHookTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Excluded_Tests_Are_Not_Reported_And_Skipped_Tests_Are()
    {
        await RunTestsWithFilter("/*/*/TestFilteringHookTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                // ExcludedByHook and ExcludedAtDiscovery are dropped entirely.
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1),
                result => result.Results.ShouldNotContain(x => x.TestName!.StartsWith("ExcludedByHook")),
                result => result.Results.ShouldNotContain(x => x.TestName!.StartsWith("ExcludedAtDiscovery"))
            ]);
    }

    [Test]
    public async Task Excluded_Dependency_Still_Runs_And_Skipped_Dependency_Skips_Dependents()
    {
        await RunTestsWithFilter("/*/*/TestFilteringDependencyTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                // Includes HookSkip_PropagatesThroughTheDependencyFailurePath (pins the skip reasons) and a
                // dependency that is excluded at discovery and re-included with its attributes intact.
                result => result.ResultSummary.Counters.Total.ShouldBe(7),
                result => result.ResultSummary.Counters.Passed.ShouldBe(5),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(2),
                result => result.Results.First(x => x.TestName!.StartsWith("ExcludedDependency")).Outcome.ShouldBe("Passed"),
                result => result.Results.First(x => x.TestName!.StartsWith("ExcludedAtDiscoveryDependency")).Outcome.ShouldBe("Passed")
            ]);
    }

    [Test]
    public async Task Overloaded_After_TestFiltering_Hooks_All_Run()
    {
        await RunTestsWithFilter("/*/*/TestFilteringOverloadTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task Dependency_Lookups_Ignore_Hook_Excluded_Tests()
    {
        await RunTestsWithFilter("/*/*/TestFilteringLookupTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task OrderTestsBy_Overrides_NotInParallel_Order()
    {
        await RunTestsWithFilter("/*/*/TestFilteringOrderTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(3),
                result =>
                {
                    // [NotInParallel(Order)] alone would run A, B, C; the hook reverses it.
                    var a = StartOf(result, "Order_A");
                    var b = StartOf(result, "Order_B");
                    var c = StartOf(result, "Order_C");

                    c.ShouldBeLessThan(b);
                    b.ShouldBeLessThan(a);
                }
            ]);
    }

    [Test]
    public async Task OrderTestsBy_With_Equal_Keys_Keeps_NotInParallel_Order_Within_A_Rank()
    {
        await RunTestsWithFilter("/*/*/TestFilteringRankTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result =>
                {
                    // [NotInParallel(Order)] alone would run First_A, First_B, Second_A, Second_B. The hook ranks
                    // the Second group first, and Order still sequences the tests within each group.
                    var secondA = StartOf(result, "Second_A");
                    var secondB = StartOf(result, "Second_B");
                    var firstA = StartOf(result, "First_A");
                    var firstB = StartOf(result, "First_B");

                    secondA.ShouldBeLessThan(secondB);
                    secondB.ShouldBeLessThan(firstA);
                    firstA.ShouldBeLessThan(firstB);
                }
            ]);
    }

    [Test]
    public async Task OrderTestsBy_Orders_Keyed_NotInParallel_Tests()
    {
        await RunTestsWithFilter("/*/*/TestFilteringKeyedOrderTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(3),
                result =>
                {
                    // Keyed tests run through the constraint-key scheduler. [NotInParallel(Order)] alone would
                    // run A, B, C; the hook reverses it.
                    var a = StartOf(result, "Keyed_A");
                    var b = StartOf(result, "Keyed_B");
                    var c = StartOf(result, "Keyed_C");

                    c.ShouldBeLessThan(b);
                    b.ShouldBeLessThan(a);
                }
            ]);
    }

    [Test]
    public async Task OrderTestsBy_Keeps_ExecutionPriority_Ahead_Of_Rank_For_Keyed_Tests()
    {
        await RunTestsWithFilter("/*/*/TestFilteringKeyedPriorityTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result =>
                {
                    // The hook ranks Low_Priority first, but keyed tests can overlap, so [ExecutionPriority] wins.
                    StartOf(result, "High_Priority").ShouldBeLessThan(StartOf(result, "Low_Priority"));
                }
            ]);
    }

    private static DateTime StartOf(TestRun result, string testName)
        => DateTime.Parse(result.Results.First(x => x.TestName!.StartsWith(testName)).StartTime!);
}
