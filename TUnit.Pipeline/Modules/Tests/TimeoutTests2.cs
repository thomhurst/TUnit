using FluentAssertions;
using ModularPipelines.Context;

namespace TUnit.Pipeline.Modules.Tests;

public class TimeoutTests2 : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/TimeoutCancellationTokenTests/InheritedTimeoutAttribute",
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