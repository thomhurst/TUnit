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
    [Arguments("TypedRowDeclaredTypeWinsOverValueType", new[] { "Run<object>(1)" })]
    [Arguments("TypedRowWithOptionalParameter", new[] { "Run<int>(1)" })]
    [Arguments("TypedRowOnNullableParameter", new[] { "Run<int>(1)" })]
    [Arguments("TypedRowWithParamsCollection", new[] { "Run<object>(1)" })]
    [Arguments("TypedTestDataRowSource", new[] { "Run<int>(1)" })]
    [Arguments("TypedFuncRowSource", new[] { "Run<int>(1)" })]
    [Arguments("TypedTestDataRowOfFuncSource", new[] { "Run<int>(() => 1)" })]
    [Arguments("TypedOneElementTupleRowSource", new[] { "Run<int>(1)" })]
    [Arguments("TypedTestDataRowOfReferenceArraySource", new[] { "Run<string>([\"a\"])" })]
    [Arguments("TypedOneElementTupleOfTestDataRowSource", new[] { "Run<int>(1)" })]
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

    // The row is matched against the parameter type, so no Run<int[]> instantiation is added. The test bodies
    // assert the type argument; an array argument's display name is not part of the contract here.
    [Test]
    [Arguments("TypedCollectionRowWithGenerateGenericTest")]
    public async Task Typed_Arguments_Row_Is_Matched_Against_The_Parameter_Type(string classFilter)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7214/{classFilter}/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.Results.ShouldAllBe(x => x.TestName.StartsWith("Run<int>(", StringComparison.Ordinal))
            ]);
    }
}
