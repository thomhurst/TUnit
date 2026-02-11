using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/4737
/// Verifies that derived SkipAttribute subclasses properly skip tests
/// when combined with ClassDataSource, even when the data source's
/// IAsyncInitializer would throw.
/// </summary>
public class DerivedSkipWithClassDataSourceTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task DerivedSkipWithFailingDataSource_ShouldBeSkipped()
    {
        await RunTestsWithFilter(
            "/*/*/DerivedSkipWithFailingClassDataSourceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task DerivedSkipWithSucceedingDataSource_ShouldBeSkipped()
    {
        await RunTestsWithFilter(
            "/*/*/DerivedSkipWithSucceedingClassDataSourceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1)
            ]);
    }
}
