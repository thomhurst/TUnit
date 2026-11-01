using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Events a data source subscribes on the builder context while its row is fetched reach the test
/// built from that row, so every scope a <c>DependencyInjectionDataSourceAttribute&lt;TScope&gt;</c>
/// creates is disposed when its test ends (#7205).
/// </summary>
public class Issue7205Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Every_Dependency_Injection_Scope_Is_Disposed()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7205/*/*",
            [
                // FailedRowScopeTests fails to build and DependencySkippedScopeTests.Fails throws, both by
                // design; the observer checks their scopes.
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(19),
                result => result.ResultSummary.Counters.Passed.ShouldBe(14),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(3)
            ]);
    }
}
