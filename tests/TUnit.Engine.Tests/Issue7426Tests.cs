using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7426Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("InheritedRuntimeSource")]
    [Arguments("DirectRuntimeSource")]
    [Arguments("InheritedExplicitSource")]
    [Arguments("InheritedArguments")]
    public async Task Constructor_Data_Is_Inherited(string className)
    {
        // Runtime-only object rows require explicit type arguments in source-generated mode.
        var cannotInfer = testMode == TestMode.AOT && className is "InheritedRuntimeSource" or "DirectRuntimeSource";
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7426/{className}/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(cannotInfer ? 0 : 1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(cannotInfer ? 1 : 0),
                result => result.Results.Where(x => x.Outcome == "Failed")
                    .Select(x => x.Output?.ErrorInfo?.Message)
                    .ShouldAllBe(message => message != null && message.Contains("class type arguments cannot be inferred at compile time"))
            ]);
    }
}
