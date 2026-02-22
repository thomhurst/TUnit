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

[DependsOn<RunMockTestsModule>]
public class PublishAndRunMockTestsAOTModule : Module<IReadOnlyList<CommandResult>>
{
    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithSkipWhen(_ => EnvironmentVariables.IsNetFramework || !RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? SkipDecision.Skip("Only runs on Linux")
            : SkipDecision.DoNotSkip)
        .Build();

    protected override async Task<IReadOnlyList<CommandResult>?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var results = new List<CommandResult>();

        var testProject = context.Git()
            .RootDirectory
            .AssertExists()
            .FindFile(x => x.Name == "TUnit.Mocks.Tests.csproj")
            .AssertExists();

        var rootDir = context.Git().RootDirectory.AssertExists().Path;

        foreach (var framework in new[] { "net8.0", "net9.0", "net10.0" })
        {
            var outputDir = Path.Combine(rootDir, $"MOCKTESTS_AOT_{framework}");

            // Publish with AOT
            var publishResult = await context.SubModule<CommandResult>($"Publish-AOT-{framework}", async () =>
            {
                return await context.DotNet().Publish(new DotNetPublishOptions
                {
                    ProjectSolution = testProject.Path,
                    Runtime = GetRuntimeIdentifier(),
                    Configuration = "Release",
                    Output = outputDir,
                    Properties =
                    [
                        new KeyValue("Aot", "true"),
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

            results.Add(publishResult);

            // Run the published AOT executable
            var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "TUnit.Mocks.Tests.exe"
                : "TUnit.Mocks.Tests";
            var exePath = Path.Combine(outputDir, exeName);

            var runResult = await context.SubModule<CommandResult>($"Run-AOT-{framework}", async () =>
            {
                return await context.Shell.Bash.Command(
                    new BashCommandOptions($"DISABLE_GITHUB_REPORTER=true \"{exePath}\""),
                    cancellationToken);
            });

            results.Add(runResult);
        }

        return results;
    }

    private static string GetRuntimeIdentifier()
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
