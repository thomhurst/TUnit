using System.Runtime.InteropServices;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules.Abstract;

public abstract class TestBaseModule : Module<IReadOnlyList<CommandResult>>
{
    protected virtual IEnumerable<string> TestableFrameworks
    {
        get
        {
            yield return "net9.0";
            yield return "net8.0";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return "net472";
            }
        }
    }

    protected override sealed async Task<IReadOnlyList<CommandResult>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var results = new List<CommandResult>();

        foreach (var framework in TestableFrameworks)
        {
            var testResult = await SubModule(framework, async () =>
            {
                var testOptions = SetDefaults(await GetTestOptions(context, framework, cancellationToken));

                return await context.DotNet().Run(testOptions, cancellationToken);
            });

            results.Add(testResult);
        }

        return results;
    }

    private DotNetRunOptions SetDefaults(DotNetRunOptions testOptions)
    {
        // Removed --fail-fast to allow all tests to run even if some fail
        
        if (testOptions.EnvironmentVariables?.Any(x => x.Key == "NET_VERSION") != true)
        {
            testOptions = testOptions with
            {
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    ["NET_VERSION"] = testOptions.Framework,
                }
            };
        }

        return testOptions;
    }

    protected abstract Task<DotNetRunOptions> GetTestOptions(IPipelineContext context, string framework, CancellationToken cancellationToken);
}
