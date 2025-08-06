using System.Text;
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
[ModuleCategory("ReadMe")]
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
            context.GitHub().RepositoryInfo.RepositoryName, new WorkflowRunsRequest
            {
                HeadSha = context.GitHub().EnvironmentVariables.Sha
            });

        var latestBenchmark = runs.WorkflowRuns.FirstOrDefault(x =>
            x.Name == "Speed Comparison" && x.Status.Value == WorkflowRunStatus.Completed && x.Conclusion?.Value == WorkflowRunConclusion.Success);

        if (latestBenchmark == null)
        {
            context.Logger.LogWarning("No completed benchmark runs found for the current commit.");
            return null;
        }

        var artifacts = await context.GitHub().Client.Actions.Artifacts.ListWorkflowArtifacts(context.GitHub().RepositoryInfo.Owner,
            context.GitHub().RepositoryInfo.RepositoryName,
            latestBenchmark.Id);

        context.Logger.LogInformation("Found {ArtifactCount} artifacts for the latest benchmark run.", artifacts.Artifacts.Count);

        var fileContents = new StringBuilder();

        // Grouping is by Scenario
        foreach (var groupedArtifacts in artifacts.Artifacts
                     .OrderBy(x => x.Name)
                     .GroupBy(x => x.Name.Split("_", 2)[1]))
        {
            fileContents.AppendLine($"### Scenario: {GetScenario(groupedArtifacts.Key)}");

            foreach (var artifact in groupedArtifacts.OrderBy(x => x.Name))
            {
                var operatingSystem = artifact.Name.Split("_")[0];

                context.Logger.LogInformation("Processing artifact: {ArtifactName} for OS: {OperatingSystem}", artifact.Name, operatingSystem);

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

                fileContents.AppendLine();
                fileContents.AppendLine($"#### {operatingSystem}");
                fileContents.AppendLine();
                fileContents.AppendLine(contents);
                fileContents.AppendLine();

                context.Logger.LogInformation("Added contents from {MarkdownFile} for OS: {OperatingSystem}", markdownFile.Name, operatingSystem);
            }
        }

        var newContents = template.Replace("${{ BENCHMARK }}", fileContents.ToString());

        if (newContents == await readme.ReadAsync(cancellationToken))
        {
            context.Logger.LogInformation("No changes to README.md, skipping write.");
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
            "AssertionTests" => "Tests focused on assertion performance and validation",
            "AsyncTests" => "Tests running asynchronous operations and async/await patterns",
            "BasicTests" => "Simple tests with basic operations and assertions",
            "DataDrivenTests" => "Parameterized tests with multiple test cases using data attributes",
            "FixtureTests" => "Tests utilizing class fixtures and shared test context",
            "ParallelTests" => "Tests executing in parallel to test framework parallelization",
            "RepeatTests" => "A test that takes 50ms to execute, repeated 100 times",
            "SetupTeardownTests" => "Tests with setup and teardown lifecycle methods",
            _ => throw new ArgumentException($"Unknown class name: {fileName}", nameof(fileName))
        };
    }
}
