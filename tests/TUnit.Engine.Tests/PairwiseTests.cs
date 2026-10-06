using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class PairwiseTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        // Pairwise_Range uses [MatrixRange<T>], which is only compiled for .NET 8+.
        var expectedCount = IsNetFramework ? 106 : 212;

        await RunTestsWithFilter(
            "/*/*/PairwiseTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedCount, "Total"),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedCount, "Passed"),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0, "Failed"),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0, "Skipped"),
                result => result.Results.Select(x => x.TestName).Distinct().Count().ShouldBe(expectedCount, "Distinct test names"),
                result => CountOf(result, "Pairwise_Bools_And_Ints").ShouldBe(16, "Pairwise_Bools_And_Ints"),
                result => CountOf(result, "Pairwise_Strings_And_Ints").ShouldBe(9, "Pairwise_Strings_And_Ints"),
                result => CountOf(result, "Pairwise_Enum_And_Nullable").ShouldBe(17, "Pairwise_Enum_And_Nullable"),
                result => CountOf(result, "Pairwise_Nullable_Enum").ShouldBe(8, "Pairwise_Nullable_Enum"),
                result => CountOf(result, "Pairwise_Matrix_Excluding").ShouldBe(6, "Pairwise_Matrix_Excluding"),
                result => CountOf(result, "Pairwise_Exclusion").ShouldBe(10, "Pairwise_Exclusion"),
                result => CountOf(result, "Pairwise_Enum_Exclusion").ShouldBe(9, "Pairwise_Enum_Exclusion"),
                result => CountOf(result, "Pairwise_Custom_Seed").ShouldBe(31, "Pairwise_Custom_Seed"),
                result => CountOf(result, "Pairwise_Range").ShouldBe(IsNetFramework ? 0 : 106, "Pairwise_Range"),
            ]);
    }

    private static int CountOf(TrxTools.TrxParser.TestRun result, string testName)
    {
        return result.Results.Count(x => x.TestName!.StartsWith(testName + "(", StringComparison.Ordinal));
    }
}
