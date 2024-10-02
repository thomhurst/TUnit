using FluentAssertions;
using ModularPipelines.Context;
using ModularPipelines.Enums;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;

namespace TUnit.Pipeline.Modules.Tests;

public class JsonOutputTests : TestModule
{
    protected override Task<SkipDecision> ShouldSkip(IPipelineContext context)
    {
        return Task.FromResult(SkipDecision.Skip("TODO"));
    }

    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var prefix = "myprefix_";
        var filename = Guid.NewGuid().ToString("N");

        var testResult = await RunTestsWithFilter(context, 
            "/*/*/PassFailTests/*",
            [
                result => result.Successful.Should().BeFalse(),
                result => result.Total.Should().Be(172),
                result => result.Passed.Should().Be(86),
                result => result.Failed.Should().Be(86),
                result => result.Skipped.Should().Be(0)
            ],
            new RunOptions
            {
                AdditionalArguments = [ "--output-json", "--output-json-prefix", prefix, "--output-json-filename", filename ],
                CommandLogging = CommandLogging.Input | CommandLogging.Duration | CommandLogging.ExitCode | CommandLogging.Output
            },
            cancellationToken);

        var file = await context
            .Git()
            .RootDirectory
            .FindFile(x => x.Name == $"{prefix}{filename}.json")
            .AssertExists()
            .ReadAsync(cancellationToken);
        
        file.Should().NotBeEmpty();
        
        return testResult;
    }
}