using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class HtmlReportCliTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Combined_Html_Report_Packages_Accept_TUnit_Namespaced_Option()
    {
        await RunTestsWithFilter(
            "/*/*/BasicTests/SynchronousTest",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
            ],
            new RunOptions()
                .WithArgument("--tunit-report-html-filename")
                .WithArgument("tunit-report.html"));
    }
}
