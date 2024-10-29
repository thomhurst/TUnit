using FluentAssertions;
using ModularPipelines.Context;

namespace TUnit.Pipeline.Modules.Tests;

public class NestedBaseTests : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/NestedBaseTests/*",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(1),
                result => result.Passed.Should().Be(1),
                result => result.Failed.Should().Be(0),
                result => result.Skipped.Should().Be(0)
            ], cancellationToken);
    }
}