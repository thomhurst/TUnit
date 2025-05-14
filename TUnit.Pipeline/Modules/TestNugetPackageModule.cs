using System.Runtime.InteropServices;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

public class TestNugetPackageModule : AbstractTestNugetPackageModule
{
    public override string ProjectName => "TUnit.NugetTester.csproj";
}

public class TestFSharpNugetPackageModule : AbstractTestNugetPackageModule
{
    public override string ProjectName => "TUnit.NugetTester.FSharp.fsproj";
}

public class TestVBNugetPackageModule : AbstractTestNugetPackageModule
{
    public override string ProjectName => "TUnit.NugetTester.VB.vbproj";
}

[DependsOn<GenerateVersionModule>]
[DependsOn<CopyToLocalNuGetModule>]
public abstract class AbstractTestNugetPackageModule : TestBaseModule
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
            .FindFile(x => x.Name == ProjectName)
            .AssertExists();

        return new DotNetRunOptions
        {
            WorkingDirectory = project.Folder!,
            Framework = framework,
            Properties =
            [
                new KeyValue("TUnitVersion", version.Value!.SemVer!)
            ]
        };
    }

    public abstract string ProjectName { get; }
}
