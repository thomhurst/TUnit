using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// https://github.com/thomhurst/TUnit/issues/7164: a typed data source (DataSourceGeneratorAttribute&lt;T&gt;)
/// infers the type arguments of a generic method of a generic class, as on a non-generic class, in both modes.
/// </summary>
public class Issue7164Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("TypedSourceOnClassGenerateGenericTest", new[] { "Run<int>(42)" })]
    [Arguments("TypedSourceAndMethodGenerateGenericTest", new[] { "Run<int>(42)", "Run<long>(42)" })]
    [Arguments("TypedSourceAndSameMethodGenerateGenericTest", new[] { "Run<int>(42)" })]
    [Arguments("TypedSourceOnClassRow", new[] { "Run<int>(42)" })]
    [Arguments("ClassRowTypedSourceAndMethodGenerateGenericTest", new[] { "Run<int>(42)", "Run<long>(42)" })]
    [Arguments("TwoTypeArgumentTypedSourceOnGenericClass", new[] { "Run<int, string>(42, x)" })]
    public async Task Typed_Source_Infers_Method_Type_Arguments_On_Generic_Class(string className, string[] expectedTestNames)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7164/{className}*/*",
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
