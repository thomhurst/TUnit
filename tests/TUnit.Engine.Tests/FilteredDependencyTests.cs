using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Regression test for GitHub issue #3637: Filtered dependent test should also run dependency
/// https://github.com/thomhurst/TUnit/issues/3637
/// </summary>
public class FilteredDependencyTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task FilteringDependentTest_ShouldAlsoRunDependency()
    {
        // When filtering to run only DependentTest, BaseTest should also be executed as a dependency
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._3627/FilteredDependencyTests/DependentTest",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2, "Both BaseTest and DependentTest should run"),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0),
                result =>
                {
                    // Verify both tests ran
                    var baseTest = result.Results.FirstOrDefault(x => x.TestName!.Contains("BaseTest"));
                    var dependentTest = result.Results.FirstOrDefault(x => x.TestName!.Contains("DependentTest"));

                    baseTest.ShouldNotBeNull("BaseTest should have been executed as a dependency");
                    dependentTest.ShouldNotBeNull("DependentTest should have been executed");
                },
                result =>
                {
                    // Verify execution order (BaseTest before DependentTest)
                    var baseTestStart = DateTime.Parse(
                        result.Results.First(x => x.TestName!.Contains("BaseTest")).StartTime!);
                    var dependentTestStart = DateTime.Parse(
                        result.Results.First(x => x.TestName!.Contains("DependentTest")).StartTime!);

                    dependentTestStart.ShouldBeGreaterThanOrEqualTo(baseTestStart,
                        "DependentTest should run after BaseTest");
                }
            ]);
    }
}
