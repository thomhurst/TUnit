using FluentAssertions;
using ModularPipelines.Context;

namespace TUnit.Pipeline.Modules.Tests;

public class CategoryTests : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        await RunTestsWithFilter(context, 
            "/*/*/CategoryTests/*[Category=A]",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(2),
                result => result.Passed.Should().Be(2),
                result => result.Failed.Should().Be(0),
                result => result.Skipped.Should().Be(0)
            ], cancellationToken);
        
        await RunTestsWithFilter(context, 
            "/*/*/CategoryTests/*[Category=B]",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(2),
                result => result.Passed.Should().Be(2),
                result => result.Failed.Should().Be(0),
                result => result.Skipped.Should().Be(0)
            ], cancellationToken);
        
        await RunTestsWithFilter(context, 
            "/*/*/CategoryTests/*[(Category=A)&(Category=B)]",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(1),
                result => result.Passed.Should().Be(1),
                result => result.Failed.Should().Be(0),
                result => result.Skipped.Should().Be(0)
            ], cancellationToken);

        return null;
    }
}