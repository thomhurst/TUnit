using System.Runtime.InteropServices;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules;

[DependsOn<RunMockTestsModule>]
public class RunMockTestsAOTModule : Module<IReadOnlyList<CommandResult>>
{
    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithSkipWhen(_ => EnvironmentVariables.IsNetFramework || !RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? SkipDecision.Skip("Only runs on Linux")
            : SkipDecision.DoNotSkip)
        .Build();

    protected override async Task<IReadOnlyList<CommandResult>?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var results = new List<CommandResult>();

        var rootDir = context.Git().RootDirectory.AssertExists().Path;

        foreach (var framework in new[] { "net8.0", "net9.0", "net10.0" })
        {
            var exePath = Path.Combine(rootDir, $"MOCKTESTS_AOT_{framework}", "TUnit.Mocks.Tests");

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
}
