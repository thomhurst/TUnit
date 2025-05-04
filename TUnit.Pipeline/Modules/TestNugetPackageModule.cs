using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

[DependsOn<GenerateVersionModule>]
[DependsOn<CopyToLocalNuGetModule>]
public class TestNugetPackageModule : TestBaseModule
{
    protected override IEnumerable<string> TestableFrameworks
    {
        get
        {
            yield return "net9.0";
            yield return "net8.0";
            yield return "net7.0";
            yield return "net6.0";
        
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return "net481";
                yield return "net48";
                yield return "net472";
                yield return "net462";
            }   
        }
    }

    protected override async Task<DotNetRunOptions> GetTestOptions(IPipelineContext context, string framework,
        CancellationToken cancellationToken)
    {
        var version = await GetModule<GenerateVersionModule>();

        var project = context.Git()
            .RootDirectory
            .AssertExists()
            .FindFile(x => x.Name == "TUnit.NugetTester.csproj")
            .AssertExists();

        return new DotNetRunOptions
        {
            Project = project,
            Framework = framework,
            Properties =
            [
                new KeyValue("TUnitVersion", version.Value!.SemVer!)
            ]
        };
    }
}
