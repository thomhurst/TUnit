using FluentAssertions;
using ModularPipelines.Context;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;

namespace TUnit.Pipeline.Modules.Tests;

public class TestDiscoveryAfterHookTests : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context,
            "/*/*/TestDiscoveryAfterTests/*",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(1),
                result => result.Passed.Should().Be(1),
                result => result.Failed.Should().Be(0),
                result => result.Skipped.Should().Be(0),
                _ => context.Git().RootDirectory.FindFile(x => x.Name.EndsWith("TestDiscoveryAfterTests.txt")).AssertExists().Delete()
            ], cancellationToken);
    }
}
