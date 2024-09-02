using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Extensions;
using ModularPipelines.FileSystem;
using ModularPipelines.Git.Attributes;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Git.Options;
using ModularPipelines.GitHub.Attributes;
using ModularPipelines.GitHub.Extensions;
using ModularPipelines.Modules;
using ModularPipelines.Options;
using Octokit;
using File = ModularPipelines.FileSystem.File;

namespace TUnit.Pipeline.Modules;

[RunOnlyOnBranch("main")]
[RunOnLinuxOnly]
[DependsOn<PackTUnitFilesModule>]
[DependsOn<TestNugetPackageModule>]
[SkipIfDependabot]
public class GenerateReadMeModule : Module<File>
{
    protected override async Task<File?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var template = await context.Git()
            .RootDirectory
            .AssertExists()
            .FindFile(x => x.Name == "README_Template.md")
            .AssertExists()
            .ReadAsync(cancellationToken);
        
        var readme = context.Git()
            .RootDirectory
            .AssertExists()
            .FindFile(x => x.Name == "README.md")
            .AssertExists();

        var runs = await context.GitHub().Client.Actions.Workflows.Runs.List(context.GitHub().RepositoryInfo.Owner,
            context.GitHub().RepositoryInfo.RepositoryName);

        var latestBenchmark = runs.WorkflowRuns.FirstOrDefault(x =>
            x.Name == "Speed Comparison" && x.Status.Value == WorkflowRunStatus.Completed && x.Conclusion?.Value == WorkflowRunConclusion.Success);

        if (latestBenchmark == null)
        {
            return null;
        }

        var artifacts = await context.GitHub().Client.Actions.Artifacts.ListWorkflowArtifacts(context.GitHub().RepositoryInfo.Owner,
            context.GitHub().RepositoryInfo.RepositoryName,
            latestBenchmark.Id);

        var downloadedArtifacts = await artifacts.Artifacts.SelectAsync(x => context.GitHub().Client.Actions.Artifacts.DownloadArtifact(
            context.GitHub().RepositoryInfo.Owner,
            context.GitHub().RepositoryInfo.RepositoryName,
            x.Id,
            "zip"))
            .ProcessInParallel();

        var artifactFiles = await downloadedArtifacts.SelectAsync(async x =>
        {
            var newTemporaryFilePath = File.GetNewTemporaryFilePath();
            await newTemporaryFilePath.WriteAsync(x, cancellationToken);
            return newTemporaryFilePath;
        }).ProcessInParallel();

        var unzipped = artifactFiles.Select(x => context.Zip.UnZipToFolder(x, Folder.CreateTemporaryFolder()));

        var files = await unzipped.SelectMany(x => x.GetFiles("**.md")).SelectAsync(x => x.ReadAsync(cancellationToken), cancellationToken: cancellationToken).ProcessInParallel();

        var markdown = string.Join(Environment.NewLine, files);

        var newContents = template.Replace("${{ BENCHMARK }}", markdown);

        if (newContents == await readme.ReadAsync(cancellationToken))
        {
            return null;
        }
        
        await readme.WriteAsync(newContents, cancellationToken);

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
        
        await context.Git().Commands.Checkout(new GitCheckoutOptions(newBranchName, true)
        {
            Arguments = ["-b"]
        }, cancellationToken);
        
        await context.Git().Commands.Add(new GitAddOptions
        {
            Arguments = ["README.md"],
            WorkingDirectory = readme.Folder!
        }, cancellationToken);

        await context.Git().Commands.Commit(new GitCommitOptions
        {
            Message = "Update README.md"
        }, cancellationToken);
        
        await context.Git().Commands.Push(token: cancellationToken);

        var pr = await context.GitHub().Client.PullRequest.Create(context.GitHub().RepositoryInfo.Owner,
            context.GitHub().RepositoryInfo.RepositoryName,
            new NewPullRequest("Update ReadMe", newBranchName, "main"));

        await context.GitHub().Client.PullRequest.Review.Create(context.GitHub().RepositoryInfo.Owner,
            context.GitHub().RepositoryInfo.RepositoryName,
            pr.Number,
            new PullRequestReviewCreate
            {
                Event = PullRequestReviewEvent.Approve
            });

        await context.Command.ExecuteCommandLineTool(new CommandLineToolOptions("gh", ["pr", "merge", "--auto", pr.Number.ToString()]), cancellationToken);
        
        return readme;
    }
}