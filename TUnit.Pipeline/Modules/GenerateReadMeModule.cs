﻿using System.Text;
using EnumerableAsyncProcessor.Extensions;
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
public class GenerateReadMeModule : Module<File>
{
    private readonly object _stringBuilderLock = new();
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

        var fileContents = new StringBuilder();

        // Grouping is by Scenario
        await artifacts.Artifacts.GroupBy(x => x.Name.Split("_", 2)[1]).ForEachAsync(async groupedArtifacts =>
        {
            await groupedArtifacts.ForEachAsync(async artifact =>
            {
                var operatingSystem = artifact.Name.Split("_")[1];

                var stream = await context.GitHub().Client.Actions.Artifacts.DownloadArtifact(
                    context.GitHub().RepositoryInfo.Owner,
                    context.GitHub().RepositoryInfo.RepositoryName,
                    artifact.Id,
                    "zip");

                var downloadedZip = File.GetNewTemporaryFilePath();
                await downloadedZip.WriteAsync(stream, cancellationToken);

                var unzippedDirectory = context.Zip.UnZipToFolder(downloadedZip, Folder.CreateTemporaryFolder());

                var markdownFile = unzippedDirectory.FindFile(x => x.Extension == ".md").AssertExists();

                var contents = await markdownFile.ReadAsync(cancellationToken);

                lock (_stringBuilderLock)
                {
                    fileContents.AppendLine($"### Scenario: {GetScenario(groupedArtifacts.Key)}");
                    fileContents.AppendLine();
                    fileContents.AppendLine($"#### {operatingSystem}");
                    fileContents.AppendLine();
                    fileContents.AppendLine(contents);
                    fileContents.AppendLine();
                }
            }, cancellationToken: cancellationToken).ProcessOneAtATime();
        }, cancellationToken: cancellationToken).ProcessOneAtATime();

        var newContents = template.Replace("${{ BENCHMARK }}", fileContents.ToString());

        if (newContents == await readme.ReadAsync(cancellationToken))
        {
            return null;
        }
        
        await readme.WriteAsync(newContents, cancellationToken);
        
        return readme;
    }

    private string GetScenario(string fileName)
    {
        if (fileName.Contains("build_time"))
        {
            return "Building the test project";
        }
        
        return fileName.Split("_").Last() switch
        {
            "BasicTest" => "A single test that completes instantly (including spawning a new process and initialising the test framework)",
            "RepeatTests" => "A test that takes 50ms to execute, repeated 100 times (including spawning a new process and initialising the test framework)",
            _ => throw new ArgumentException($"Unknown class name: {fileName}", nameof(fileName))
        };
    }
}
