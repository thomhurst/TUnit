using FluentAssertions;
using ModularPipelines.Context;

namespace TUnit.Testing.Pipeline.Modules;

public class CustomerFilteringTests4 : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/CustomFilteringTests/*[one=other]",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(0),
                result => result.Passed.Should().Be(0),
                result => result.Failed.Should().Be(0),
                result => result.Skipped.Should().Be(0)
            ], cancellationToken);
    }
}