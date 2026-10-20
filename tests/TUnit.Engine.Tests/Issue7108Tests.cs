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
                result => result.ResultSummary.Counters.Total.ShouldBe(39),
                result => result.ResultSummary.Counters.Passed.ShouldBe(39),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    /// <summary>
    /// Runs one table of [MethodDataSource] overload scenarios in this mode. Each row names the overload
    /// both modes must choose, so a drift between the generator's scorer and the reflection binder fails here.
    /// </summary>
    [Test]
    public async Task Both_Modes_Choose_The_Same_Data_Source_Overload()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7108/DataSourceOverloadParityTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(25),
                result => result.ResultSummary.Counters.Passed.ShouldBe(25),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    /// <summary>
    /// A private data source member on a generic class keeps the attribute's reflection fallback, because the
    /// generated factory cannot name a declaring type with type parameters. Its rows must still arrive.
    /// </summary>
    [Test]
    public async Task Private_Member_On_A_Generic_Class_Still_Supplies_Rows()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7108/GenericClassPrivateMemberDataSourceTests*/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Single_Tuple_Parameter_Keeps_The_CancellationToken_At_Its_Declared_Position()
    {
        Skip.When(testMode == TestMode.Reflection, "Reflection mode does not bind a single tuple parameter next to a CancellationToken yet: https://github.com/thomhurst/TUnit/issues/7173");

        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7108/GenericSingleTupleWithCancellationTokenTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
