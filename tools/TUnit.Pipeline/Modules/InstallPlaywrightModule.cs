using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.Git.Extensions;
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
        if (OperatingSystem.IsLinux())
        {
            // Cancelling a timed-out attempt kills bash but not the sudo'd apt-get
            // that Playwright spawns, so the retry fails instantly on the dpkg lock.
            // Make apt wait for the lock instead.
            await context.Shell.Bash.Command(
                new BashCommandOptions("echo 'DPkg::Lock::Timeout \"300\";' | sudo tee /etc/apt/apt.conf.d/99-tunit-lock-timeout"),
                cancellationToken);
        }

        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        attemptCts.CancelAfter(PerAttemptTimeout);

        // Only Chromium is used by the Playwright tests. Installing every browser
        // pulls in the WebKit/Firefox GStreamer apt dependencies, which can take
        // longer than the per-attempt timeout on its own.
        // Use the installer shipped by the resolved Microsoft.Playwright package,
        // rather than npm latest, so downloaded browsers match the test driver.
        var script = Path.Combine(context.Git().RootDirectory.Path,
            "src", "TUnit.Templates", "content", "TUnit.Playwright", "bin", "Release", "net10.0", "playwright.ps1");
        // Bash also runs on Windows; forward slashes and shell quoting preserve paths
        // containing spaces or apostrophes on every runner.
        var quotedScript = "'" + script.Replace('\\', '/').Replace("'", "'\"'\"'") + "'";
        return await context.Shell.Bash.Command(
            new BashCommandOptions($"pwsh -NoProfile -File {quotedScript} install --with-deps chromium"),
            attemptCts.Token);
    }
}
