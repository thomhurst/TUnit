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
}
