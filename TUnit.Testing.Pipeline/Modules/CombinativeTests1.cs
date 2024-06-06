using FluentAssertions;
using ModularPipelines.Context;

namespace TUnit.Testing.Pipeline.Modules;

public class CombinativeTests1 : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/CombinativeTests/CombinativeTest_One",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(24, "Total"),
                result => result.Passed.Should().Be(24, "Passed"),
                result => result.Failed.Should().Be(0, "Failed"),
                result => result.Skipped.Should().Be(0, "Skipped")
            ], cancellationToken);
    }
}