using FluentAssertions;
using ModularPipelines.Context;

namespace TUnit.Pipeline.Modules.Tests;

public class SkipTests : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/SkipTests/*",
            [
                result => result.Successful.Should().BeFalse(),
                result => result.Total.Should().Be(1),
                result => result.Passed.Should().Be(0),
                result => result.Failed.Should().Be(0),
                result => result.Skipped.Should().Be(1)
            ], cancellationToken);
    }
}