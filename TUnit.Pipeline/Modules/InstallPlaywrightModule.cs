using ModularPipelines.Context;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules;

public class InstallPlaywrightModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken) =>
        await context.Shell.Bash.Command(
            new BashCommandOptions("npx playwright install --with-deps"),
            cancellationToken);
}
