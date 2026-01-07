using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using Polly.Retry;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

[DependsOn<GenerateVersionModule>]
[DependsOn<CopyToLocalNuGetModule>]
public class TestAspNetCoreNugetPackageModule : TestBaseModule
{
    protected override AsyncRetryPolicy<IReadOnlyList<CommandResult>?> RetryPolicy
        => CreateRetryPolicy(3);

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

    protected override async Task<DotNetRunOptions> GetTestOptions(IPipelineContext context, string framework,
        CancellationToken cancellationToken)
    {
        var version = await GetModule<GenerateVersionModule>();

        var project = context.Git()
            .RootDirectory
            .AssertExists()
            .FindFile(x => x.Name == "TUnit.AspNetCore.NugetTester.csproj")
            .AssertExists();

        return new DotNetRunOptions
        {
            WorkingDirectory = project.Folder!,
            Framework = framework,
            Properties =
            [
                new KeyValue("TUnitVersion", version.Value!.SemVer!)
            ],
            Arguments =
            [
                "--coverage",
                "--report-trx"
            ]
        };
    }
}
