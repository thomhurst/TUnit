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
        get { yield return "net10.0"; }
    }

    /// <summary>True on Windows, where legacy .NET Framework TFMs (net4xx) can be tested.</summary>
    protected static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

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

        // Probe for an actual built binary, not just the directory: a stale empty
        // bin/<config>/<tfm> folder (e.g. from `dotnet clean`) would otherwise be treated
        // as a successful build and trigger a misleading "process cannot find the file" later.
        try
        {
            return Directory.EnumerateFiles(binPath, "*.dll", SearchOption.TopDirectoryOnly).Any()
                || Directory.EnumerateFiles(binPath, "*.exe", SearchOption.TopDirectoryOnly).Any();
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
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

    /// <summary>
    /// Called once per framework in <see cref="TestableFrameworks"/>, <em>before</em> the
    /// missing-output skip check. Keep this cheap — expensive work (e.g. awaiting other modules)
    /// is wasted on TFMs the project did not build for.
    /// </summary>
    protected abstract Task<(DotNetRunOptions Options, CommandExecutionOptions? ExecutionOptions)> GetTestOptions(IModuleContext context, string framework, CancellationToken cancellationToken);
}
