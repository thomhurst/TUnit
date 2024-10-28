using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Attributes;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Git.Options;
using ModularPipelines.GitHub.Attributes;
using ModularPipelines.GitHub.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;
using Octokit;

namespace TUnit.Pipeline.Modules;

[RunOnlyOnBranch("main")]
[RunOnLinuxOnly]
[DependsOn<PackTUnitFilesModule>]
[DependsOn<TestNugetPackageModule>]
[DependsOn<GenerateReadMeModule>(IgnoreIfNotRegistered = true)]
[SkipIfDependabot]
[ModuleCategory("ReadMe")]
public class CommitFilesModule : Module<CommandResult>
{
    protected override async Task<SkipDecision> ShouldSkip(IPipelineContext context)
    {
        var generateReadMeModule = GetModuleIfRegistered<GenerateReadMeModule>();

        if (generateReadMeModule is null)
        {
            return "Nothing to commit";
        }

        var result = await generateReadMeModule;
        
        return result.SkipDecision.ShouldSkip || !result.HasValue;
    }

    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        await context.Git().Commands.Config(new GitConfigOptions
        {
            Global = true,
            Arguments = ["user.name", context.GitHub().EnvironmentVariables.Actor!]
        }, cancellationToken);
        
        await context.Git().Commands.Config(new GitConfigOptions
        {
            Global = true,
            Arguments = ["user.email", $"{context.GitHub().EnvironmentVariables.ActorId!}_{context.GitHub().EnvironmentVariables.Actor!}@users.noreply.github.com"]
        }, cancellationToken);

        var newBranchName = $"feature/readme-{Guid.NewGuid():N}";
        
        await context.Git().Commands.Checkout(new GitCheckoutOptions(newBranchName, true), cancellationToken);
        
        await context.Git().Commands.Add(new GitAddOptions
        {
            Arguments = ["README.md"],
            WorkingDirectory = context.Git().RootDirectory.AssertExists()!
        }, cancellationToken);

        await context.Git().Commands.Commit(new GitCommitOptions
        {
            Message = "Update README.md"
        }, cancellationToken);

        await context.Git().Commands.Push(new GitPushOptions
        {
            Arguments = ["--set-upstream", "origin", newBranchName]
        });
        
        await context.Git().Commands.Push(token: cancellationToken);

        var pr = await context.GitHub().Client.PullRequest.Create(context.GitHub().RepositoryInfo.Owner,
            context.GitHub().RepositoryInfo.RepositoryName,
            new NewPullRequest("Update ReadMe", newBranchName, "main"));

        return await context.Command.ExecuteCommandLineTool(new CommandLineToolOptions("gh", "pr", "merge", "--admin", "--squash", pr.Number.ToString())
        {
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["GH_TOKEN"] = Environment.GetEnvironmentVariable("ADMIN_TOKEN")
            }
        }, cancellationToken);
    }
}