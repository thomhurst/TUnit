using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A single tuple parameter receives the whole row, and a CancellationToken parameter receives the
/// test's token, wherever the token is declared (#7173).
/// </summary>
public class Issue7173Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Tuple_Parameter_And_CancellationToken_Bind_Wherever_The_Token_Is_Declared()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7173/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(23),
                result => result.ResultSummary.Counters.Passed.ShouldBe(23),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
