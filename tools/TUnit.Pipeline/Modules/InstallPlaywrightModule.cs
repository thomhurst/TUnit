using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules;

public class InstallPlaywrightModule : Module<CommandResult>
{
    // Browser download is ~500 MB. 10 minutes is far past the happy-path
    // (~2 min on a warm runner) but short enough that a hung connection gets
    // killed and retried instead of burning the module's outer 30-min budget.
    private static readonly TimeSpan PerAttemptTimeout = TimeSpan.FromMinutes(10);

    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithRetryCount(2)
        .Build();

    protected override async Task<CommandResult?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        attemptCts.CancelAfter(PerAttemptTimeout);

        return await context.Shell.Bash.Command(
            new BashCommandOptions("npx playwright install --with-deps"),
            attemptCts.Token);
    }
}
