using FluentAssertions;
using ModularPipelines.Context;
using Polly.Retry;

namespace TUnit.Pipeline.Modules.Tests;

public class TimeoutTests1 : TestModule
{
    protected override AsyncRetryPolicy<TestResult?> RetryPolicy => CreateRetryPolicy(3);
    
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/TimeoutCancellationTokenTests/BasicTest",
            [
                result => result.Successful.Should().BeFalse(),
                result => result.Total.Should().Be(1),
                result => result.Passed.Should().Be(0),
                result => result.Failed.Should().Be(1),
                result => result.Skipped.Should().Be(0),
                result => result.TrxReport.UnitTestResults[0].Duration.Should().BeLessThan(TimeSpan.FromMinutes(1)),
                result => result.TrxReport.UnitTestResults[0].Duration.Should().BeGreaterThan(TimeSpan.FromSeconds(4)),
            ], cancellationToken);
    }
}