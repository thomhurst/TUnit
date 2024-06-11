using FluentAssertions;
using ModularPipelines.Context;
using ModularPipelines.Enums;

namespace TUnit.Pipeline.Modules.Tests;

public class JsonOutputTests : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var prefix = "myprefix_";
        var filename = Guid.NewGuid().ToString("N");

        var testResult = await RunTestsWithFilter(context, 
            "/*/*/PassFailTests/*",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(172),
                result => result.Passed.Should().Be(86),
                result => result.Failed.Should().Be(86),
                result => result.Skipped.Should().Be(0)
            ],
            new RunOptions
            {
                AdditionalArguments = [ "--output-json", "--output-json-prefix", prefix, "--output-json-filename", filename ],
                CommandLogging = CommandLogging.Input | CommandLogging.Duration | CommandLogging.ExitCode | CommandLogging.Error
            },
            cancellationToken);

        (await File.ReadAllLinesAsync(Path.Combine(Environment.CurrentDirectory, $"{prefix}{filename}.json"), cancellationToken))
            .Should()
            .NotBeEmpty();
        
        return testResult;
    }
}