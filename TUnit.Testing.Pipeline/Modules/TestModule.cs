using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Testing.Pipeline;


public abstract class TestModule : Module<CommandResult>
{
    protected async Task<CommandResult> RunTestsWithFilter(IPipelineContext context, string filter)
    {
        var project = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.TestProject.csproj").AssertExists();

        return await context.DotNet().Test(new DotNetTestOptions(project)
        {
            NoBuild = true,
            Filter = filter
        });
    }
}