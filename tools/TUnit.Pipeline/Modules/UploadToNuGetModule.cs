using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git.Attributes;
using ModularPipelines.Git.Extensions;
using ModularPipelines.GitHub.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;
using Octokit;

namespace TUnit.Pipeline.Modules;

[RunOnlyOnBranch("main")]
[RunOnLinuxOnly]
[DependsOn<GenerateVersionModule>]
[DependsOn<PackTUnitFilesModule>]
[DependsOn<TestNugetPackageModule>]
// The AOT-mode engine tests run in aot.yml, not in this pipeline. ExecuteAsync waits for that
// workflow to pass for the commit before it pushes anything.
[DependsOn<RunEngineTestsModule>]
// The engine modules no longer wait for these, so a release has to.
[DependsOn<RunRpcTestsModule>(Optional = true)]
[DependsOn<RunPlaywrightTestsModule>(Optional = true)]
[DependsOn<TestFSharpNugetPackageModule>]
[DependsOn<TestVBNugetPackageModule>]
public class UploadToNuGetModule(IOptions<NuGetOptions> options) : Module<CommandResult[]>
{
    // Local builds pin this dummy version (see Directory.Build.props). Pushing it to NuGet.org
    // would burn the version and ship an unbuildable package, so guard against it explicitly.
    private const string DummyLocalVersion = "99.99.99";

    private const string AotWorkflowFileName = "aot.yml";
    private static readonly TimeSpan AotWorkflowPollInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan AotWorkflowTimeout = TimeSpan.FromMinutes(60);

    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithSkipWhen(_ =>
        {
            if (!options.Value.ShouldPublish)
            {
                return SkipDecision.Skip("Should Publish is false");
            }

            if (string.IsNullOrEmpty(options.Value.ApiKey))
            {
                return SkipDecision.Skip("No API key found");
            }

            return SkipDecision.DoNotSkip;
        })
        .Build();

    protected override async Task<CommandResult[]?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var versionResult = await context.GetModule<GenerateVersionModule>();
        var semVer = versionResult.ValueOrDefault?.SemVer;

        if (string.IsNullOrWhiteSpace(semVer) || semVer.StartsWith(DummyLocalVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Refusing to publish to NuGet: resolved version '{semVer}' is missing or is the dummy local version '{DummyLocalVersion}'. " +
                "This indicates the CI versioning step did not run; aborting to avoid shipping a bad package.");
        }

        await WaitForAotWorkflowAsync(context, cancellationToken);

        var nupkgs = context.Git().RootDirectory
            .GetFiles(x => x.NameWithoutExtension.Contains("TUnit") && x.Extension is ".nupkg")
            .ToArray();

        if (nupkgs.Any(file => file.NameWithoutExtension.Contains(DummyLocalVersion)))
        {
            throw new InvalidOperationException(
                $"Refusing to publish to NuGet: found a package stamped with the dummy local version '{DummyLocalVersion}'.");
        }

        var results = new CommandResult[nupkgs.Length];

        for (var i = 0; i < nupkgs.Length; i++)
        {
            results[i] = await context.DotNet().Nuget.Push(new DotNetNugetPushOptions
            {
                Path = nupkgs[i].Path,
                Source = "https://api.nuget.org/v3/index.json",
                ApiKey = options.Value.ApiKey,
            }, new CommandExecutionOptions
            {
                LogSettings = new CommandLoggingOptions
                {
                    ShowCommandArguments = true,
                    ShowStandardError = true,
                    ShowExecutionTime = true,
                    ShowExitCode = true
                }
            }, cancellationToken);
        }

        return results;
    }

    // Blocks the release until an aot.yml run for this commit has succeeded. Fails when no run
    // exists, when every run for the commit finished without success, or when the runs in
    // progress do not finish within the timeout.
    private static async Task WaitForAotWorkflowAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var gitHub = context.GitHub();
        var repository = gitHub.EnvironmentVariables.Repository!.Split('/');
        var sha = gitHub.EnvironmentVariables.Sha;

        if (string.IsNullOrEmpty(sha))
        {
            throw new InvalidOperationException("Refusing to publish to NuGet: GITHUB_SHA is not set, so the AOT workflow result cannot be checked.");
        }

        var deadline = DateTimeOffset.UtcNow + AotWorkflowTimeout;

        while (true)
        {
            var runs = await gitHub.Client.Actions.Workflows.Runs.ListByWorkflow(
                repository[0],
                repository[1],
                AotWorkflowFileName,
                new WorkflowRunsRequest { HeadSha = sha });

            if (runs.WorkflowRuns.Any(run => run.Status == WorkflowRunStatus.Completed && run.Conclusion == WorkflowRunConclusion.Success))
            {
                context.Logger.LogInformation("The {Workflow} workflow passed for {Sha}", AotWorkflowFileName, sha);
                return;
            }

            if (!runs.WorkflowRuns.Any(run => run.Status != WorkflowRunStatus.Completed))
            {
                var conclusions = string.Join(", ", runs.WorkflowRuns.Select(run => $"{run.HtmlUrl} ({run.Conclusion?.StringValue})"));

                throw new InvalidOperationException(
                    runs.WorkflowRuns.Count == 0
                        ? $"Refusing to publish to NuGet: no {AotWorkflowFileName} run exists for {sha}. Run that workflow on this commit and retry."
                        : $"Refusing to publish to NuGet: no {AotWorkflowFileName} run passed for {sha}: {conclusions}");
            }

            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"Refusing to publish to NuGet: the {AotWorkflowFileName} run for {sha} did not finish within {AotWorkflowTimeout.TotalMinutes} minutes.");
            }

            context.Logger.LogInformation("Waiting for the {Workflow} workflow to finish for {Sha}", AotWorkflowFileName, sha);
            await Task.Delay(AotWorkflowPollInterval, cancellationToken);
        }
    }
}
