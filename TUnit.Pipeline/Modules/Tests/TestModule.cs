using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet;
using ModularPipelines.DotNet.Enums;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.DotNet.Parsers.NUnitTrx;
using ModularPipelines.Enums;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;
using Polly;
using Polly.Retry;

namespace TUnit.Pipeline.Modules.Tests;

[NotInParallel("Unit Tests")]
[ParallelLimiter<ProcessorParallelLimit>]
[DependsOn<PublishAOTModule>]
public abstract class TestModule : Module<TestResult>
{
    public override ModuleRunType ModuleRunType => ModuleRunType.AlwaysRun;

    protected override AsyncRetryPolicy<TestResult?> RetryPolicy { get; } = Policy<TestResult?>.Handle<Exception>().RetryAsync(3);
    
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true 
    };

    protected Task<TestResult?> RunTestsWithFilter(IPipelineContext context, string filter,
        List<Action<TestResult>> assertions,
        CancellationToken cancellationToken = default)
    {
        return RunTestsWithFilter(context, filter, assertions, new RunOptions(), cancellationToken);
    }

    protected async Task<TestResult?> RunTestsWithFilter(IPipelineContext context, string filter,
        List<Action<TestResult>> assertions, RunOptions runOptions, CancellationToken cancellationToken = default)
    {
        await SubModule("WithoutAot", async () =>
        {
            await RunWithoutAot(context, filter, assertions, runOptions, cancellationToken);
        });

        await SubModule("Aot", async () =>
        {
            await RunWithAot(context, filter, assertions, runOptions, cancellationToken);
        });

        return null;
    }

    private static async Task RunWithoutAot(IPipelineContext context, string filter, List<Action<TestResult>> assertions, RunOptions runOptions,
        CancellationToken cancellationToken)
    {
        var trxFilename = Guid.NewGuid().ToString("N") + ".trx";
        
        var result = await context.DotNet().Run(new DotNetRunOptions
        {
            WorkingDirectory = context.Git().RootDirectory.GetFolder("TUnit.TestProject"),
            Configuration = Configuration.Release,
            NoBuild = true,
            ThrowOnNonZeroExitCode = false,
            CommandLogging = runOptions.CommandLogging,
            Arguments =
            [
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

        await AssertTrx(context, assertions, cancellationToken, trxFilename);
    }
    
    private static async Task RunWithAot(IPipelineContext context, string filter, List<Action<TestResult>> assertions, RunOptions runOptions,
        CancellationToken cancellationToken)
    {
        var folder = context.Git().RootDirectory.AssertExists()
            .GetFolder("TUnit.TestProject")
            .GetFolder("bin")
            .GetFolder("Release")
            .GetFolder("net8.0")
            .GetFolder(GetFolder())
            .GetFolder("publish");
        
        var files = folder.ListFiles().ToArray();

        var aotApp = files.FirstOrDefault(x => x.Name == "TUnit.TestProject") 
                     ?? files.First(x => x.Name == "TUnit.TestProject.exe");
        
        var trxFilename = Guid.NewGuid().ToString("N") + ".trx";
        
        var result = await context.Command.ExecuteCommandLineTool(new CommandLineToolOptions(aotApp)
        {
            ThrowOnNonZeroExitCode = false,
            CommandLogging = runOptions.CommandLogging,
            Arguments =
            [
                "--treenode-filter", filter, 
                "--report-trx", "--report-trx-filename", trxFilename,
                // "--diagnostic", "--diagnostic-output-fileprefix", $"log_{GetType().Name}", 
                "--timeout", "5m",
                ..runOptions.AdditionalArguments
            ]
        }, cancellationToken);

        await AssertTrx(context, assertions, cancellationToken, trxFilename);
    }

    private static async Task AssertTrx(IPipelineContext context, List<Action<TestResult>> assertions, CancellationToken cancellationToken,
        string trxFilename)
    {
        var trxFileContents = await context.Git()
            .RootDirectory
            .AssertExists()
            .FindFile(x => x.Name == trxFilename)
            .AssertExists($"TRX file not found: {trxFilename}")
            .ReadAsync(cancellationToken);

        var parsedTrx = new TrxParser().ParseTrxContents(trxFileContents);

        var parsedResult = new TestResult(parsedTrx);

        try
        {
            assertions.ForEach(x => x.Invoke(parsedResult));
        }
        catch (Exception e)
        {
            throw new Exception($"""
                                 Error asserting results

                                 Trx file: {JsonSerializer.Serialize(parsedResult, JsonSerializerOptions)}
                                 Raw Trx file: {trxFileContents}
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

public record TestResult
{
    public DotNetTestResult TrxReport { get; }

    public TestResult(DotNetTestResult trxReport)
    {
        TrxReport = trxReport;
    }

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