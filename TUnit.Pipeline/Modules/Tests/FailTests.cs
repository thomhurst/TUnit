using FluentAssertions;
using ModularPipelines.Context;
using ModularPipelines.Enums;

namespace TUnit.Pipeline.Modules.Tests;

public class FailTests : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/PassFailTests/*[Category=Fail]",
            [
                result => result.Successful.Should().BeFalse(),
                result => result.Total.Should().Be(86),
                result => result.Passed.Should().Be(0),
                result => result.Failed.Should().Be(86),
                result => result.Skipped.Should().Be(0)
            ], new RunOptions { CommandLogging = CommandLogging.Input | CommandLogging.Duration | CommandLogging.ExitCode | CommandLogging.Error }, cancellationToken);
    }
}