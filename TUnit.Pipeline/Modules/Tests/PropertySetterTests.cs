using FluentAssertions;
using ModularPipelines.Context;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;

namespace TUnit.Pipeline.Modules.Tests;

public class PropertySetterTests : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/PropertySetterTests/*",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(1),
                result => result.Passed.Should().Be(1),
                result => result.Failed.Should().Be(0),
                result => result.Skipped.Should().Be(0),
                _ => context.Git().RootDirectory.FindFile(x => x.Name == "PropertySetterTests_CapturedOutput.txt").AssertExists().ReadAsync(cancellationToken).Result.Should().Contain(
                    """
                    Initializing Static Property
                    Before Test Session
                    Before Assembly
                    Before Class
                    Initializing Property
                    Initializing Property
                    Initializing Property
                    Initializing Property
                    Running Test
                    StaticInnerModel { IsInitialized = True, Foo = Bar }
                    Disposing Property
                    """
                    ),
                _ => context.Git().RootDirectory.FindFile(x => x.Name == "StaticProperty_IAsyncDisposable.txt").AssertExists()
            ], cancellationToken);
    }
}