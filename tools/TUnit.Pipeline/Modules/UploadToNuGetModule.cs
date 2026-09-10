using Microsoft.Extensions.Options;
using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git.Attributes;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules;

[RunOnlyOnBranch("main")]
[RunOnLinuxOnly]
[DependsOn<GenerateVersionModule>]
[DependsOn<PackTUnitFilesModule>]
[DependsOn<TestNugetPackageModule>]
[DependsOn<RunEngineTestsModule>]
[DependsOn<TestFSharpNugetPackageModule>]
[DependsOn<TestVBNugetPackageModule>]
public class UploadToNuGetModule(IOptions<NuGetOptions> options) : Module<CommandResult[]>
{
    // Local builds pin this dummy version (see Directory.Build.props). Pushing it to NuGet.org
    // would burn the version and ship an unbuildable package, so guard against it explicitly.
    private const string DummyLocalVersion = "99.99.99";

    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithSkipWhen(_ =>
        {
            if (!options.Value.ShouldPublish)
            {
                return SkipDecision.Skip("Should Publish is false");
            }

            if (string.IsNullOrEmpty(options.Value.ApiKey))
            {
                return SkipDecision.Skip("No API key found");
            }

            return SkipDecision.DoNotSkip;
        })
        .Build();

    protected override async Task<CommandResult[]?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var versionResult = await context.GetModule<GenerateVersionModule>();
        var semVer = versionResult.ValueOrDefault?.SemVer;

        if (string.IsNullOrWhiteSpace(semVer) || semVer.StartsWith(DummyLocalVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Refusing to publish to NuGet: resolved version '{semVer}' is missing or is the dummy local version '{DummyLocalVersion}'. " +
                "This indicates the CI versioning step did not run; aborting to avoid shipping a bad package.");
        }

        var nupkgs = context.Git().RootDirectory
            .GetFiles(x => x.NameWithoutExtension.Contains("TUnit") && x.Extension is ".nupkg")
            .ToArray();

        if (nupkgs.Any(file => file.NameWithoutExtension.Contains(DummyLocalVersion)))
        {
            throw new InvalidOperationException(
                $"Refusing to publish to NuGet: found a package stamped with the dummy local version '{DummyLocalVersion}'.");
        }

        var results = new CommandResult[nupkgs.Length];

        for (var i = 0; i < nupkgs.Length; i++)
        {
            results[i] = await context.DotNet().Nuget.Push(new DotNetNugetPushOptions
            {
                Path = nupkgs[i].Path,
                Source = "https://api.nuget.org/v3/index.json",
                ApiKey = options.Value.ApiKey,
            }, new CommandExecutionOptions
            {
                LogSettings = new CommandLoggingOptions
                {
                    ShowCommandArguments = true,
                    ShowStandardError = true,
                    ShowExecutionTime = true,
                    ShowExitCode = true
                }
            }, cancellationToken);
        }

        return results;
    }
}
