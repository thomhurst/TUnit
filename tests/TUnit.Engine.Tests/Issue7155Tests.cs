using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A null and a value-type value for the same type parameter of a generic test method infer object in
/// both modes, instead of the value type with the null turned into its default (#7155).
/// </summary>
public class Issue7155Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("NullAndValueTypeGenericMethod", 2)]
    [Arguments("NullAndValueTypeGenerateGenericTestMethod", 1)]
    [Arguments("NullAndValueTypeWithTokenGenerateGenericTestMethod", 1)]
    [Arguments("NullableCollectionAndNullGenericMethod", 1)]
    public async Task Null_And_Value_Type_Row_Runs_As_Object(string className, int expectedTests)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7155/{className}/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    /// <summary>
    /// With a 'struct' constraint, the object inferred for a null row breaks the constraint, so each row fails
    /// with the constraint message rather than running with the null turned into 0.
    /// </summary>
    [Test]
    public async Task Null_And_Value_Type_Row_With_Struct_Constraint_Fails_With_The_Constraint_Message()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7155/NullAndValueTypeStructConstraintMethod/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                result => result.Results.ShouldAllBe(x => x.Output!.ErrorInfo!.Message!.Contains(
                    "Type 'Object' does not satisfy the 'struct' constraint for generic parameter 'T'"))
            ]);
    }
}
