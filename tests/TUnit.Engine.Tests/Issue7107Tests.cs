using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Generic type arguments that break a class or method constraint, whether inferred from class
/// rows, method rows, <c>[GenerateGenericTest]</c> or <c>IInfersType&lt;T&gt;</c>, are reported as one
/// failed test per row or type, with the same name and message in both modes. Valid rows keep
/// running, and a bad class row never fails discovery of the rest of the assembly (#7107).
/// </summary>
public class Issue7107Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    private const string Namespace = "/*/TUnit.TestProject.Bugs._7107";
    private const string StructViolation = "Type 'String' does not satisfy the 'struct' constraint for generic parameter 'T'";

    [Test]
    public async Task Bad_Class_Row_Does_Not_Hide_Other_Tests_In_The_Assembly()
    {
        // Before the fix, reflection mode replaced the whole assembly with one
        // "[ASSEMBLY SCAN FAILED]" test, so this unrelated test was never discovered.
        await RunTestsWithFilter(
            $"{Namespace}/ConstraintViolationNeighbourTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.Results.Single().TestName.ShouldBe("StillDiscovered")
            ]);
    }

    [Test]
    public async Task Bad_Class_Row_Does_Not_Hide_Generic_Tests_Elsewhere_In_The_Assembly()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject/GenericConstraintValidationTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(9),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(5)
            ]);
    }

    [Test]
    [Arguments("ClassRowConstraintTests", 1)]
    [Arguments("OnlyInvalidClassRowConstraintTests", 0)]
    [Arguments("ClassGenerateGenericConstraintTests", 1)]
    [Arguments("MethodGenerateGenericConstraintTests", 1)]
    public async Task Invalid_Type_Fails_And_Valid_Types_Run(string className, int expectedPassed)
    {
        await RunTestsWithFilter(
            // Trailing '*': generic class nodes carry the arity, and source-generated
            // generic method nodes carry the type arguments.
            $"{Namespace}/{className}*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedPassed + 1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedPassed),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single(x => x.Outcome == "Failed").TestName.ShouldBe("Run"),
                result => result.Results.Single(x => x.Outcome == "Failed").Output?.ErrorInfo?.Message.ShouldContain(StructViolation)
            ]);
    }

    [Test]
    public async Task Method_Row_That_Infers_An_Invalid_Class_Type_Fails()
    {
        // The valid row runs and the invalid row fails, in both modes (#7106).
        await RunTestsWithFilter(
            $"{Namespace}/MethodRowInfersClassConstraintTests*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single(x => x.Outcome == "Failed").TestName.ShouldBe("Run"),
                result => result.Results.Single(x => x.Outcome == "Failed").Output?.ErrorInfo?.Message.ShouldContain(StructViolation)
            ]);
    }

    [Test]
    public async Task Invalid_Inferred_Matrix_Type_Fails_Once_Per_Row()
    {
        await RunTestsWithFilter(
            $"{Namespace}/MatrixInfersTypeConstraintTests/Invalid*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                result => result.Results.ShouldAllBe(x => x.TestName == "Invalid"),
                result => result.Results.ShouldAllBe(x => x.Output!.ErrorInfo!.Message!.Contains(StructViolation)),
                result => result.Results.Select(x => x.TestId).Distinct().Count().ShouldBe(2)
            ]);
    }

    [Test]
    public async Task Valid_Inferred_Matrix_Type_Still_Runs()
    {
        await RunTestsWithFilter(
            $"{Namespace}/MatrixInfersTypeConstraintTests/Valid*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2)
            ]);
    }

    [Test]
    public async Task Each_Invalid_Class_Row_Fails_Each_Test_Method()
    {
        // 3 invalid rows x 2 test methods.
        await RunTestsWithFilter(
            $"{Namespace}/SeveralInvalidClassRowsConstraintTests*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(6),
                result => result.ResultSummary.Counters.Failed.ShouldBe(6),
                result => result.Results.Count(x => x.TestName == "First").ShouldBe(3),
                result => result.Results.Count(x => x.TestName == "Second").ShouldBe(3),
                result => result.Results.Count(x => x.Output!.ErrorInfo!.Message!.Contains(StructViolation)).ShouldBe(4),
                result => result.Results.Count(x => x.Output!.ErrorInfo!.Message!.Contains(
                    "Type 'DayOfWeek' does not implement interface 'IEquatable`1' required for generic parameter 'T'")).ShouldBe(2),
                result => result.Results.Select(x => x.TestId).Distinct().Count().ShouldBe(6)
            ]);
    }

    [Test]
    public async Task Each_Invalid_Method_Row_Fails()
    {
        await RunTestsWithFilter(
            $"{Namespace}/SeveralInvalidMethodRowsConstraintTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                result => result.Results.Where(x => x.Outcome == "Failed").ShouldAllBe(x => x.TestName == "Run"),
                result => result.Results.Where(x => x.Outcome == "Failed").Select(x => x.TestId).Distinct().Count().ShouldBe(2)
            ]);
    }

    [Test]
    public async Task Invalid_Method_Row_Combined_With_A_Class_Row_Fails()
    {
        // The valid method row runs once, and the invalid one fails once: it is not also run
        // (and failed with a cast error) by the valid instantiation.
        await RunTestsWithFilter(
            $"{Namespace}/ClassAndMethodRowsConstraintTests*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single(x => x.Outcome == "Failed").TestName.ShouldBe("Run"),
                result => result.Results.Single(x => x.Outcome == "Failed").Output?.ErrorInfo?.Message.ShouldContain(
                    "Type 'String' does not satisfy the 'struct' constraint for generic parameter 'TValue'")
            ]);
    }

    [Test]
    public async Task Method_Constraint_Naming_A_Class_Type_Parameter_Uses_The_Class_Type_Argument()
    {
        // `where U : IEnumerable<T>` with T = int: List<int> satisfies it.
        await RunTestsWithFilter(
            $"{Namespace}/MethodConstraintNamesClassTypeParameterTests*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task Method_Constraint_Naming_A_Class_Type_Parameter_Fails_When_Broken()
    {
        // `where U : IEnumerable<T>` with T = int: List<string> breaks it, with the same message in both modes.
        await RunTestsWithFilter(
            $"{Namespace}/MethodConstraintNamesClassTypeParameterViolationTests*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single().Output?.ErrorInfo?.Message
                    .ShouldContain("Type 'List`1' does not implement interface 'IEnumerable`1' required for generic parameter 'U'")
            ]);
    }

    [Test]
    [Arguments("VariantInterfaceConstraintTests")]
    [Arguments("VariantInterfaceClassConstraintTests")]
    public async Task Variant_Interface_Satisfies_The_Constraint(string className)
    {
        await RunTestsWithFilter(
            $"{Namespace}/{className}*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task Broken_Constraint_On_An_Array_Of_A_Type_Parameter_Names_The_Interface()
    {
        // `where T : IEnumerable<U[]>` with T = List<string[]>, U = int: the same specific message in both modes.
        await RunTestsWithFilter(
            $"{Namespace}/ArrayElementConstraintViolationTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single().Output?.ErrorInfo?.Message
                    .ShouldContain("Type 'List`1' does not implement interface 'IEnumerable`1' required for generic parameter 'T'")
            ]);
    }

    [Test]
    public async Task Constraint_On_A_Nested_Type_Substitutes_The_Containing_Type()
    {
        // `where T : ConstraintOuter<U>.IOut<string>` with U = int, satisfied by the payload.
        await RunTestsWithFilter(
            $"{Namespace}/NestedTypeConstraintTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task Broken_Constraint_On_A_Nested_Type_Names_The_Interface()
    {
        // The same constraint with U = string: the payload does not implement ConstraintOuter<string>.IOut<string>.
        await RunTestsWithFilter(
            $"{Namespace}/NestedTypeConstraintViolationTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single().Output?.ErrorInfo?.Message
                    .ShouldContain("Type 'NestedConstraintPayload' does not implement interface 'IOut`1' required for generic parameter 'T'")
            ]);
    }

    [Test]
    public async Task Constraint_On_An_Array_Of_A_Type_Parameter_Substitutes_The_Element_Type()
    {
        // `where T : IEnumerable<U[]>` with T = List<int[]>, U = int.
        await RunTestsWithFilter(
            $"{Namespace}/ArrayElementConstraintTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task Array_Type_Argument_Is_Named_As_Reflection_Names_It()
    {
        await RunTestsWithFilter(
            $"{Namespace}/ArrayTypeArgumentConstraintTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single(x => x.Outcome == "Failed").Output?.ErrorInfo?.Message
                    .ShouldContain("Type 'Int32[]' does not satisfy the 'struct' constraint for generic parameter 'T'")
            ]);
    }

    [Test]
    public async Task Method_Row_Does_Not_Infer_A_Class_Type_That_Generate_Generic_Test_Supplies()
    {
        // [GenerateGenericTest(typeof(object))] with [Arguments(1)]: only the valid <object> test, no <int> violation.
        await RunTestsWithFilter(
            $"{Namespace}/SuppliedClassTypeMethodRowTests*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    [Arguments("TypeParameterConstraintTests", "Type 'String' does not satisfy the base type constraint 'Stream' for generic parameter 'T'")]
    [Arguments("NewConstraintTests", "Type arguments 'NoParameterlessConstructor' do not satisfy the generic constraints of type 'NewConstraintTests`1'")]
    [Arguments("NullableStructConstraintTests", "Type 'Nullable`1' does not satisfy the 'struct' constraint for generic parameter 'T'")]
    [Arguments("NullableInterfaceConstraintTests", "Type 'Nullable`1' does not implement interface 'IComparable' required for generic parameter 'T'")]
    public async Task Other_Constraint_Kinds_Fail_With_The_Same_Message(string className, string expectedMessage)
    {
        await RunTestsWithFilter(
            $"{Namespace}/{className}*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single(x => x.Outcome == "Failed").TestName.ShouldBe("Run"),
                result => result.Results.Single(x => x.Outcome == "Failed").Output?.ErrorInfo?.Message.ShouldContain(expectedMessage)
            ]);
    }

    [Test]
    [Arguments("NullMethodRowInfersClassConstraintTests", "Type 'Object' does not satisfy the 'struct' constraint for generic parameter 'T'")]
    [Arguments("TypeValueMethodRowInfersClassConstraintTests", "Type 'Type' does not satisfy the 'struct' constraint for generic parameter 'T'")]
    public async Task Method_Row_Infers_The_Class_Type_As_The_Source_Generator_Does(string className, string expectedMessage)
    {
        // A null value infers object and a Type value infers System.Type, in both modes.
        await RunTestsWithFilter(
            $"{Namespace}/{className}*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single().TestName.ShouldBe("Run"),
                result => result.Results.Single().Output?.ErrorInfo?.Message.ShouldContain(expectedMessage)
            ]);
    }
}
