using System.Runtime.InteropServices;
using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules;

[DependsOn<TestNugetPackageModule>]
[DependsOn<GenerateVersionModule>]
[DependsOn<CopyToLocalNuGetModule>]
public class PublishNugetTesterAOTModule : Module<IReadOnlyList<CommandResult>>
{
    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithSkipWhen(_ => EnvironmentVariables.IsNetFramework
            ? SkipDecision.Skip("Running on .NET Framework")
            : SkipDecision.DoNotSkip)
        .Build();

    protected override async Task<IReadOnlyList<CommandResult>?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var results = new List<CommandResult>();
        var version = await context.GetModule<GenerateVersionModule>();

        var testProject = context.Git()
            .RootDirectory
            .AssertExists()
            .FindFile(x => x.Name == "TUnit.NugetTester.csproj")
            .AssertExists();

        // Test AOT publishing
        foreach (var framework in new[] { "net8.0", "net9.0", "net10.0" })
        {
            var result = await context.SubModule<CommandResult>($"AOT-{framework}", async () =>
            {
                return await context.DotNet().Publish(new DotNetPublishOptions
                {
                    ProjectSolution = testProject.Path,
                    Runtime = GetRuntimeIdentifier(),
                    Configuration = "Release",
                    Output = $"NUGETTESTER_AOT_{framework}",
                    Properties =
                    [
                        new KeyValue("Aot", "true"),
                        new KeyValue("TUnitVersion", version.ValueOrDefault!.SemVer!)
                    ],
                    Framework = framework,
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
