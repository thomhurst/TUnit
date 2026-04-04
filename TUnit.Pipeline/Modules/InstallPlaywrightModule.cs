using ModularPipelines.Context;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules;

public class InstallPlaywrightModule : Module<CommandResult>
{
    protected override Task<CommandResult?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        // If browsers were restored from cache, skip system dependency installation
        var command = IsBrowserCachePresent()
            ? "npx playwright install"
            : "npx playwright install --with-deps";

        return context.Shell.Bash.Command(
            new BashCommandOptions(command),
            cancellationToken);
    }

    private static bool IsBrowserCachePresent()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] cachePaths =
        [
            Path.Combine(home, ".cache", "ms-playwright"),             // Linux
            Path.Combine(home, "Library", "Caches", "ms-playwright"), // macOS
            Path.Combine(home, "AppData", "Local", "ms-playwright"),  // Windows
        ];

        return cachePaths.Any(p => Directory.Exists(p) && Directory.EnumerateDirectories(p).Any());
    }
}
