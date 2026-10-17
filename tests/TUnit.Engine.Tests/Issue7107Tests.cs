using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Generic type arguments that break a class or method constraint, whether inferred from class
/// rows, method rows, <c>[GenerateGenericTest]</c> or <c>IInfersType&lt;T&gt;</c>, are reported as one
/// failed test per row or type, with the same name and message in both modes. Valid rows keep
/// running, and a bad class row never fails discovery of the rest of the assembly (#7107).
/// </summary>
public class Issue7107Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    private const string Namespace = "/*/TUnit.TestProject.Bugs._7107";
    private const string StructViolation = "Type 'String' does not satisfy the 'struct' constraint for generic parameter 'T'";

    [Test]
    public async Task Bad_Class_Row_Does_Not_Hide_Other_Tests_In_The_Assembly()
    {
        // Before the fix, reflection mode replaced the whole assembly with one
        // "[ASSEMBLY SCAN FAILED]" test, so this unrelated test was never discovered.
        await RunTestsWithFilter(
            $"{Namespace}/ConstraintViolationNeighbourTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.Results.Single().TestName.ShouldBe("StillDiscovered")
            ]);
    }

    [Test]
    public async Task Bad_Class_Row_Does_Not_Hide_Generic_Tests_Elsewhere_In_The_Assembly()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject/GenericConstraintValidationTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(9),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(5)
            ]);
    }

    [Test]
    [Arguments("ClassRowConstraintTests", 1)]
    [Arguments("OnlyInvalidClassRowConstraintTests", 0)]
    [Arguments("ClassGenerateGenericConstraintTests", 1)]
    [Arguments("MethodGenerateGenericConstraintTests", 1)]
    public async Task Invalid_Type_Fails_And_Valid_Types_Run(string className, int expectedPassed)
    {
        await RunTestsWithFilter(
            // Trailing '*': generic class nodes carry the arity, and source-generated
            // generic method nodes carry the type arguments.
            $"{Namespace}/{className}*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedPassed + 1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedPassed),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single(x => x.Outcome == "Failed").TestName.ShouldBe("Run"),
                result => result.Results.Single(x => x.Outcome == "Failed").Output?.ErrorInfo?.Message.ShouldContain(StructViolation)
            ]);
    }

    [Test]
    public async Task Method_Row_That_Infers_An_Invalid_Class_Type_Fails()
    {
        // Only the failed row is asserted: reflection mode does not run method rows of generic
        // classes yet (#7106), while source-generated mode also runs the valid row.
        await RunTestsWithFilter(
            $"{Namespace}/MethodRowInfersClassConstraintTests*/*",
            [
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single(x => x.Outcome == "Failed").TestName.ShouldBe("Run"),
                result => result.Results.Single(x => x.Outcome == "Failed").Output?.ErrorInfo?.Message.ShouldContain(StructViolation)
            ]);
    }

    [Test]
    public async Task Invalid_Inferred_Matrix_Type_Fails_Once_Per_Row()
    {
        await RunTestsWithFilter(
            $"{Namespace}/MatrixInfersTypeConstraintTests/Invalid*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                result => result.Results.ShouldAllBe(x => x.TestName == "Invalid"),
                result => result.Results.ShouldAllBe(x => x.Output!.ErrorInfo!.Message!.Contains(StructViolation)),
                result => result.Results.Select(x => x.TestId).Distinct().Count().ShouldBe(2)
            ]);
    }

    [Test]
    public async Task Valid_Inferred_Matrix_Type_Still_Runs()
    {
        await RunTestsWithFilter(
            $"{Namespace}/MatrixInfersTypeConstraintTests/Valid*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2)
            ]);
    }

    [Test]
    public async Task Each_Invalid_Class_Row_Fails_Each_Test_Method()
    {
        // 3 invalid rows x 2 test methods.
        await RunTestsWithFilter(
            $"{Namespace}/SeveralInvalidClassRowsConstraintTests*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(6),
                result => result.ResultSummary.Counters.Failed.ShouldBe(6),
                result => result.Results.Count(x => x.TestName == "First").ShouldBe(3),
                result => result.Results.Count(x => x.TestName == "Second").ShouldBe(3),
                result => result.Results.Count(x => x.Output!.ErrorInfo!.Message!.Contains(StructViolation)).ShouldBe(4),
                result => result.Results.Count(x => x.Output!.ErrorInfo!.Message!.Contains(
                    "Type 'DayOfWeek' does not implement interface 'IEquatable`1' required for generic parameter 'T'")).ShouldBe(2),
                result => result.Results.Select(x => x.TestId).Distinct().Count().ShouldBe(6)
            ]);
    }

    [Test]
    public async Task Each_Invalid_Method_Row_Fails()
    {
        await RunTestsWithFilter(
            $"{Namespace}/SeveralInvalidMethodRowsConstraintTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                result => result.Results.Where(x => x.Outcome == "Failed").ShouldAllBe(x => x.TestName == "Run"),
                result => result.Results.Where(x => x.Outcome == "Failed").Select(x => x.TestId).Distinct().Count().ShouldBe(2)
            ]);
    }
}
