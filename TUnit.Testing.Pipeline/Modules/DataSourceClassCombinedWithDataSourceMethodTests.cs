using FluentAssertions;
using ModularPipelines.Context;

namespace TUnit.Testing.Pipeline.Modules;

public class DataSourceClassCombinedWithDataSourceMethodTests : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/DataSourceClassCombinedWithDataSourceMethod/*",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(9),
                result => result.Passed.Should().Be(9),
                result => result.Failed.Should().Be(0),
                result => result.Skipped.Should().Be(0)
            ], cancellationToken);
    }
}