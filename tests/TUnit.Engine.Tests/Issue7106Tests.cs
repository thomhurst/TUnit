using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Every class-level and method-level [Arguments] row on a generic test class produces its own test,
/// whatever type it infers, in both modes (#7106).
/// </summary>
public class Issue7106Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("GenericClassSameTypeRows", 3)]
    [Arguments("GenericClassMixedTypeRows", 4)]
    [Arguments("GenericClassSameTypeMethodRows", 2)]
    [Arguments("GenericClassMixedTypeMethodRows", 3)]
    [Arguments("GenericClassRowsTimesGenericMethodRows", 4)]
    [Arguments("GenericClassRowsTimesMethodRows", 4)]
    [Arguments("GenericClassRowWithMethodRow", 1)]
    [Arguments("OverloadedGenericClassMethodRows", 2)]
    [Arguments("GenerateGenericTestWithMethodRows", 2)]
    [Arguments("GenerateGenericTestWithUninferredMethodRow", 2)]
    [Arguments("GenerateGenericTestWithTypeMethodRow", 1)]
    [Arguments("ClassRowWithUninferredMethodRow", 1)]
    [Arguments("GenerateGenericTestWithClassRowAndUninferredMethodRow", 1)]
    [Arguments("ClassRowWithInferredAndUninferredMethodRows", 2)]
    [Arguments("GenericMethodUninferredMethodRow", 2)]
    [Arguments("GenericMethodSingleNullMethodRow", 2)]
    [Arguments("NonGenericClassGenericMethodNullMethodRow", 2)]
    [Arguments("TypedNullMethodRowInfersObject", 1)]
    [Arguments("NullElementMethodRowInfersObject", 1)]
    [Arguments("NullArrayMethodRowOnConstrainedClass", 1)]
    public async Task Every_Row_Runs(string className, int expectedTests)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7106/{className}*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Each_Class_Row_Reaches_Its_Own_Test()
    {
        // Counting tests alone would pass if every test received the first row's value
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7106/GenericClassSameTypeRowValues*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Passed.ShouldBe(3),
                result => result.Results
                    .SelectMany(x => (x.Output?.StdOut ?? "").Split('\n'))
                    .Select(line => line.Trim())
                    .Where(line => line.StartsWith("Issue7106 class row value:", StringComparison.Ordinal))
                    .OrderBy(line => line, StringComparer.Ordinal)
                    .ShouldBe([
                        "Issue7106 class row value: 1",
                        "Issue7106 class row value: 2",
                        "Issue7106 class row value: 3"
                    ])
            ]);
    }

    [Test]
    [Arguments("PartitionedClassDataSourceReceiver")]
    [Arguments("SingleTypeClassDataSourceReadOnce")]
    public async Task Class_Data_Source_Is_Read_Once_And_Keeps_Its_Registration_Receiver(string className)
    {
        // Source generation cannot infer type arguments from an untyped runtime data source, so the
        // classes are only discovered in reflection mode
        Skip.When(testMode != TestMode.Reflection, "Only reflection mode discovers these classes");

        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7106/{className}*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Deferred_Class_Data_Source_Stays_Deferred_After_Type_Inference()
    {
        Skip.When(testMode != TestMode.Reflection, "Only reflection mode discovers this class");

        // 3 rows inferring one type => 3 cases + 1 placeholder container. Expanded during discovery, the
        // rows would produce 3 tests and no placeholder.
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7106/DeferredClassDataSourceReceiver*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    [Arguments("RowSettingsOnGenericClassRows")]
    [Arguments("RowSettingsOnNonGenericClassRows")]
    public async Task Class_Row_Settings_Apply_Only_To_Their_Own_Row(string className)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7106/{className}*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1)
            ]);
    }
}
