using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class RegistrationFailure7525Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("RegistrationFailureTests", 3, 1)]
    [Arguments("ParallelRegistrationFailureTests", 8, 6)]
    public async Task RegistrationFailureDoesNotAbortRequest(string className, int total, int passed)
    {
        await RunTestsWithFilter($"/*/TUnit.TestProject.Bugs._7525/{className}/*",
        [
            result => result.ResultSummary.Counters.Total.ShouldBe(total),
            result => result.ResultSummary.Counters.Failed.ShouldBe(2),
            result => result.ResultSummary.Counters.Passed.ShouldBe(passed),
            result => result.Results.Where(x => x.Outcome == "Failed")
                .ShouldAllBe(x => x.Output!.ErrorInfo!.Message!.Contains("Registration ")
                    && x.Output.ErrorInfo.Message.Contains("failed (#7525)"))
        ]);
    }
    [Test]
    public async Task RegistrationFailureDisposesExistingArguments()
    {
        await RunTestsWithFilter("/*/TUnit.TestProject.Bugs._7525/RegistrationDisposalTests/*",
        [
            result => result.ResultSummary.Counters.Total.ShouldBe(2),
            result => result.ResultSummary.Counters.Failed.ShouldBe(1),
            result => result.ResultSummary.Counters.Passed.ShouldBe(1),
            result => result.Results.Single(x => x.Outcome == "Failed").Output!.ErrorInfo!.Message!
                .ShouldContain("Registration receiver failed (#7525)")
        ]);
    }
}
