using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Generic tests get the same tree-node path and display name in source-generated and
/// reflection mode (#7105). Path segments are the declared names without generic decoration,
/// so one filter selects every instantiation; display names carry the type arguments with
/// C# keyword aliases and no namespaces.
/// </summary>
public class Issue7105Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Generic_Method_Inferred_Per_Row_Has_Bare_Path_And_Typed_Display_Name()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7105/GenericMethodNames/Method",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.Results.Select(x => x.TestName).OrderBy(x => x, StringComparer.Ordinal).ToArray()
                    .ShouldBe(new[] { "Method<int>(5)", "Method<string>(s)" })
            ]);
    }

    [Test]
    public async Task Generic_Method_From_GenerateGenericTest_Has_Bare_Path_And_Typed_Display_Name()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7105/GenericMethodNames/Generated",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.Results.Select(x => x.TestName).OrderBy(x => x, StringComparer.Ordinal).ToArray()
                    .ShouldBe(new[] { "Generated<GenericNamePayload>", "Generated<List<int>>", "Generated<int>", "Generated<int?>" })
            ]);
    }

    [Test]
    public async Task Same_Named_Type_Arguments_From_Different_Namespaces_Stay_Distinct()
    {
        // Both display as SameSimpleName<SamePayload>; their ids must still differ or one is lost.
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7105/GenericMethodNames/SameSimpleName",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.Results.Select(x => x.TestName).ToArray()
                    .ShouldBe(new[] { "SameSimpleName<SamePayload>", "SameSimpleName<SamePayload>" })
            ]);
    }

    [Test]
    public async Task Generic_Class_Path_Has_No_Arity_Suffix()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7105/GenericClassNames/Plain",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.Results.Single().TestName.ShouldBe("Plain")
            ]);
    }
}
