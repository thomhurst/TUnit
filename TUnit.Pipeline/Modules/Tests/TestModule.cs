using System.Text.RegularExpressions;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules.Tests;

[NotInParallel("Unit Tests")]
[DependsOn<ListTestsModule>]
public abstract partial class TestModule : Module<TestResult>
{
    public override ModuleRunType ModuleRunType => ModuleRunType.AlwaysRun;

    protected async Task<TestResult> RunTestsWithFilter(IPipelineContext context, string filter, List<Action<TestResult>> assertions, CancellationToken cancellationToken = default)
    {
        var project = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.TestProject.csproj").AssertExists();
        
        var result = await context.DotNet().Run(new DotNetRunOptions
        {
            Project = project,
            NoBuild = true,
            ThrowOnNonZeroExitCode = false,
            Arguments = [ "--treenode-filter", filter, "--diagnostic" ]
        }, cancellationToken);

        var parsedResult = ParseOutput(result.StandardOutput);
        
        assertions.ForEach(x => x.Invoke(parsedResult));

        return parsedResult;
    }

    private TestResult ParseOutput(string resultStandardOutput)
    {
        var values = resultStandardOutput
            .Trim()
            .Split('\n')
            .Last()
            .Split('-')[1].Trim();

        var parsed = TestCount().Matches(values);

        return new TestResult
        {
            Failed = Convert.ToInt32(parsed[0].Groups[1].Value),
            Passed = Convert.ToInt32(parsed[1].Groups[1].Value),
            Skipped = Convert.ToInt32(parsed[2].Groups[1].Value),
            Total = Convert.ToInt32(parsed[3].Groups[1].Value)
        };
    }

    [GeneratedRegex(@": (\d+)")]
    private partial Regex TestCount();
}

public record TestResult
{
    public required int Failed { get; init; }
    public required int Passed { get; init; }
    public required int Skipped { get; init; }
    public required int Total { get; init; }

    public bool Successful => Failed == 0;
}