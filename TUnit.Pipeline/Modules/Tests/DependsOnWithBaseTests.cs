using FluentAssertions;
using ModularPipelines.Context;
using Polly.Retry;

namespace TUnit.Pipeline.Modules.Tests;

public class DependsOnWithBaseTests : TestModule
{
    protected override AsyncRetryPolicy<TestResult?> RetryPolicy => CreateRetryPolicy(3);
    
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/DependsOnWithBaseTests/*",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(2),
                result => result.Passed.Should().Be(2),
                result => result.Failed.Should().Be(0),
                result => result.Skipped.Should().Be(0),
                result =>
                {
                    // var baseTest = result.TrxReport.UnitTestResults.First(x => x.TestName!.StartsWith("SubTypeTest")).StartTime!.Value;
                    // var subTypeTestStart = result.TrxReport.UnitTestResults.First(x => x.TestName!.StartsWith("BaseTest")).StartTime!.Value;
                    //
                    // subTypeTestStart.Should().BeOnOrAfter(baseTest.AddSeconds(4.9));
                }
            ], cancellationToken);
    }
}