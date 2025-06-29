using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class JsonOutputTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Skip("TODO")]
    public async Task Test()
    {
        var prefix = "myprefix_";
        var filename = Guid.NewGuid().ToString("N");

        await RunTestsWithFilter(
            "/*/*/PassFailTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(172),
                result => result.ResultSummary.Counters.Passed.ShouldBe(86),
                result => result.ResultSummary.Counters.Failed.ShouldBe(86),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ],
            new RunOptions
            {
                AdditionalArguments = ["--output-json", "--output-json-prefix", prefix, "--output-json-filename", filename],
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
