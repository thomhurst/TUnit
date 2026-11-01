using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// https://github.com/thomhurst/TUnit/issues/7206: a typed data source closes a generic test method over its
/// declared type argument in both modes, also without a method-level [GenerateGenericTest]. Reflection mode
/// used to close it over the run-time type of the values instead.
/// </summary>
public class Issue7206Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("ObjectSourceOnNonGenericClass", new[] { "Run<object>(str)" })]
    [Arguments("ObjectSourceOnGenericClass", new[] { "Run<object>(str)" })]
    [Arguments("ObjectSourceAndArgumentsRow", new[] { "Run<int>(1)", "Run<object>(str)" })]
    [Arguments("GenericClassObjectSourceAndArgumentsRow", new[] { "Run<int>(1)", "Run<object>(str)" })]
    [Arguments("TwoObjectSources", new[] { "Run<object>(other)", "Run<object>(str)" })]
    [Arguments("ObjectSourceOnClassRows", new[] { "Run<object>(str)", "Run<object>(str)" })]
    public async Task Typed_Source_Closes_Method_Over_Declared_Type(string className, string[] expectedTestNames)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7206/{className}*/*",
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
