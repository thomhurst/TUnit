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

        var trxFile = File.GetNewTemporaryFilePath();
        
        await context.DotNet().Test(new DotNetTestOptions(project)
        {
            NoBuild = true,
            Filter = filter,
            ThrowOnNonZeroExitCode = false,
            Logger = new[] { $"trx;LogFileName={trxFile}" },
        });

        var parsedResults = await context.Trx().ParseTrxFile(trxFile);

        assertions.ForEach(x => x.Invoke(parsedResults));

        return parsedResults;
    }
}