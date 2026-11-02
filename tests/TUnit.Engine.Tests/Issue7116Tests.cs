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

    // The generator's run-time explanation and analyzer TUnit0001's message must say the same thing.
    // Keep this list in step with DeclaredTypeMessage_Shares_Wording_With_Generator in TUnit.Analyzers.Tests.
    private static readonly string[] SharedExplanationPhrases =
    [
        "declared type",
        "not from the run-time type of",
        "Declare the data source with a type that satisfies the constraint",
        "list the type argument with [GenerateGenericTest]"
    ];

    [Test]
    [Arguments("ObjectDeclaredType", "Type 'Object' does not implement interface 'IDisposable' required for generic parameter 'T'", "'object'")]
    [Arguments("NullableDeclaredType", "Type 'Nullable`1' does not implement interface 'IComparable' required for generic parameter 'T'", "'int?'")]
    [Arguments("TypedObjectDeclaredType", "Type 'Object' does not implement interface 'IDisposable' required for generic parameter 'T'", "'object'")]
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
                result =>
                {
                    var message = result.Results.Single().Output?.ErrorInfo?.Message;
                    foreach (var phrase in SharedExplanationPhrases)
                    {
                        message.ShouldContain(phrase);
                    }
                }
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
    [Arguments("GenerateGenericTestWithExactAndObjectRows")]
    [Arguments("GenerateGenericTestWithObjectAndExactRows")]
    public async Task GenerateGenericTest_Runs_Broad_Rows_Alongside_A_Source_That_Infers_Its_Type(string methodName)
    {
        await RunTestsWithFilter(
            $"{ClassFilter}/{methodName}*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task GenerateGenericTest_With_Full_Arity_Runs_Broad_Rows_Beside_A_Wrongly_Sized_One()
    {
        // The full [GenerateGenericTest] runs the object-declared rows in both modes, and both modes fail the
        // wrongly sized one on its own (#7222).
        await RunTestsWithFilter(
            $"{ClassFilter}/MixedArityGenerateGenericTestWithObjectRows*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task GenerateGenericTest_Fails_A_Row_That_Cannot_Convert_To_Its_Type()
    {
        // [GenerateGenericTest] turns off the constraint check on the declared type, so a value of the wrong
        // run-time type fails its test at run time, naming both types.
        await RunTestsWithFilter(
            $"{ClassFilter}/GenerateGenericTestWithUnconvertibleRows*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single().Output?.ErrorInfo?.Message.ShouldContain("System.IO.StringReader"),
                result => result.Results.Single().Output?.ErrorInfo?.Message.ShouldContain("System.IO.MemoryStream")
            ]);
    }

    [Test]
    [Arguments("EmptyGenerateGenericTestWithObjectRows")]
    [Arguments("WrongArityGenerateGenericTestWithObjectRows")]
    public async Task Incomplete_GenerateGenericTest_Does_Not_Hide_Broad_Rows(string methodName)
    {
        await RunTestsWithFilter(
            $"{ClassFilter}/{methodName}*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1)
            ]);
    }
}
