using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules;

public class InstallPlaywrightModule : Module<CommandResult>
{
    // Browser download is ~500 MB. 10 minutes is far past the happy-path
    // (~2 min on a warm runner) but short enough that a hung connection gets
    // canceled and retried instead of burning the module's 30-minute per-attempt budget.
    private static readonly TimeSpan PerAttemptTimeout = TimeSpan.FromMinutes(10);

    protected override void Configure(ModuleConfigurationBuilder module) => module
        .WithRetry(2);

    protected override async Task<CommandResult> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsLinux())
        {
            // Other package installations can share the runner's dpkg lock.
            // Make apt wait for the lock instead of failing the browser installation.
            await context.Shell.Bash.RunAsync(
                new BashCommandOptions("echo 'DPkg::Lock::Timeout \"300\";' | sudo tee /etc/apt/apt.conf.d/99-tunit-lock-timeout"),
                cancellationToken: cancellationToken);
        }

        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        attemptCts.CancelAfter(PerAttemptTimeout);

        // Only Chromium is used by the Playwright tests. Installing every browser
        // pulls in the WebKit/Firefox GStreamer apt dependencies, which can take
        // longer than the per-attempt timeout on its own.
        // Use the installer shipped by the resolved Microsoft.Playwright package,
        // rather than npm latest, so downloaded browsers match the test driver.
        var script = Path.Combine((await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken)).Root.Path,
            "src", "TUnit.Templates", "content", "TUnit.Playwright", "bin", "Release", "net10.0", "playwright.ps1");
        // Bash also runs on Windows; forward slashes and shell quoting preserve paths
        // containing spaces or apostrophes on every runner.
        var quotedScript = "'" + script.Replace('\\', '/').Replace("'", "'\"'\"'") + "'";
        return await context.Shell.Bash.RunAsync(
            new BashCommandOptions($"pwsh -NoProfile -File {quotedScript} install --with-deps chromium"),
            cancellationToken: attemptCts.Token);
    }
}
