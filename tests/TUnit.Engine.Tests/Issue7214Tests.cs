using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// https://github.com/thomhurst/TUnit/issues/7214: a typed [Arguments&lt;T&gt;] row on a generic test method
/// runs in both modes, also when another row infers the same type arguments. A generic class is matched with a
/// trailing *, since its tree node name carries its arity.
/// </summary>
public class Issue7214Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("ArgumentsAndTypedArgumentsRow", new[] { "Run<int>(1)", "Run<int>(2)" })]
    [Arguments("TypedArgumentsRowThenArguments", new[] { "Run<int>(1)", "Run<int>(2)" })]
    [Arguments("TwoTypedArgumentsRows", new[] { "Run<int>(1)", "Run<int>(2)" })]
    [Arguments("ArgumentsAndTypedArgumentsRowOnGenericClass*", new[] { "Run<int>(1)", "Run<int>(2)" })]
    [Arguments("ArgumentsAndTypedArgumentsRowWithGenerateGenericTest", new[] { "Run<int>(1)", "Run<int>(2)" })]
    [Arguments("TwoTypedArgumentsRowsWithGenerateGenericTest", new[] { "Run<int>(1)", "Run<int>(2)" })]
    [Arguments("TypedArgumentsRowOfOtherTypeWithGenerateGenericTest", new[] { "Run<int>(1)", "Run<long>(2)" })]
    public async Task Typed_Arguments_Row_Runs_On_Generic_Method(string classFilter, string[] expectedTestNames)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7214/{classFilter}/*",
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
