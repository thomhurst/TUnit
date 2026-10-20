using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// End-to-end regression tests for https://github.com/thomhurst/TUnit/issues/7126.
/// Display names must format numeric arguments with the invariant culture, identically in
/// source-generated and reflection mode, whatever culture the test process runs under.
/// </summary>
public class Bug7126CultureInvariantDisplayNameTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("en-US")]
    [Arguments("sv-SE")]
    [Arguments("de-DE")]
    public async Task Numeric_Arguments_Are_Displayed_Invariantly(string culture)
    {
        await RunTestsWithFilter(
            "/*/*/Bug7126CultureInvariantDisplayNameTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.Results.Single().TestName.ShouldBe("NumericArguments(1.5, -2, 2.25, 3.75)")
            ],
            new RunOptions().WithEnvironmentVariable("TUNIT_BUG_7126_CULTURE", culture));
    }
}
