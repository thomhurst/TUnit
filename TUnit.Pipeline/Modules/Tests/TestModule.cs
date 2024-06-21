using System.Text.RegularExpressions;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
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
public abstract partial class TestModule : Module<TestResult>
{
    public override ModuleRunType ModuleRunType => ModuleRunType.AlwaysRun;

    protected override AsyncRetryPolicy<TestResult?> RetryPolicy { get; } = Policy<TestResult?>.Handle<Exception>().RetryAsync(3);
    private readonly List<Exception> _exceptions = [];

    private static readonly AsyncSemaphore AsyncSemaphore = new(Environment.ProcessorCount * 4);

    protected override Task<bool> ShouldIgnoreFailures(IPipelineContext context, Exception exception)
    {
        _exceptions.Add(exception);
        return base.ShouldIgnoreFailures(context, exception);
    }

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

        var result = await context.DotNet().Run(new DotNetRunOptions
        {
            Project = project,
            NoBuild = true,
            ThrowOnNonZeroExitCode = false,
            CommandLogging = runOptions.CommandLogging,
            Arguments =
            [
                "--treenode-filter", filter, "--diagnostic", "--diagnostic-output-fileprefix",
                $"log_{GetType().Name}", ..runOptions.AdditionalArguments
            ]
        }, cancellationToken);

        var parsedResult = ParseOutput(result.StandardOutput);

        assertions.ForEach(x => x.Invoke(parsedResult));

        if (_exceptions.Any())
        {
            // Temporary - If we've retried and succeeded on the next retry, throw anyway
            // To get info on the --treenode-filter issue
            throw new AggregateException(_exceptions);
        }

        return parsedResult;
    }

    private TestResult ParseOutput(string resultStandardOutput)
    {
        var values = resultStandardOutput
            .Trim()
            .Split('\n')
            .Select(x => x.Trim())
            .First(x => x.Contains("- TUnit.TestProject.dll"))
            .Split('-')[1].Trim();

        var parsed = TestCount().Matches(values);

        return new TestResult
        {
            Failed = Convert.ToInt32(parsed[0].Groups[1].Value),
            Passed = Convert.ToInt32(parsed[1].Groups[1].Value),
            Skipped = Convert.ToInt32(parsed[2].Groups[1].Value),
            Total = Convert.ToInt32(parsed[3].Groups[1].Value)
        };
    }

    [GeneratedRegex(@": (\d+)")]
    private partial Regex TestCount();
}

public record TestResult
{
    public required int Failed { get; init; }
    public required int Passed { get; init; }
    public required int Skipped { get; init; }
    public required int Total { get; init; }

    public bool Successful => Failed == 0;
}

public record RunOptions()
{
    public List<string> AdditionalArguments { get; init; } = [];
    public CommandLogging CommandLogging { get; set; } = CommandLogging.Default;
}