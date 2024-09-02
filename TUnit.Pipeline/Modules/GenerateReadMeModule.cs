using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Extensions;
using ModularPipelines.FileSystem;
using ModularPipelines.Git.Attributes;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Git.Options;
using ModularPipelines.GitHub.Extensions;
using ModularPipelines.Modules;
using ModularPipelines.Options;
using Octokit;
using File = ModularPipelines.FileSystem.File;

namespace TUnit.Pipeline.Modules;

[RunOnlyOnBranch("main")]
[DependsOn<UploadToNuGetModule>]
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

        var downloadArtifact =
            await context.Downloader.DownloadFileAsync(new DownloadFileOptions(new Uri(latestBenchmark.ArtifactsUrl)), cancellationToken);

        var unzipped = context.Zip.UnZipToFolder(downloadArtifact, Folder.CreateTemporaryFolder());

        var files = await unzipped.GetFiles("**.md").SelectAsync(x => x.ReadAsync(cancellationToken), cancellationToken: cancellationToken).ProcessInParallel();

        var markdown = string.Join(Environment.NewLine, files);

        await readme.WriteAsync(template.Replace("${{ BENCHMARK }}", markdown), cancellationToken);

        await context.Git().Commands.Add(new GitAddOptions
        {
            All = true
        }, cancellationToken);

        await context.Git().Commands.Push(token: cancellationToken);
        
        return readme;
    }
}