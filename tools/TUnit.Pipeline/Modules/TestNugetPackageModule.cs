using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.FileSystem;
using ModularPipelines.Options;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

[ModuleCategory(PipelineCategories.NuGetPackageTests)]
[NotInParallel("NugetTester")]
public class TestNugetPackageModule : AbstractTestNugetPackageModule
{
    public override string ProjectName => "TUnit.NugetTester.csproj";
}

[ModuleCategory(PipelineCategories.NuGetPackageTests)]
[NotInParallel("NugetTester")]
public class TestFSharpNugetPackageModule : AbstractTestNugetPackageModule
{
    public override string ProjectName => "TUnit.NugetTester.FSharp.fsproj";
}

[ModuleCategory(PipelineCategories.NuGetPackageTests)]
[NotInParallel("NugetTester")]
public class TestVBNugetPackageModule : AbstractTestNugetPackageModule
{
    public override string ProjectName => "TUnit.NugetTester.VB.vbproj";
}

[DependsOn<GenerateVersionModule>]
[DependsOn<CopyToLocalNuGetModule>]
public abstract class AbstractTestNugetPackageModule : TestBaseModule
{
    protected override void Configure(ModuleConfigurationBuilder module) => module
        .WithRetry(3);

    protected override IEnumerable<string> TestableFrameworks
    {
        get
        {
            yield return "net10.0";

            // See ModernFrameworks: only Linux covers net8.0.
            if (IsLinux)
            {
                yield return "net8.0";
            }

            if (IsWindows)
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

        var project = (await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken)).Root
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
