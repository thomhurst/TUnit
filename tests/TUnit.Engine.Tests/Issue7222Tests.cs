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
    public async Task Failing_Instance_Source_Rows_Keep_The_Class_Constructor_Arguments()
    {
        // Each row of the instance data source fails on its own. Without the class data sources, source-generated
        // mode could not build the instance that the rows come from and reported one constructor error instead.
        // The message differs by mode: source-generated mode cannot infer the type from [InstanceMethodDataSource].
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7222/InstanceSourceConstraintViolationTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                result => result.Results.ShouldAllBe(x => x.Output != null
                    && x.Output.ErrorInfo != null
                    && x.Output.ErrorInfo.Message != null
                    && !x.Output.ErrorInfo.Message.Contains("Index was outside the bounds of the array"))
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
}
