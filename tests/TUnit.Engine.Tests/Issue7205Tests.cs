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
                // FailedRowScopeTests, ThrowingCreateScopeTests and InitFailingScopeTests fail to build, DependencySkippedScopeTests.Fails
                // throws and both CircularDependencyScopeTests fail their circular dependency, all by design;
                // the observer checks their scopes.
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(23),
                result => result.ResultSummary.Counters.Passed.ShouldBe(14),
                result => result.ResultSummary.Counters.Failed.ShouldBe(6),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(3)
            ]);
    }

    [Test]
    public async Task Scope_Of_A_Test_Excluded_By_A_Property_Filter_Is_Disposed()
    {
        // The property filter is evaluated after building, so Excluded is built, its scope created, and
        // then dropped from the run; Observer checks that scope was disposed.
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7205.FilterExcluded/*/*[Issue7205=Observer]",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1)
            ]);
    }
}
