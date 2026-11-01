using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// End-to-end regression tests for https://github.com/thomhurst/TUnit/issues/7207.
/// Overloaded [Test] methods share a class, method name and filter path, so a filtered run
/// must not collapse them into one test.
/// </summary>
public class Bug7207OverloadedTestMethodsTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("GenericClassOverloadTests*")]
    [Arguments("NonGenericClassOverloadTests")]
    public async Task Both_Overloads_Run(string className)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7207/{className}/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.Results.Select(x => x.TestName).OrderBy(x => x, StringComparer.Ordinal)
                    .ShouldBe(["Overloaded(a, 3)", "Overloaded(a, b)"])
            ]);
    }

    [Test]
    public async Task Overloads_With_Erased_Parameter_Types_Both_Run()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7207/GenericArrayOverloadTests*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Filtering_A_Dependent_Pulls_In_Every_Overload_It_Depends_On()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7207/DependsOnOverloadTests/Dependent",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(3),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.Results.Select(x => x.TestName).OrderBy(x => x, StringComparer.Ordinal)
                    .ShouldBe(["Dependent", "Overloaded(a, 3)", "Overloaded(a, b)"])
            ]);
    }
}
