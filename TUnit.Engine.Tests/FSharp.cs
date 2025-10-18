using CliWrap;
using CliWrap.Buffered;
using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class FSharp
{
    [Test]
    public async Task Test()
    {
        var runOptions = new RunOptions();

        var testProject = Sourcy.DotNet.Projects.TUnit_TestProject_FSharp;

        // Build path to the executable
        var binPath = Path.Combine(testProject.DirectoryName!, "bin", "Release", "net10.0");

        // Find the executable file (with or without .exe extension)
        var files = new DirectoryInfo(binPath).EnumerateFiles("*", SearchOption.TopDirectoryOnly).ToArray();
        var executable = files.FirstOrDefault(x => x.Name == "TUnit.TestProject.FSharp")
                         ?? files.First(x => x.Name == "TUnit.TestProject.FSharp.exe");

        var guid = Guid.NewGuid().ToString("N");
        var trxFilename = guid + ".trx";
        var command = Cli.Wrap(executable.FullName)
            .WithArguments(
                [
                    "--report-trx", "--report-trx-filename", trxFilename,
                    "--diagnostic-verbosity", "Debug",
                    "--diagnostic", "--diagnostic-file-prefix", $"log_{GetType().Name}_",
                    "--timeout", "5m",
                    // "--hangdump", "--hangdump-filename", $"hangdump.tests-{guid}.dmp", "--hangdump-timeout", "3m",

                    ..runOptions.AdditionalArguments
                ]
            )
            .WithValidation(CommandResultValidation.None);

        var result = await command.ExecuteBufferedAsync();

        await TrxAsserter.AssertTrx(TestMode.Reflection, command, result,

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
