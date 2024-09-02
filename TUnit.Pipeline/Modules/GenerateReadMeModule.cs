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

        await readme.WriteAsync(template.Replace("${{ BENCHMARK }}", markdown), cancellationToken);

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
        
        return readme;
    }
}