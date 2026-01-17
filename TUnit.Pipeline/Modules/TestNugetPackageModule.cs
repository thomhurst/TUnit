using System.Runtime.InteropServices;
using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Options;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

public class TestNugetPackageModule : AbstractTestNugetPackageModule
{
    public override string ProjectName => "TUnit.NugetTester.csproj";
}

[RunOnWindowsOnly, RunOnLinuxOnly]
public class TestFSharpNugetPackageModule : AbstractTestNugetPackageModule
{
    public override string ProjectName => "TUnit.NugetTester.FSharp.fsproj";
}

[RunOnWindowsOnly, RunOnLinuxOnly]
public class TestVBNugetPackageModule : AbstractTestNugetPackageModule
{
    public override string ProjectName => "TUnit.NugetTester.VB.vbproj";
}

[DependsOn<GenerateVersionModule>]
[DependsOn<CopyToLocalNuGetModule>]
public abstract class AbstractTestNugetPackageModule : TestBaseModule
{
    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithRetryCount(3)
        .Build();

    protected override IEnumerable<string> TestableFrameworks
    {
        get
        {
            yield return "net10.0";
            yield return "net8.0";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return "net481";
                yield return "net48";
                yield return "net472";
                yield return "net462";
            }
        }
    }

    protected override async Task<(DotNetRunOptions Options, CommandExecutionOptions? ExecutionOptions)> GetTestOptions(IModuleContext context, string framework,
        CancellationToken cancellationToken)
    {
        var version = await context.GetModule<GenerateVersionModule>();

        var project = context.Git()
            .RootDirectory
            .AssertExists()
            .FindFile(x => x.Name == ProjectName)
            .AssertExists();

        return (
            new DotNetRunOptions
            {
                Framework = framework,
                Properties =
                [
                    new KeyValue("TUnitVersion", version.ValueOrDefault!.SemVer!)
                ],
                Arguments =
                [
                    "--coverage",
                    "--report-trx"
                ]
            },
            new CommandExecutionOptions
            {
                WorkingDirectory = project.Folder!.Path,
            }
        );
    }

    public abstract string ProjectName { get; }
}
