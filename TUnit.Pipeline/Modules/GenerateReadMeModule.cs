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

        // Expected artifact name pattern: ubuntu_markdown_{build_time|run_time_{class}}
        // Example: ubuntu_markdown_run_time_AsyncTests, ubuntu_markdown_build_time
        var benchmarkArtifacts = artifacts.Artifacts
            .Where(x => x.Name.StartsWith("ubuntu_markdown_"))
            .ToList();

        if (benchmarkArtifacts.Count == 0)
        {
            context.Logger.LogWarning("No benchmark markdown artifacts found.");
            return null;
        }

        // Grouping is by Scenario (e.g., "markdown_run_time_AsyncTests" or "markdown_build_time")
        foreach (var groupedArtifacts in benchmarkArtifacts
                     .OrderBy(x => x.Name)
                     .GroupBy(x => x.Name.Substring("ubuntu_".Length)))
        {
            fileContents.AppendLine($"### Scenario: {GetScenario(groupedArtifacts.Key)}");

            foreach (var artifact in groupedArtifacts.OrderBy(x => x.Name))
            {
                context.Logger.LogInformation("Processing artifact: {ArtifactName}", artifact.Name);

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
                fileContents.AppendLine(contents);
                fileContents.AppendLine();

                context.Logger.LogInformation("Added contents from {MarkdownFile}", markdownFile.Name);
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
            "AsyncTests" => "Tests running asynchronous operations and async/await patterns",
            "DataDrivenTests" => "Parameterized tests with multiple test cases using data attributes",
            "MassiveParallelTests" => "Tests executing massively parallel workloads with CPU-bound, I/O-bound, and mixed operations",
            "MatrixTests" => "Tests with complex parameter combinations creating 25-125 test variations",
            "ScaleTests" => "Large-scale parameterized tests with 100+ test cases testing framework scalability",
            _ => throw new ArgumentException($"Unknown class name: {fileName}", nameof(fileName))
        };
    }
}
