using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A method-level <c>[GenerateGenericTest]</c> with no type arguments, or the wrong number of them, fails as a
/// test of its own in both modes, with the same message (#7222).
/// </summary>
public class Issue7222Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    private const string ClassFilter = "/*/TUnit.TestProject.Bugs._7222/GenerateGenericTestArityTests";

    [Test]
    public async Task Class_Skip_Is_Preserved_On_Arity_Failures()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7222/SkippedClassGenerateGenericTestArityTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(2)
            ]);
    }

    [Test]
    public async Task Explicit_Arity_Failures_Are_Excluded_Beside_Normal_Tests()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7222/ExplicitGenerateGenericTestArityTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.Results.Single().TestName.ShouldBe("NormalTest")
            ]);
    }

    [Test]
    [Arguments("EmptyTypeArguments")]
    [Arguments("WrongTypeArgumentCount")]
    public async Task Explicit_Arity_Failure_Runs_When_Selected(string methodName)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7222/ExplicitGenerateGenericTestArityTests/{methodName}",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single().Output?.ErrorInfo?.Message.ShouldContain("[GenerateGenericTest]")
            ]);
    }

    [Test]
    [Arguments("TooManyTypeArguments", "[GenerateGenericTest] on method 'TooManyTypeArguments' provides 2 type argument(s) but method requires 1. Provided: [Int32, String]")]
    [Arguments("NoTypeArguments", "[GenerateGenericTest] on method 'NoTypeArguments' has no type arguments")]
    [Arguments("TooFewTypeArguments", "[GenerateGenericTest] on method 'TooFewTypeArguments' provides 1 type argument(s) but method requires 2. Provided: [Int32]")]
    public async Task Wrongly_Sized_GenerateGenericTest_Fails(string methodName, string message)
    {
        await RunTestsWithFilter(
            $"{ClassFilter}/{methodName}*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single().Output?.ErrorInfo?.Message.ShouldContain(message)
            ]);
    }

    [Test]
    public async Task Wrongly_Sized_GenerateGenericTest_Fails_Beside_Valid_Ones()
    {
        await RunTestsWithFilter(
            $"{ClassFilter}/MixedArity*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single(x => x.Outcome == "Failed").TestName.ShouldBe("MixedArity"),
                result => result.Results.Single(x => x.Outcome == "Failed").Output?.ErrorInfo?.Message
                    .ShouldContain("[GenerateGenericTest] on method 'MixedArity' provides 2 type argument(s) but method requires 1. Provided: [Int32, String]")
            ]);
    }

    [Test]
    public async Task Array_Type_Argument_Runs()
    {
        await RunTestsWithFilter(
            $"{ClassFilter}/ArrayTypeArgument*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task Wrongly_Sized_GenerateGenericTest_Fails_On_Each_Concrete_Class()
    {
        // Two concrete classes, each running MixedArity<string> and failing the wrongly sized one
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7222/GenericClassGenerateGenericTestArityTests*/MixedArity*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2)
            ]);
    }

    [Test]
    public async Task Wrongly_Sized_GenerateGenericTest_Fails_When_None_Is_Valid_On_A_Generic_Class()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7222/OnlyWronglySizedOnGenericClassTests*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single().Output?.ErrorInfo?.Message
                    .ShouldContain("[GenerateGenericTest] on method 'NoTypeArguments' has no type arguments")
            ]);
    }

    [Test]
    public async Task Arity_And_Constraint_Failures_Both_Fail()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7222/ArityAndConstraintOrderTests*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2)
            ]);
    }

    [Test]
    public async Task Wrongly_Sized_GenerateGenericTest_Fails_Once_Beside_Class_Rows()
    {
        // Two class rows run MixedArity<string>; the wrongly sized one fails once, not once per class row
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7222/ClassRowsGenerateGenericTestArityTests*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task NonGeneric_Class_Instance_Source_Infers_Method_Types()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7222/NonGenericInstanceSourceTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2)
            ]);
    }

    [Test]
    [Arguments("ExplicitClassInstanceSourcesTests*", 2)]
    [Arguments("ExplicitClassMethodSourceCollisionTests*", 2)]
    [Arguments("ClassRowMethodSourceCollisionTests*", 2)]
    [Arguments("ExplicitMethodInstanceSourceTests", 1)]
    [Arguments("ClosedClassParameterInferredSourceTests*", 1)]
    [Arguments("ExplicitClassParameterInferredSourceTests*", 1)]
    public async Task Explicit_And_Parameter_Inference_Preserve_Instance_Sources(string className, int count)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7222/{className}/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(count),
                result => result.ResultSummary.Counters.Passed.ShouldBe(count)
            ]);
    }

    [Test]
    [Arguments("MixedClassRowSourcesTests*")]
    [Arguments("MixedExplicitClassSourcesTests*")]
    public async Task Mixed_Inferred_Sources_Preserve_Both_Declaration_Orders(string className)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7222/{className}/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.Results.Count(x => x.TestName.StartsWith("InstanceFirst<string>", StringComparison.Ordinal)).ShouldBe(2),
                result => result.Results.Count(x => x.TestName.StartsWith("StaticFirst<string>", StringComparison.Ordinal)).ShouldBe(2)
            ]);
    }

    [Test]
    [Arguments("ClosedClassUninferredInstanceSourceTests*")]
    [Arguments("ClosedClassRowUninferredInstanceSourceTests*")]
    public async Task Closed_Class_Uninferred_Instance_Source_Is_Reported(string className)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7222/{className}/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task Generic_Class_Instance_Source_Infers_Method_Types()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7222/GenericInstanceSourceTests*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(5),
                result => result.ResultSummary.Counters.Passed.ShouldBe(3),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                result => result.Results.Where(x => x.Outcome == "Failed")
                    .ShouldAllBe(x => x.Output != null && x.Output.ErrorInfo != null
                        && x.Output.ErrorInfo.Message != null && x.Output.ErrorInfo.Message.Contains(
                        "Type 'String' does not satisfy the 'struct' constraint for generic parameter 'T'"))
            ]);
    }

    [Test]
    public async Task Failing_Instance_Source_Rows_Keep_The_Class_Constructor_Arguments()
    {
        // Each row of the instance data source fails on its own. Without the class data sources, source-generated
        // mode could not build the instance that the rows come from and reported one constructor error instead.
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7222/InstanceSourceConstraintViolationTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                result => result.Results.ShouldAllBe(x => x.Output != null
                    && x.Output.ErrorInfo != null
                    && x.Output.ErrorInfo.Message != null
                    && x.Output.ErrorInfo.Message.Contains("Type 'String' does not satisfy the 'struct' constraint for generic parameter 'T'"))
            ]);
    }

    [Test]
    public async Task Constructor_Argument_Class_Runs_Valid_And_Fails_Wrongly_Sized()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7222/ConstructorArgumentGenerateGenericTestArityTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1)
            ]);
    }

    [Test]
    [Arguments("NullTypeArgument", "[GenerateGenericTest] on method 'NullTypeArgument' provides type argument 'null', which cannot be a generic type argument")]
    [Arguments("NullBesideType", "[GenerateGenericTest] on method 'NullBesideType' provides 2 type argument(s) but method requires 1. Provided: [Int32, null]")]
    public async Task Null_Type_Argument_Fails(string methodName, string message)
    {
        // Reflection mode used to throw a NullReferenceException out of discovery; source-generated mode dropped the
        // null and ran NullBesideType<int>.
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7222/NullTypeArgumentGenerateGenericTestTests/{methodName}*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single().Output?.ErrorInfo?.Message.ShouldContain(message)
            ]);
    }

    [Test]
    [Arguments("BroadSourceOnGenericClassRowTests")]
    [Arguments("BroadSourceOnGenericClassTypeTests")]
    public async Task Broad_Source_On_Generic_Class_Defers_To_GenerateGenericTest(string className)
    {
        // Only the arity failure, not a second one for the object data source's declared type
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7222/{className}*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single().Output?.ErrorInfo?.Message
                    .ShouldContain("[GenerateGenericTest] on method 'EmptyGenerateGenericTest' has no type arguments")
            ]);
    }

    [Test]
    public async Task Deferred_Broad_Source_Joins_Method_Row_Instantiation_On_Class_Row()
    {
        // The [Arguments] row creates Test<int> first; the object source's row joins it and both run
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7222/BroadSourceJoinsClassRowTests*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2)
            ]);
    }

    [Test]
    public async Task Deferred_Source_Joins_Only_Its_Own_Concrete_Class()
    {
        // int class: Test<int> runs the [Arguments] row and the deferred object row. object class: Test<int>
        // runs the [Arguments] row and Test<object> the object row, which is not deferred there.
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7222/DeferredSourcePerConcreteClassTests*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4)
            ]);
    }

    [Test]
    public async Task Constraint_Violation_Without_GenerateGenericTest_Fails_Once_Per_Class_Row()
    {
        // Without a method-level [GenerateGenericTest], reflection mode keeps the class rows on the failing
        // entry, so source-generated mode must not drop them: two class rows give two failures in both modes.
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7222/ClassRowsConstraintViolationTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2)
            ]);
    }
}
