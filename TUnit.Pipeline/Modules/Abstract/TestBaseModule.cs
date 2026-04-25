using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules.Abstract;

public abstract class TestBaseModule : Module<IReadOnlyList<CommandResult>>
{
    protected virtual IEnumerable<string> TestableFrameworks
    {
        get
        {
            yield return "net10.0";
            yield return "net8.0";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return "net472";
            }
        }
    }

    protected sealed override async Task<IReadOnlyList<CommandResult>?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var results = new List<CommandResult>();

        foreach (var framework in TestableFrameworks)
        {
            var (testOptions, executionOptions) = await GetTestOptions(context, framework, cancellationToken);

            // Test projects no longer multi-target every TFM by default (see TestProject.props).
            // Skip frameworks that this project did not actually build to avoid spurious
            // "process cannot find the file" errors for missing per-TFM output binaries.
            var configuration = testOptions.Configuration ?? "Release";
            if (!HasFrameworkOutput(context, executionOptions, framework, configuration))
            {
                context.Logger.LogInformation("Skipping {Framework}: no build output found for this test project.", framework);
                continue;
            }

            var testResult = await context.SubModule<CommandResult>(framework, async () =>
            {
                var finalExecutionOptions = SetDefaults(testOptions, executionOptions ?? new CommandExecutionOptions(), framework);

                return await context.DotNet().Run(testOptions, finalExecutionOptions, cancellationToken);
            });

            results.Add(testResult);
        }

        return results;
    }

    private static bool HasFrameworkOutput(IModuleContext context, CommandExecutionOptions? executionOptions, string framework, string configuration)
    {
        var workingDirectory = executionOptions?.WorkingDirectory;
        if (string.IsNullOrEmpty(workingDirectory))
        {
            // Cannot determine — fall through to attempt the run (preserves prior behaviour).
            context.Logger.LogWarning("Cannot probe build output for {Framework}: no WorkingDirectory set on execution options.", framework);
            return true;
        }

        var binPath = Path.Combine(workingDirectory, "bin", configuration, framework);
        return Directory.Exists(binPath);
    }

    private CommandExecutionOptions SetDefaults(DotNetRunOptions testOptions, CommandExecutionOptions executionOptions, string framework)
    {
        var envVars = executionOptions.EnvironmentVariables ?? new Dictionary<string, string?>();
        if (!envVars.ContainsKey("NET_VERSION"))
        {
            envVars = new Dictionary<string, string?>(envVars)
            {
                ["NET_VERSION"] = framework
            };
        }

        // Suppress output for successful operations, but show errors and basic info
        return executionOptions with
        {
            EnvironmentVariables = envVars,
            LogSettings = new CommandLoggingOptions
            {
                ShowCommandArguments = true,
                ShowStandardError = true,
                ShowExecutionTime = true,
                ShowExitCode = true
            }
        };
    }

    protected abstract Task<(DotNetRunOptions Options, CommandExecutionOptions? ExecutionOptions)> GetTestOptions(IModuleContext context, string framework, CancellationToken cancellationToken);
}
