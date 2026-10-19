using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Source-generated mode infers a generic test method's type arguments from its data source's declared
/// type, because it creates the generic calls at compile time to stay Native AOT compatible. Reflection
/// mode infers them from the values' run-time types. A broad declared type that breaks a constraint
/// therefore fails in source-generated mode, with a message that explains why and how to fix it, while
/// reflection mode runs the test. <c>[GenerateGenericTest]</c> runs the same rows in both modes (#7116).
/// </summary>
public class Issue7116Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    private const string ClassFilter = "/*/TUnit.TestProject.Bugs._7116/DeclaredTypeInferenceTests";

    [Test]
    [Arguments("ObjectDeclaredType", "Type 'Object' does not implement interface 'IDisposable' required for generic parameter 'T'", "'object'")]
    [Arguments("NullableDeclaredType", "Type 'Nullable`1' does not implement interface 'IComparable' required for generic parameter 'T'", "'int?'")]
    public async Task Broad_Declared_Type_Fails_In_Source_Generated_Mode_With_Explanation(string methodName, string constraintMessage, string declaredType)
    {
        if (testMode == TestMode.Reflection)
        {
            // Reflection mode closes the method over the value's run-time type.
            await RunTestsWithFilter(
                $"{ClassFilter}/{methodName}*",
                [
                    result => result.ResultSummary.Counters.Total.ShouldBe(1),
                    result => result.ResultSummary.Counters.Passed.ShouldBe(1)
                ]);
            return;
        }

        await RunTestsWithFilter(
            $"{ClassFilter}/{methodName}*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single().Output?.ErrorInfo?.Message.ShouldContain(constraintMessage),
                result => result.Results.Single().Output?.ErrorInfo?.Message.ShouldContain($"'T' was inferred from the declared type {declaredType} of the data source"),
                result => result.Results.Single().Output?.ErrorInfo?.Message.ShouldContain("[GenerateGenericTest]")
            ]);
    }

    [Test]
    [Arguments("GenerateGenericTestWithObjectRows")]
    [Arguments("GenerateGenericTestWithSingleObjectRows")]
    public async Task GenerateGenericTest_Runs_Broad_Declared_Type_Rows_In_Both_Modes(string methodName)
    {
        // Before the fix, source-generated mode also reported the tuple rows' declared type (object)
        // as a constraint violation, while reflection mode skipped it.
        await RunTestsWithFilter(
            $"{ClassFilter}/{methodName}*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task GenerateGenericTest_Runs_Broad_Rows_Alongside_A_Source_That_Infers_Its_Type()
    {
        await RunTestsWithFilter(
            $"{ClassFilter}/GenerateGenericTestWithExactAndObjectRows*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Empty_GenerateGenericTest_Does_Not_Hide_Broad_Rows()
    {
        await RunTestsWithFilter(
            $"{ClassFilter}/EmptyGenerateGenericTestWithObjectRows*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1)
            ]);
    }
}
