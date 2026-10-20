using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// https://github.com/thomhurst/TUnit/issues/7108: a generic test method fed by a
/// [MethodDataSource] whose rows are not <c>Func&lt;tuple&gt;</c> must be discovered in both modes.
/// </summary>
public class Issue7108Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Generic_Method_With_Non_Tuple_Method_Data_Source_Is_Discovered()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7108/GenericMethodNonTupleMethodDataSourceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(25),
                result => result.ResultSummary.Counters.Passed.ShouldBe(25),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
