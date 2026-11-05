using Shouldly;
using TUnit.Engine.Tests.Enums;
using TUnit.Tests.Shared;

namespace TUnit.Engine.Tests;

public class Issue7617Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Convertible_Row_Preserves_Optional_Defaults()
    {
        const string filter = "/*/TUnit.TestProject.Bugs._7617/ConvertibleOptionalRowTests/*";
        List<Action<TrxTools.TrxParser.TestRun>> assertions =
        [
            result => result.ResultSummary.Counters.Total.ShouldBe(9),
            result => result.ResultSummary.Counters.Passed.ShouldBe(9),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0)
        ];
        await RunTestsWithFilter(filter, assertions);
        if (testMode == TestMode.Reflection)
        {
            await RunTestsWithSourceGeneration(filter, assertions);
        }
    }
}
