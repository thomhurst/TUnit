using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A generic test method whose data-inferred type argument breaks its generic constraints must be
/// reported as a failed test with the same message in source-generated and reflection modes,
/// rather than being silently dropped in source-generated mode (#7097).
/// </summary>
public class GenericConstraintValidationTests(TestMode testMode) : InvokableTestBase(testMode)
{
    private const string ClassFilter = "/*/*/GenericConstraintValidationTests";

    [Test]
    public async Task Whole_Class_Has_Same_Shape_In_Both_Modes()
    {
        // 4 valid rows pass; 5 rows whose inferred type breaks a constraint fail.
        await RunTestsWithFilter(
            $"{ClassFilter}/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(9),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(5),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }

    [Test]
    [Arguments("GenericMethodWithClassConstraint_InvalidType", "Type 'Int32' does not satisfy the 'class' constraint for generic parameter 'T'")]
    [Arguments("GenericMethodWithInterfaceConstraint_InvalidType", "Type 'NonImplementingClass' does not implement interface 'ITestInterface' required for generic parameter 'T'")]
    [Arguments("GenericMethodWithStructConstraint_InvalidType", "Type 'String' does not satisfy the 'struct' constraint for generic parameter 'T'")]
    [Arguments("GenericMethodWithStructConstraint_SkippedInvalidType", "Type 'String' does not satisfy the 'struct' constraint for generic parameter 'T'")]
    public async Task Invalid_Row_Fails_With_Constraint_Message(string methodName, string expectedMessage)
    {
        await RunTestsWithFilter(
            // Trailing '*': source-generated method nodes carry the type arguments (e.g. "<int>").
            $"{ClassFilter}/{methodName}*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0),
                result => result.Results.Single().Output?.ErrorInfo?.Message.ShouldContain(expectedMessage)
            ]);
    }

    [Test]
    public async Task Mixed_Rows_Run_Valid_Row_And_Fail_Invalid_Row()
    {
        await RunTestsWithFilter(
            $"{ClassFilter}/GenericMethodWithStructConstraint_MixedRows*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single(x => x.Outcome == "Failed").Output?.ErrorInfo?.Message
                    .ShouldContain("Type 'String' does not satisfy the 'struct' constraint for generic parameter 'T'")
            ]);
    }
}
