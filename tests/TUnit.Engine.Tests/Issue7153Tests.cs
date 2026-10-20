using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// With a method-level <c>[GenerateGenericTest]</c>, an <c>[Arguments]</c> row whose inferred type
/// argument breaks the method's constraints is reported as a failed test in both modes. Reflection mode
/// used to drop the row silently (#7153).
/// </summary>
public class Issue7153Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    private const string ClassFilter = "/*/TUnit.TestProject.Bugs._7153/GenerateGenericTestArgumentsConstraintTests";
    private const string StructViolation = "Type 'String' does not satisfy the 'struct' constraint for generic parameter 'T'";

    [Test]
    public async Task Invalid_Row_Fails_And_Valid_Row_Runs()
    {
        // Trailing '*': source-generated generic method nodes carry the type arguments (e.g. "<int>").
        await RunTestsWithFilter(
            $"{ClassFilter}/Run*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single(x => x.Outcome == "Failed").TestName.ShouldBe("Run"),
                result => result.Results.Single(x => x.Outcome == "Failed").Output?.ErrorInfo?.Message.ShouldContain(StructViolation)
            ]);
    }

    [Test]
    public async Task Each_Invalid_Row_Fails()
    {
        await RunTestsWithFilter(
            $"{ClassFilter}/TwoInvalidRows*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                result => result.Results.Where(x => x.Outcome == "Failed").ShouldAllBe(x => x.TestName == "TwoInvalidRows"),
                result => result.Results.Where(x => x.Outcome == "Failed").ShouldAllBe(x => x.Output!.ErrorInfo!.Message!.Contains(StructViolation)),
                result => result.Results.Where(x => x.Outcome == "Failed").Select(x => x.TestId).Distinct().Count().ShouldBe(2)
            ]);
    }

    [Test]
    public async Task Invalid_Row_And_Invalid_Type_Each_Fail()
    {
        await RunTestsWithFilter(
            $"{ClassFilter}/InvalidRowAndInvalidType*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                result => result.Results.ShouldAllBe(x => x.TestName == "InvalidRowAndInvalidType"),
                result => result.Results.ShouldAllBe(x => x.Output!.ErrorInfo!.Message!.Contains(StructViolation)),
                result => result.Results.Select(x => x.TestId).Distinct().Count().ShouldBe(2)
            ]);
    }

    [Test]
    public async Task Injected_Token_Before_The_Data_Parameter_Is_Not_Matched_With_A_Row_Value()
    {
        await RunTestsWithFilter(
            $"{ClassFilter}/TokenBeforeValue*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single(x => x.Outcome == "Failed").TestName.ShouldBe("TokenBeforeValue"),
                result => result.Results.Single(x => x.Outcome == "Failed").Output?.ErrorInfo?.Message.ShouldContain(StructViolation)
            ]);
    }
}
