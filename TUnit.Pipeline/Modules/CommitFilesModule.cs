using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
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
[DependsOn<TestFSharpNugetPackageModule>]
[DependsOn<TestVBNugetPackageModule>]
[DependsOn<GenerateReadMeModule>(IgnoreIfNotRegistered = true)]
[SkipIfDependabot]
[ModuleCategory("ReadMe")]
public class CommitFilesModule : Module<CommandResult>
{
    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithSkipWhen(async ctx =>
        {
            var generateReadMeModule = ctx.GetModuleIfRegistered<GenerateReadMeModule>();

            if (generateReadMeModule is null)
            {
                return SkipDecision.Skip("Nothing to commit");
            }

            var result = await generateReadMeModule;

            return result.IsSkipped || !result.IsSuccess
                ? SkipDecision.Skip("GenerateReadMeModule was skipped or has no value")
                : SkipDecision.DoNotSkip;
        })
        .Build();

    protected override async Task<CommandResult?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var repositoryId = long.Parse(context.GitHub().EnvironmentVariables.RepositoryId!);

        await context.Git().Commands.Config(new GitConfigOptions
        {
            Global = true,
            Arguments = ["user.name", context.GitHub().EnvironmentVariables.Actor!]
        }, token: cancellationToken);

        await context.Git().Commands.Config(new GitConfigOptions
        {
            Global = true,
            Arguments = ["user.email", $"{context.GitHub().EnvironmentVariables.ActorId!}_{context.GitHub().EnvironmentVariables.Actor!}@users.noreply.github.com"]
        }, token: cancellationToken);

        var newBranchName = $"feature/readme-{Guid.NewGuid():N}";

        await context.Git().Commands.Checkout(new GitCheckoutOptions(newBranchName, true), token: cancellationToken);

        await context.Git().Commands.Add(new GitAddOptions
        {
            Arguments = ["README.md"],
        }, new CommandExecutionOptions
        {
            WorkingDirectory = context.Git().RootDirectory.AssertExists().Path
        }, cancellationToken);

        await context.Git().Commands.Commit(new GitCommitOptions
        {
            Message = "Update README.md"
        }, token: cancellationToken);

        await context.Git().Commands.Push(new GitPushOptions
        {
            SetUpstream = true,
            Arguments = ["origin", newBranchName]
        }, token: cancellationToken);

        await context.Git().Commands.Push(token: cancellationToken);

        var pr = await context.GitHub().Client.PullRequest.Create(repositoryId,
            new NewPullRequest("Update ReadMe", newBranchName, "main"));

        var issueUpdate = new IssueUpdate();
        issueUpdate.AddLabel("ignore-for-release");

        await context.GitHub().Client.Issue.Update(repositoryId, pr.Number,
            issueUpdate);

        var adminToken = Environment.GetEnvironmentVariable("ADMIN_TOKEN");
        return await context.Shell.Bash.Command(new BashCommandOptions($"GH_TOKEN={adminToken} gh pr merge --admin --squash {pr.Number}"), cancellationToken);
    }
}
