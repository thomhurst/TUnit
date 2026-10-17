using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// https://github.com/thomhurst/TUnit/issues/7092: data source and hook shapes that behaved differently
/// in reflection mode and source-generated mode.
/// </summary>
public class Issue7092Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Parameterless_Test_With_Method_Data_Source_Runs()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/ParameterlessMethodDataSourceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task ValueTask_Method_Data_Source_Is_Awaited()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/ValueTaskMethodDataSourceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task GenerateGenericTest_Pairs_Arguments_With_Matching_Type()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/GenerateGenericTestWithArgumentsTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task GenerateGenericTest_Runs_Rows_For_Their_Inferred_Type()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/GenerateGenericTestWithConvertibleArgumentsTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task GenerateGenericTest_Not_Inferred_From_Arguments_Runs_Every_Row()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/GenerateGenericTestNotInferredFromArgumentsTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Static_Class_Hook_On_Generic_Base_Runs_Once()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/GenericBaseWithClassHookTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Empty_Method_Data_Source_Runs_Test_Once_With_No_Arguments()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/EmptyMethodDataSourceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Empty_Method_Data_Source_With_SkipIfEmpty_Is_Reported_As_Skipped()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/SkipIfEmptyMethodDataSourceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
