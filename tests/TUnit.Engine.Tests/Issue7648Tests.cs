using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7648Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Unrelated_Method_Source_Attributes_Do_Not_Add_Rows()
    {
        await RunTestsWithFilter("/*/TUnit.TestProject.Bugs._7648/MethodSourceIdentityTests/*",
        [
            result => result.ResultSummary.Counters.Total.ShouldBe(2),
            result => result.ResultSummary.Counters.Passed.ShouldBe(2),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0)
        ]);
    }
}
