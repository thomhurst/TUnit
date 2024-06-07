using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Requirements;

namespace TUnit.Pipeline;

public class BuiltTestProjectRequirement : IPipelineRequirement
{
    public async Task<RequirementDecision> MustAsync(IPipelineHookContext context)
    {
        var project = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.TestProject.csproj").AssertExists();

        await context.DotNet().Build(new DotNetBuildOptions(project));
        
        return RequirementDecision.Passed;
    }
}