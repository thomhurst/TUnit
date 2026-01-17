using System.Runtime.InteropServices;
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
            var testResult = await context.SubModule<CommandResult>(framework, async () =>
            {
                var (testOptions, executionOptions) = await GetTestOptions(context, framework, cancellationToken);

                var finalExecutionOptions = SetDefaults(testOptions, executionOptions ?? new CommandExecutionOptions(), framework);

                return await context.DotNet().Run(testOptions, finalExecutionOptions, cancellationToken);
            });

            results.Add(testResult);
        }

        return results;
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
