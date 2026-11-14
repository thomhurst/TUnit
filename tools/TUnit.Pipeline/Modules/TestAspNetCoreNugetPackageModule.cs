using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.FileSystem;
using ModularPipelines.Options;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

[ModuleCategory(PipelineCategories.NuGetPackageTests)]
[DependsOn<GenerateVersionModule>]
[DependsOn<CopyToLocalNuGetModule>]
public class TestAspNetCoreNugetPackageModule : TestBaseModule
{
    protected override void Configure(ModuleConfigurationBuilder module) => module
        .WithRetry(3);

    // ASP.NET Core only supports .NET Core frameworks, not .NET Framework
    protected override IEnumerable<string> TestableFrameworks => ModernFrameworks;

    protected override async Task<(DotNetRunOptions Options, CommandExecutionOptions? ExecutionOptions)> GetTestOptions(IModuleContext context, string framework,
        CancellationToken cancellationToken)
    {
        var version = await context.GetModule<GenerateVersionModule>();

        var project = (await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken)).Root
            .AssertExists()
            .FindFile(x => x.Name == "TUnit.AspNetCore.NugetTester.csproj")
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
}
