using EnumerableAsyncProcessor.Extensions;
using Microsoft.Extensions.Logging;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Extensions;
using ModularPipelines.FileSystem;
using ModularPipelines.Git.Attributes;
using ModularPipelines.Git.Extensions;
using ModularPipelines.GitHub.Attributes;
using ModularPipelines.GitHub.Extensions;
using ModularPipelines.Modules;
using Octokit;
using File = ModularPipelines.FileSystem.File;

namespace TUnit.Pipeline.Modules;

[RunOnlyOnBranch("main")]
[RunOnLinuxOnly]
[SkipIfDependabot]
[DependsOn<UploadToNuGetModule>]
[DependsOn<PackTUnitFilesModule>]
[DependsOn<TestNugetPackageModule>]
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
        
        context.Logger.LogInformation("Downloaded Artifacts to: {Files}", string.Join(", ", artifactFiles.Select(x => x.ToString())));

        var unzipped = artifactFiles.Select(x => context.Zip.UnZipToFolder(x, Folder.CreateTemporaryFolder())).ToList();
        
        context.Logger.LogInformation("Unzipped locations: {Folders}", string.Join(", ", unzipped.Select(x => x.ToString())));

        var markdownFiles = unzipped.SelectMany(x => x.GetFiles(f => f.Extension == ".md")).ToList();
        
        context.Logger.LogInformation("Markdown files found: {Files}", string.Join(", ", markdownFiles.Select(x => x.ToString())));
        
        var filesContents = await markdownFiles.SelectAsync(x => x.ReadAsync(cancellationToken), cancellationToken: cancellationToken).ProcessInParallel();
        
        var markdown = string.Join(Environment.NewLine, filesContents);

        var newContents = template.Replace("${{ BENCHMARK }}", markdown);

        if (newContents == await readme.ReadAsync(cancellationToken))
        {
            return null;
        }
        
        await readme.WriteAsync(newContents, cancellationToken);
        
        return readme;
    }
}