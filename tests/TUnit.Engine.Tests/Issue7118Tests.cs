using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A generic class whose [Arguments] rows infer different type arguments produces the same tests in both
/// modes: a method row that infers the class's type arguments runs only on that instantiation (#7118).
/// </summary>
public class Issue7118Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("MixedRowsGenericMethod", 4)]
    [Arguments("MixedRowsClassTypeMethod", 2)]
    [Arguments("MixedRowsSeveralRowsPerType", 5)]
    [Arguments("MixedRowsWithUninferredMethodRow", 4)]
    public async Task Each_Method_Row_Runs_On_Its_Own_Instantiation(string className, int expectedTests)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7118/{className}*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Method_Row_Without_Matching_Class_Row_Fails_With_Explanation()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7118/MethodRowWithoutMatchingClassRow*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results
                    .Single(x => x.Outcome == "Failed")
                    .Output?.ErrorInfo?.Message
                    .ShouldContain("none of the class-level data rows infer")
            ]);
    }
}
