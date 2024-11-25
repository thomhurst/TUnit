using FluentAssertions;

namespace TUnit.Engine.Tests;

public class JsonOutputTests : TestModule
{
    [Test]
    [Ignore("TODO")]
    public async Task Test()
    {
        var prefix = "myprefix_";
        var filename = Guid.NewGuid().ToString("N");

        await RunTestsWithFilter(
            "/*/*/PassFailTests/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Failed"),
                result => result.ResultSummary.Counters.Total.Should().Be(172),
                result => result.ResultSummary.Counters.Passed.Should().Be(86),
                result => result.ResultSummary.Counters.Failed.Should().Be(86),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0)
            ],
            new RunOptions
            {
                AdditionalArguments = [ "--output-json", "--output-json-prefix", prefix, "--output-json-filename", filename ],
            });

        // var file = await context
        //     .Git()
        //     .RootDirectory
        //     .FindFile(x => x.Name == $"{prefix}{filename}.json")
        //     .AssertExists()
        //     .ReadAsync(cancellationToken);
        //
        // file.Should().NotBeEmpty();
        //
        // return testResult;
    }
}