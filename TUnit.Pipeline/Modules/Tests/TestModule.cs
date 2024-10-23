using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet;
using ModularPipelines.DotNet.Enums;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.DotNet.Parsers.Trx;
using ModularPipelines.Enums;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules.Tests;

[NotInParallel("Unit Tests")]
[ParallelLimiter<ProcessorParallelLimit>]
[DependsOn<PublishAOTModule>]
[DependsOn<PublishSingleFileModule>]
public abstract class TestModule : Module<TestResult>
{
    protected Task<TestResult?> RunTestsWithFilter(IPipelineContext context, string filter,
        List<Action<TestResult>> assertions,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression(nameof(assertions))] string assertionExpression = "")
    {
        return RunTestsWithFilter(context, filter, assertions, new RunOptions(), cancellationToken, assertionExpression);
    }

    protected async Task<TestResult?> RunTestsWithFilter(IPipelineContext context, string filter,
        List<Action<TestResult>> assertions, RunOptions runOptions, CancellationToken cancellationToken = default,
        [CallerArgumentExpression(nameof(assertions))] string assertionExpression = "")
    {
        await SubModule("WithoutAot", async () =>
        {
            await RunWithoutAot(context, filter, assertions, runOptions, cancellationToken, assertionExpression);
        });

        await SubModule("Aot", async () =>
        {
            await RunWithAot(context, filter, assertions, runOptions, cancellationToken, assertionExpression);
        });

        await SubModule("SingleFile", async () =>
        {
            await RunWithSingleFile(context, filter, assertions, runOptions, cancellationToken, assertionExpression);
        });
        
        return null;
    }

    private static async Task RunWithoutAot(IPipelineContext context, string filter,
        List<Action<TestResult>> assertions, RunOptions runOptions,
        CancellationToken cancellationToken, string assertionExpression)
    {
        var trxFilename = Guid.NewGuid().ToString("N") + ".trx";
        
        var result = await context.DotNet().Run(new DotNetRunOptions
        {
            WorkingDirectory = context.Git().RootDirectory.GetFolder("TUnit.TestProject"),
            Configuration = Configuration.Release,
            NoBuild = true,
            ThrowOnNonZeroExitCode = false,
            CommandLogging = CommandLogging.None,
            Arguments =
            [
                "-f", "net8.0",
                "--treenode-filter", filter, 
                "--report-trx", "--report-trx-filename", trxFilename,
                // "--diagnostic", "--diagnostic-output-fileprefix", $"log_{GetType().Name}", 
                "--timeout", "5m",
                ..runOptions.AdditionalArguments
            ]
        }, cancellationToken);

        if (result.StandardError.Contains("System.ComponentModel.Win32Exception"))
        {
            throw new Exception("Unknown error running tests");
        }

        await AssertTrx(context, result, assertions, cancellationToken, trxFilename, assertionExpression);
    }
    
    private static async Task RunWithAot(IPipelineContext context, string filter, List<Action<TestResult>> assertions,
        RunOptions runOptions,
        CancellationToken cancellationToken, string assertionExpression)
    {
        var files = context.Git().RootDirectory
            .AssertExists()
            .FindFolder(x => x.Name == "TESTPROJECT_AOT")
            .AssertExists()
            .GetFiles(_ => true)
            .ToArray();

        var aotApp = files.FirstOrDefault(x => x.Name == "TUnit.TestProject") 
                     ?? files.First(x => x.Name == "TUnit.TestProject.exe");
        
        var trxFilename = Guid.NewGuid().ToString("N") + ".trx";
        
        var result = await context.Command.ExecuteCommandLineTool(new CommandLineToolOptions(aotApp)
        {
            ThrowOnNonZeroExitCode = false,
            CommandLogging = CommandLogging.None,
            Arguments =
            [
                "--treenode-filter", filter, 
                "--report-trx", "--report-trx-filename", trxFilename,
                // "--diagnostic", "--diagnostic-output-fileprefix", $"log_{GetType().Name}", 
                "--timeout", "5m",
                ..runOptions.AdditionalArguments
            ]
        }, cancellationToken);

        await AssertTrx(context, result, assertions, cancellationToken, trxFilename, assertionExpression);
    }
    
    private static async Task RunWithSingleFile(IPipelineContext context, string filter,
        List<Action<TestResult>> assertions, RunOptions runOptions,
        CancellationToken cancellationToken, string assertionExpression)
    {
        var files = context.Git().RootDirectory
            .AssertExists()
            .FindFolder(x => x.Name == "TESTPROJECT_SINGLEFILE")
            .AssertExists()
            .GetFiles(_ => true)
            .ToArray();
        
        var aotApp = files.FirstOrDefault(x => x.Name == "TUnit.TestProject") 
                     ?? files.First(x => x.Name == "TUnit.TestProject.exe");
        
        var trxFilename = Guid.NewGuid().ToString("N") + ".trx";
        
        var result = await context.Command.ExecuteCommandLineTool(new CommandLineToolOptions(aotApp)
        {
            ThrowOnNonZeroExitCode = false,
            CommandLogging = CommandLogging.None,
            Arguments =
            [
                "--treenode-filter", filter, 
                "--report-trx", "--report-trx-filename", trxFilename,
                // "--diagnostic", "--diagnostic-output-fileprefix", $"log_{GetType().Name}", 
                "--timeout", "5m",
                ..runOptions.AdditionalArguments
            ]
        }, cancellationToken);

        await AssertTrx(context, result, assertions, cancellationToken, trxFilename, assertionExpression);
    }

    private static async Task AssertTrx(IPipelineContext context, CommandResult commandResult,
        List<Action<TestResult>> assertions,
        CancellationToken cancellationToken,
        string trxFilename, string assertionExpression)
    {
        try
        {
            var trxFileContents = await context.Git()
                .RootDirectory
                .AssertExists()
                .FindFile(x => x.Name == trxFilename)
                .AssertExists($"TRX file not found: {trxFilename}")
                .ReadAsync(cancellationToken);

            var parsedTrx = new TrxParser().ParseTrxContents(trxFileContents);

            var unitTestResults = parsedTrx.UnitTestResults.Where(x =>
                    !x.TestName!.Contains("Before Class: ")
                    && !x.TestName.Contains("After Class: ")
                    && !x.TestName.Contains("Before Assembly: ")
                    && !x.TestName.Contains("After Assembly: "))
                .ToList();

            var parsedResult = new TestResult(
                new DotNetTestResult(unitTestResults,
                    new ResultSummary(parsedTrx.ResultSummary.Outcome,
                        new Counters(
                            unitTestResults.Count,
                            unitTestResults.Count(x => x.Outcome != TestOutcome.NotExecuted),
                            unitTestResults.Count(x => x.Outcome == TestOutcome.Passed),
                            unitTestResults.Count(x => x.Outcome == TestOutcome.Failed),
                            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
                        )
                    )
                )
            );

            using (new AssertionScope())
            {
                assertions.ForEach(x => x.Invoke(parsedResult));
            }
        }
        catch (Exception e)
        {
            context.Logger.LogInformation("Command Input: {Input}", commandResult.CommandInput);
            context.Logger.LogInformation("Error: {Error}", commandResult.StandardError);
            context.Logger.LogInformation("Output: {Output}", commandResult.StandardOutput);

            throw new Exception($"""
                                 Error asserting results

                                 Expression: {assertionExpression}
                                 """, e);
        }
    }

    private static string GetFolder()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux-x64";
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win-x64";
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx-x64";
        }

        throw new ArgumentException("Unknown platform");
    }

    private static string GetAotExtension()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux-x64";
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ".exe";
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx-x64";
        }

        throw new ArgumentException("Unknown platform");
    }
}

public record TestResult(DotNetTestResult TrxReport)
{
    public int Failed => TrxReport.UnitTestResults.Count(x => x.Outcome == TestOutcome.Failed);
    public int Passed => TrxReport.UnitTestResults.Count(x => x.Outcome == TestOutcome.Passed);
    public int Skipped => TrxReport.UnitTestResults.Count(x => x.Outcome == TestOutcome.NotExecuted);
    public int Total => TrxReport.UnitTestResults.Count;

    public bool Successful => Failed == 0;
}

public record RunOptions()
{
    public List<string> AdditionalArguments { get; init; } = [];
    public CommandLogging CommandLogging { get; set; } = CommandLogging.Default;
}
