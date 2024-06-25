using FluentAssertions;
using ModularPipelines.Context;

namespace TUnit.Pipeline.Modules.Tests;

public class RetryTests : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/RetryTests/*",
            [
                result => result.Successful.Should().BeFalse(),
                result => result.Total.Should().Be(4),
                result => result.Passed.Should().Be(1),
                result => result.Failed.Should().Be(3),
                result => result.Skipped.Should().Be(0)
            ], cancellationToken);
    }
}