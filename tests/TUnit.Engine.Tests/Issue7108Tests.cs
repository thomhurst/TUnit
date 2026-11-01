using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// https://github.com/thomhurst/TUnit/issues/7108: a generic test method fed by a
/// [MethodDataSource] whose rows are not <c>Func&lt;tuple&gt;</c> must be discovered in both modes.
/// </summary>
public class Issue7108Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Generic_Method_With_Non_Tuple_Method_Data_Source_Is_Discovered()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7108/GenericMethodNonTupleMethodDataSourceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(41),
                result => result.ResultSummary.Counters.Passed.ShouldBe(41),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    /// <summary>
    /// Runs one table of [MethodDataSource] overload scenarios in this mode. Each row names the overload
    /// both modes must choose, so a drift between the generator's scorer and the reflection binder fails here.
    /// </summary>
    [Test]
    public async Task Both_Modes_Choose_The_Same_Data_Source_Overload()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7108/DataSourceOverloadParityTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(29),
                result => result.ResultSummary.Counters.Passed.ShouldBe(29),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    /// <summary>
    /// A private data source member on a generic class keeps the attribute's reflection fallback, because the
    /// generated factory cannot name a declaring type with type parameters. Its rows must still arrive.
    /// </summary>
    [Test]
    public async Task Private_Member_On_A_Generic_Class_Still_Supplies_Rows()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7108/GenericClassPrivateMemberDataSourceTests*/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    /// <summary>
    /// A public field declared as <c>object</c> that holds an array gives one row per element in both modes. Before
    /// this PR every field went through the attribute's reflection fallback, which decides from the runtime value; the
    /// generated factory for a field must keep doing so rather than pass the whole array as one argument.
    /// </summary>
    [Test]
    public async Task Public_Object_Field_Holding_An_Array_Yields_A_Row_Per_Element()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7108/PublicObjectFieldDataSourceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    /// <summary>
    /// Fields, properties and methods named after C# keywords (e.g. <c>@event</c>) must be escaped in generated code.
    /// </summary>
    [Test]
    public async Task Keyword_Named_Data_Source_Members_Compile_And_Run()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7108/KeywordNamedDataSourceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(6),
                result => result.ResultSummary.Counters.Passed.ShouldBe(6),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    /// <summary>
    /// Rows of a private nested type would close the generic test over a type that generated code cannot name.
    /// Reflection mode runs them. Source-generated mode must still compile, and reports each test as a failure
    /// that names the type and the fix.
    /// </summary>
    [Test]
    public async Task Private_Row_Type_Compiles_And_Is_Reported_In_Source_Generated_Mode()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7108/PrivateRowTypeDataSourceTests/*",
            testMode == TestMode.Reflection
                ?
                [
                    result => result.ResultSummary.Counters.Total.ShouldBe(2),
                    result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                ]
                :
                [
                    result => result.ResultSummary.Counters.Total.ShouldBe(2),
                    result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                    result => result.Results.ShouldAllBe(x => x.Output!.ErrorInfo!.Message!.Contains(
                        "Its rows give the type argument 'TUnit.TestProject.Bugs._7108.PrivateRowTypeDataSourceTests.Row', which source-generated code cannot name")),
                ]);
    }

    /// <summary>
    /// Protected data source members declared on a base class, read through the generated factory's reflection on
    /// the declaring type, supply their rows in both modes, including an object-typed field split by its value.
    /// </summary>
    [Test]
    public async Task Inherited_Protected_Members_Supply_Rows()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7108/InheritedProtectedMemberDataSourceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    /// <summary>
    /// With TUnit0001 suppressed and no overload accepting the Arguments, the generic test is still discovered and
    /// fails when its data method is called, in both modes, instead of disappearing.
    /// </summary>
    [Test]
    public async Task Generic_Test_Without_A_Matching_Overload_Fails_Instead_Of_Vanishing()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7108/NoMatchingOverloadDataSourceTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
            ]);
    }

    [Test]
    public async Task Single_Tuple_Parameter_Keeps_The_CancellationToken_At_Its_Declared_Position()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7108/GenericSingleTupleWithCancellationTokenTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
