namespace TUnit.TestProject;

/// <summary>
/// Driven by TUnit.Engine.Tests' SigtermTests, which sets TUNIT_SIGTERM_MARKER_DIR and sends SIGTERM
/// once the test has started. Without that variable the test returns immediately.
/// </summary>
[Timeout(300_000)]
public class SigtermCancellationTests
{
    private static readonly string? MarkerDirectory = Environment.GetEnvironmentVariable("TUNIT_SIGTERM_MARKER_DIR");

    [Test]
    public async Task Test_That_Runs_Until_Sigterm(CancellationToken cancellationToken)
    {
        if (MarkerDirectory is null)
        {
            return;
        }

        Directory.CreateDirectory(MarkerDirectory);
        await File.WriteAllTextAsync(Path.Combine(MarkerDirectory, "started.txt"), "Test started", cancellationToken);

        await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
    }

    [After(Test)]
    public async Task Slow_Cleanup()
    {
        if (MarkerDirectory is null)
        {
            return;
        }

        // Longer than ProcessExitHookDelay (500ms), so it only completes when SIGTERM is handled as a
        // graceful cancellation rather than left to the runtime's process-exit path.
        await Task.Delay(TimeSpan.FromSeconds(2));
        await File.WriteAllTextAsync(Path.Combine(MarkerDirectory, "after.txt"), "After hook executed");
    }
}
