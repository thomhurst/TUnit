using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// https://github.com/thomhurst/TUnit/issues/7117: [GenerateGenericTest] on a generic method of a
/// generic class closes the method on every class the class sources produce, in both modes.
/// </summary>
public class Issue7117Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("MethodGenerateGenericTestOnClassRow", new[] { "Run<int>(2)" })]
    [Arguments("MethodGenerateGenericTestWithClassRows", new[] { "Run<bool>", "Run<bool>" })]
    [Arguments("ClassGenerateGenericTestWithMethodRow", new[] { "Run<int>(1)" })]
    [Arguments("MethodGenerateGenericTestTypeNotInferredByRow", new[] { "Run<int>(2)", "Run<long>(2)" })]
    [Arguments("MethodDataSourceOnClassRow", new[] { "Run<string>(a, 1)" })]
    [Arguments("MethodDataSourceOnClassGenerateGenericTest", new[] { "Run<string>(a, 1)" })]
    [Arguments("ClassTypedMethodDataSourceOnClassGenerateGenericTest", new[] { "Run<long>(0, 1)" })]
    [Arguments("TypedDataSourceOnClassGenerateGenericTest", new[] { "Run<long>(42)" })]
    public async Task Generic_Method_Of_Generic_Class_Runs_Each_Instantiation(string className, string[] expectedTestNames)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7117/{className}*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedTestNames.Length),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedTestNames.Length),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.Results.Select(x => x.TestName).OrderBy(x => x, StringComparer.Ordinal)
                    .ShouldBe(expectedTestNames.OrderBy(x => x, StringComparer.Ordinal))
            ]);
    }

    // A method constraint violation paired with a class row fails once per row of the method source
    // ([MethodDataSource] rows, or every method data source for IInfersType<T>), not once in total.
    [Test]
    [Arguments("MethodDataSourceConstraintViolationOnClassRow")]
    [Arguments("MatrixConstraintViolationOnClassRow")]
    public async Task Method_Constraint_Violation_On_Class_Row_Fails_Once_Per_Row(string className)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7117/{className}/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                result => result.Results.ShouldAllBe(x => x.TestName == "Run"),
                result => result.Results.ShouldAllBe(x => x.Output!.ErrorInfo!.Message!.Contains("TMethod"))
            ]);
    }
}
