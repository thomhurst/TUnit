using System.Runtime.InteropServices;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

public class PublishAOTModule : Module<CommandResult>
{
    public override ModuleRunType ModuleRunType => ModuleRunType.AlwaysRun;
    
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var testProject = context.Git().RootDirectory!.FindFile(x => x.Name == "TUnit.TestProject.csproj").AssertExists();

        return await context.DotNet().Publish(new DotNetPublishOptions(testProject)
        {
            RuntimeIdentifier = GetRuntimeIdentifier(),
            Configuration = Configuration.Release,
            OutputDirectory = "TESTPROJECT_AOT",
            Properties = [new KeyValue("Aot", "true")],
            Framework = "net8.0"
        }, cancellationToken);
    }

    private string? GetRuntimeIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux-x64";
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win-x64";
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx-x64";
        }

        throw new ArgumentException("Unknown platform");
    }
}