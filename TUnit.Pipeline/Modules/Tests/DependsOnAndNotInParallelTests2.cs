using FluentAssertions;
using ModularPipelines.Context;
using Polly.Retry;

namespace TUnit.Pipeline.Modules.Tests;

public class NotInParallelWithDependsOnTests : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/NotInParallelWithDependsOnTests/*",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(3),
                result => result.Passed.Should().Be(0),
                result => result.Failed.Should().Be(3),
                result => result.Skipped.Should().Be(0),

            ], cancellationToken);
    }
}