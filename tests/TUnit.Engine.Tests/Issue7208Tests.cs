using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A class-level [MethodDataSource] that names a member of the generic test class itself is read on the
/// closed class, in both modes. A class whose types nothing infers fails on its own, without failing the
/// rest of the assembly in reflection mode (#7208).
/// </summary>
public class Issue7208Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("OwnClassMethodDataSource")]
    [Arguments("OwnClassPropertyDataSource")]
    [Arguments("OwnClassTypeParameterMethodDataSourceWithGenerateGenericTest")]
    public async Task Instance_Is_Built_From_The_Own_Class_Member(string className)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7208/{className}/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Uninferable_Own_Class_Member_Fails_Only_Its_Class()
    {
        // Source-gen emits no test for this class (#7333). Reflection reports one failed test for it, and
        // the rest of the namespace still runs.
        var reflection = testMode == TestMode.Reflection;

        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7208/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe(reflection ? "Failed" : "Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(reflection ? 4 : 3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(3),
                result => result.ResultSummary.Counters.Failed.ShouldBe(reflection ? 1 : 0),
                result => result.Results
                    .Where(x => x.Outcome == "Failed")
                    .Select(x => x.Output?.ErrorInfo?.Message)
                    .ShouldAllBe(message => message != null && message.Contains("names a member of the class itself"))
            ]);
    }
}
