using FluentAssertions;
using ModularPipelines.Context;

namespace TUnit.Pipeline.Modules.Tests;

public class CustomRetryTests : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/CustomRetryTests/*",
            [
                result => result.Successful.Should().BeFalse(),
                result => result.Total.Should().Be(5),
                result => result.Passed.Should().Be(1),
                result => result.Failed.Should().Be(4),
                result => result.Skipped.Should().Be(0)
            ], cancellationToken);
    }
}