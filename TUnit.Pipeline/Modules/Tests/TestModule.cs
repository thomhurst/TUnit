﻿using System.Text.Json;
using System.Text.Json.Nodes;
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
using Polly;
using Polly.Retry;
using Semaphores;

namespace TUnit.Pipeline.Modules.Tests;

[NotInParallel("Unit Tests")]
[DependsOn<ListTestsModule>]
public abstract class TestModule : Module<TestResult>
{
    public override ModuleRunType ModuleRunType => ModuleRunType.AlwaysRun;

    protected override AsyncRetryPolicy<TestResult?> RetryPolicy { get; } = Policy<TestResult?>.Handle<Exception>().RetryAsync(3);

    private static readonly AsyncSemaphore AsyncSemaphore = new(Environment.ProcessorCount * 4);

    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true 
    };

    protected Task<TestResult> RunTestsWithFilter(IPipelineContext context, string filter,
        List<Action<TestResult>> assertions,
        CancellationToken cancellationToken = default)
    {
        return RunTestsWithFilter(context, filter, assertions, new RunOptions(), cancellationToken);
    }

    protected async Task<TestResult> RunTestsWithFilter(IPipelineContext context, string filter,
        List<Action<TestResult>> assertions, RunOptions runOptions, CancellationToken cancellationToken = default)
    {
        using var lockHandle = await AsyncSemaphore.WaitAsync(cancellationToken);

        var project = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.TestProject.csproj")
            .AssertExists();

        var trxFilename = Guid.NewGuid().ToString("N") + ".trx";
        
        var result = await context.DotNet().Run(new DotNetRunOptions
        {
            Project = project,
            NoBuild = true,
            ThrowOnNonZeroExitCode = false,
            CommandLogging = runOptions.CommandLogging,
            Arguments =
            [
                "--treenode-filter", filter, 
                "--report-trx", "--report-trx-filename", trxFilename,
                // "--diagnostic", "--diagnostic-output-fileprefix", $"log_{GetType().Name}", 
                ..runOptions.AdditionalArguments
            ]
        }, cancellationToken);

        var trxFileContents = await context.Git()
            .RootDirectory
            .AssertExists()
            .FindFile(x => x.Name == trxFilename)
            .AssertExists()
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

        return parsedResult;
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