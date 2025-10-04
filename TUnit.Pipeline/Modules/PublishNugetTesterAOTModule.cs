using System.Runtime.InteropServices;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Enums;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

[DependsOn<TestNugetPackageModule>]
[DependsOn<GenerateVersionModule>]
[DependsOn<CopyToLocalNuGetModule>]
public class PublishNugetTesterAOTModule : Module<IReadOnlyList<CommandResult>>
{
    protected override Task<SkipDecision> ShouldSkip(IPipelineContext context)
    {
        return Task.FromResult<SkipDecision>(EnvironmentVariables.IsNetFramework);
    }

    protected override async Task<IReadOnlyList<CommandResult>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var results = new List<CommandResult>();
        var version = await GetModule<GenerateVersionModule>();

        var testProject = context.Git()
            .RootDirectory
            .AssertExists()
            .FindFile(x => x.Name == "TUnit.NugetTester.csproj")
            .AssertExists();

        // Test AOT publishing for net8.0 and net9.0
        foreach (var framework in new[] { "net8.0", "net9.0" })
        {
            var result = await SubModule($"AOT-{framework}", async () =>
            {
                return await context.DotNet().Publish(new DotNetPublishOptions(testProject)
                {
                    RuntimeIdentifier = GetRuntimeIdentifier(),
                    Configuration = Configuration.Release,
                    OutputDirectory = $"NUGETTESTER_AOT_{framework}",
                    Properties =
                    [
                        new KeyValue("Aot", "true"),
                        new KeyValue("TUnitVersion", version.Value!.SemVer!)
                    ],
                    Framework = framework,
                    CommandLogging = CommandLogging.Input | CommandLogging.Error | CommandLogging.Duration | CommandLogging.ExitCode
                }, cancellationToken);
            });

            results.Add(result);
        }

        return results;
    }

    private string GetRuntimeIdentifier()
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
            return "osx-arm64";
        }

        throw new ArgumentException("Unknown platform");
    }
}
