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
    public async Task GenerateGenericTest_Typed_Data_Source_Runs_For_Its_Inferred_Type()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/GenerateGenericTestTypedDataSourceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task GenerateGenericTest_Covered_By_Typed_Data_Source_Is_Not_Repeated()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/GenerateGenericTestCoveredByTypedDataSourceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task GenerateGenericTest_Matrix_Infers_Its_Own_Type()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/GenerateGenericTestMatrixInferenceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task GenerateGenericTest_Tuple_Method_Data_Source_Infers_Its_Own_Types()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/GenerateGenericTestMethodDataSourceInferenceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task GenerateGenericTest_Inferred_Instantiation_Runs_Only_Its_Own_Method_Data_Source()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/GenerateGenericTestMultipleMethodDataSourceInferenceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task GenerateGenericTest_Duplicate_Method_Data_Source_Inference_Keeps_The_First_Source()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/GenerateGenericTestDuplicateMethodDataSourceInferenceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(3),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task GenerateGenericTest_Generic_Method_Data_Source_Infers_From_Its_Provider()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/GenerateGenericTestGenericMethodDataSourceProviderTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task GenerateGenericTest_Typed_Data_Source_Duplicating_Arguments_Type_Is_Dropped()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/GenerateGenericTestTypedDataSourceDuplicatingArgumentsTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task GenerateGenericTest_Repeated_Arguments_Rows_Run_On_A_Generic_Class()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/GenerateGenericTestGenericClassArgumentsRowsTests*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task GenerateGenericTest_Null_Arguments_Row_Runs_As_Object()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/GenerateGenericTestTypedNullArgumentsTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task GenerateGenericTest_Eight_Element_Tuple_Infers_Eight_Types()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7092/GenerateGenericTestEightElementTupleTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
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
