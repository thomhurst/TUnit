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

[DependsOn<GenerateVersionModule>]
[DependsOn<CopyToLocalNuGetModule>]
public class TestAspNetCoreNugetPackageModule : TestBaseModule
{
    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithRetryCount(3)
        .Build();

    // ASP.NET Core only supports .NET Core frameworks, not .NET Framework
    protected override IEnumerable<string> TestableFrameworks
    {
        get
        {
            yield return "net10.0";
            yield return "net9.0";
            yield return "net8.0";
        }
    }

    protected override async Task<(DotNetRunOptions Options, CommandExecutionOptions? ExecutionOptions)> GetTestOptions(IModuleContext context, string framework,
        CancellationToken cancellationToken)
    {
        var version = await context.GetModule<GenerateVersionModule>();

        var project = context.Git()
            .RootDirectory
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
