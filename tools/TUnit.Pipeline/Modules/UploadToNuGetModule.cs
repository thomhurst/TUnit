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
// The engine tests, in both modes, run in aot.yml, not in this pipeline. ExecuteAsync waits for
// that workflow to pass for the commit before it pushes anything (WaitForAotWorkflowAsync fails
// closed). Do not drop that wait: it is the only release gate for the engine tests.
// The engine modules no longer wait for the test suites below, and aot.yml does not run them,
// so a release has to.
[DependsOn<RunAnalyzersTestsModule>]
[DependsOn<RunUnitTestsModule>]
[DependsOn<RunTemplateTestsModule>]
[DependsOn<RunAspNetTestsModule>(Optional = true)]
[DependsOn<RunAssertionsTestsModule>]
[DependsOn<RunAssertionsShouldTestsModule>]
[DependsOn<RunAssertionsShouldSourceGeneratorTestsModule>]
[DependsOn<RunAssertionsAnalyzersTestsModule>]
[DependsOn<RunPublicAPITestsModule>]
[DependsOn<RunSourceGeneratorTestsModule>]
[DependsOn<RunAssertionsCodeFixersTestsModule>]
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

    // Above the AOT wait, with time left to push the packages, so the module's own timeout
    // cannot cut the wait short.
    private static readonly TimeSpan ModuleTimeout = AotWorkflowTimeout + TimeSpan.FromMinutes(30);

    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithTimeout(ModuleTimeout)
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

    // Blocks the release until an aot.yml run for this commit has succeeded. Fails when every
    // run for the commit finished without success, or when no run succeeds within the timeout.
    // A run that is not listed yet counts as pending. Any successful run on the commit passes the
    // gate, whatever its trigger: a push run and a dispatched run build the same code. A
    // dotnet.yml publish dispatched on a commit with no aot.yml run fails closed.
    private static async Task WaitForAotWorkflowAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var gitHub = context.GitHub();
        var repository = gitHub.EnvironmentVariables.Repository?.Split('/');
        var sha = gitHub.EnvironmentVariables.Sha;

        if (repository is not [var owner, var name])
        {
            throw new InvalidOperationException("Refusing to publish to NuGet: GITHUB_REPOSITORY is not set as owner/name, so the AOT workflow result cannot be checked.");
        }

        if (string.IsNullOrEmpty(sha))
        {
            throw new InvalidOperationException("Refusing to publish to NuGet: GITHUB_SHA is not set, so the AOT workflow result cannot be checked.");
        }

        var deadline = DateTimeOffset.UtcNow + AotWorkflowTimeout;

        while (true)
        {
            WorkflowRunsResponse runs;

            try
            {
                runs = await gitHub.Client.Actions.Workflows.Runs.ListByWorkflow(
                    owner,
                    name,
                    AotWorkflowFileName,
                    new WorkflowRunsRequest { HeadSha = sha });
            }
            catch (ApiException e) when (DateTimeOffset.UtcNow < deadline)
            {
                // A transient API failure or rate limit should not abort a release mid-wait. The
                // deadline still bounds the retries.
                context.Logger.LogWarning(e, "Listing {Workflow} runs failed; retrying", AotWorkflowFileName);
                await Task.Delay(AotWorkflowPollInterval, cancellationToken);
                continue;
            }

            if (runs.WorkflowRuns.Any(run => run.Status == WorkflowRunStatus.Completed && run.Conclusion == WorkflowRunConclusion.Success))
            {
                context.Logger.LogInformation("The {Workflow} workflow passed for {Sha}", AotWorkflowFileName, sha);
                return;
            }

            if (runs.WorkflowRuns.Count > 0 && !runs.WorkflowRuns.Any(run => run.Status != WorkflowRunStatus.Completed))
            {
                var conclusions = string.Join(", ", runs.WorkflowRuns.Select(run => $"{run.HtmlUrl} ({run.Conclusion?.StringValue})"));

                throw new InvalidOperationException(
                    $"Refusing to publish to NuGet: no {AotWorkflowFileName} run passed for {sha}: {conclusions}");
            }

            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    runs.WorkflowRuns.Count == 0
                        ? $"Refusing to publish to NuGet: no {AotWorkflowFileName} run appeared for {sha} within {AotWorkflowTimeout.TotalMinutes} minutes. Run that workflow on this commit and retry."
                        : $"Refusing to publish to NuGet: the {AotWorkflowFileName} run for {sha} did not finish within {AotWorkflowTimeout.TotalMinutes} minutes.");
            }

            context.Logger.LogInformation("Waiting for the {Workflow} workflow to finish for {Sha}", AotWorkflowFileName, sha);
            await Task.Delay(AotWorkflowPollInterval, cancellationToken);
        }
    }
}
