using FluentAssertions;
using ModularPipelines.Context;

namespace TUnit.Pipeline.Modules.Tests;

public class FailTests : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/PassFailTests/*[Category=Fail]",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(326),
                result => result.Passed.Should().Be(326),
                result => result.Failed.Should().Be(0),
                result => result.Skipped.Should().Be(0)
            ], cancellationToken);
    }
}