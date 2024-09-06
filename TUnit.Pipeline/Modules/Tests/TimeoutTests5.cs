using FluentAssertions;
using ModularPipelines.Context;

namespace TUnit.Pipeline.Modules.Tests;

public class TimeoutTests5 : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var start = DateTime.UtcNow;
        return await RunTestsWithFilter(context, 
            "/*/*/TimeoutCancellationTokenTests/MatrixTest",
            [
                result => result.Successful.Should().BeFalse(),
                result => result.Total.Should().Be(3),
                result => result.Passed.Should().Be(0),
                result => result.Failed.Should().Be(3),
                result => result.Skipped.Should().Be(0),
                result => result.TrxReport.UnitTestResults[0].Duration.Should().BeLessThan(TimeSpan.FromMinutes(1)),
                result => result.TrxReport.UnitTestResults[0].Duration.Should().BeGreaterThan(TimeSpan.FromSeconds(30)),
            ], cancellationToken);
    }
}