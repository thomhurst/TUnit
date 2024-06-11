using FluentAssertions;
using ModularPipelines.Context;

namespace TUnit.Pipeline.Modules.Tests;

public class PriorityFilteringTests7 : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/PriorityFilteringTests/*[*=Medium]",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(2),
                result => result.Passed.Should().Be(2),
                result => result.Failed.Should().Be(0),
                result => result.Skipped.Should().Be(0)
            ], cancellationToken);
    }
}