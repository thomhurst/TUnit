using CliWrap;
using CliWrap.Buffered;
using Shouldly;

namespace TUnit.Engine.Tests;

public class FSharp
{
    [Test]
    public async Task Test()
    {
        var runOptions = new RunOptions();
        
        var testProject = Sourcy.DotNet.Projects.TUnit_TestProject_FSharp;
        var guid = Guid.NewGuid().ToString("N");
        var trxFilename = guid + ".trx";
        var command = Cli.Wrap("dotnet")
            .WithArguments(
                [
                    "run",
                    "--no-build",
                    "-f", "net9.0",
                    "--configuration", "Release",
                    "--report-trx", "--report-trx-filename", trxFilename,
                    "--diagnostic-verbosity", "Debug",
                    "--diagnostic", "--diagnostic-output-fileprefix", $"log_{GetType().Name}_", 
                    "--timeout", "5m",
                    // "--hangdump", "--hangdump-filename", $"hangdump.tests-{guid}.txt", "--hangdump-timeout", "3m",

                    ..runOptions.AdditionalArguments
                ]
            )
            .WithWorkingDirectory(testProject.DirectoryName!)
            .WithValidation(CommandResultValidation.None);
        
        var result = await command.ExecuteBufferedAsync();

        await TrxAsserter.AssertTrx(command, result, 
            
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBeGreaterThanOrEqualTo(9),
                result => result.ResultSummary.Counters.Passed.ShouldBeGreaterThanOrEqualTo(9),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ],
            
            trxFilename);
    }
}