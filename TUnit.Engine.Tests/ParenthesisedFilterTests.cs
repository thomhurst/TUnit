using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// End-to-end regression tests for GitHub issue #6026.
/// Parenthesised TreeNodeFilter expressions in any path segment must reach MTP's
/// authoritative matcher instead of being mis-classified as a literal method name
/// by the source-gen pre-filter.
/// </summary>
public class ParenthesisedFilterTests(TestMode testMode) : InvokableTestBase(testMode)
{
    private const string ClassPath = "/*/TUnit.TestProject.Bugs._6026/ParenthesisedFilterTests";

    [Test]
    public async Task Filter_LiteralMethodName_StillWorks()
    {
        // Sanity check — the plain-literal path was already green; guard against regression.
        await RunTestsWithFilter(
            $"{ClassPath}/MyTest1",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1,
                    $"Expected 1 test (MyTest1) but got {result.ResultSummary.Counters.Total}. " +
                    $"Test names: {string.Join(", ", result.Results.Select(r => r.TestName))}"),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Filter_ParenthesisedSingleMethod_Matches()
    {
        // Exact repro from #6026 — returned 0 tests in source-gen mode before the fix.
        await RunTestsWithFilter(
            $"{ClassPath}/(MyTest1)",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1,
                    $"Expected 1 test (MyTest1) but got {result.ResultSummary.Counters.Total}. " +
                    $"Test names: {string.Join(", ", result.Results.Select(r => r.TestName))}"),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Filter_OrAlternation_MatchesBoth()
    {
        await RunTestsWithFilter(
            $"{ClassPath}/(MyTest1|MyTest2)",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2,
                    $"Expected 2 tests (MyTest1, MyTest2) but got {result.ResultSummary.Counters.Total}. " +
                    $"Test names: {string.Join(", ", result.Results.Select(r => r.TestName))}"),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Filter_NotOperator_ExcludesNamedMethod()
    {
        // MTP TreeNodeFilter requires NOT to appear inside a grouping expression at the
        // path-segment level — bare "!MyTest1" yields zero matches in MTP itself.
        await RunTestsWithFilter(
            $"{ClassPath}/(!MyTest1)",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1,
                    $"Expected 1 test (MyTest2) but got {result.ResultSummary.Counters.Total}. " +
                    $"Test names: {string.Join(", ", result.Results.Select(r => r.TestName))}"),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
