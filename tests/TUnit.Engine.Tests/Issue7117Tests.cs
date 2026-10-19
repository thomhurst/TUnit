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
}
