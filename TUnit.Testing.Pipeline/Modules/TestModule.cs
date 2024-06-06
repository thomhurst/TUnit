using ModularPipelines.Context;
using ModularPipelines.DotNet;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace TUnit.Testing.Pipeline.Modules;


public abstract class TestModule : Module<DotNetTestResult>
{
    public override ModuleRunType ModuleRunType => ModuleRunType.AlwaysRun;

    protected async Task<DotNetTestResult> RunTestsWithFilter(IPipelineContext context, string filter, List<Action<DotNetTestResult>> assertions)
    {
        var project = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.TestProject.csproj").AssertExists();

        var trxFileName = $"{Guid.NewGuid():N}.trx";
        
        await context.DotNet().Run(new DotNetRunOptions
        {
            Project = project,
            NoBuild = true,
            ThrowOnNonZeroExitCode = false,
            Arguments = [ "--treenode-filter", filter, "--report-trx", "--report-trx-filename", trxFileName ]
        });

        var foundTrxFile = context.Git().RootDirectory.FindFile(x => x.Name.EndsWith(trxFileName)).AssertExists();
        
        var parsedResults = await context.Trx().ParseTrxFile(foundTrxFile);

        assertions.ForEach(x => x.Invoke(parsedResults));

        return parsedResults;
    }
}