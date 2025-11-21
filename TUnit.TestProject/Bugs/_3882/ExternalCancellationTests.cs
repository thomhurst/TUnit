using System.Diagnostics;

namespace TUnit.TestProject.Bugs._3882;

/// <summary>
/// Tests for issue #3882: After Test hook is not run when test is cancelled EXTERNALLY
/// https://github.com/thomhurst/TUnit/issues/3882
///
/// This test demonstrates that After hooks execute even when tests are cancelled externally
/// (e.g., Ctrl+C, VS Test Explorer Stop button, process termination).
/// The Before hook starts a process, the test delays indefinitely, and the After hook cleans up the process.
/// NOTE: No [Timeout] attribute - these tests only stop via external cancellation.
/// </summary>
public class ExternalCancellationTests
{
    private static readonly string MarkerFileDirectory = Path.Combine(Path.GetTempPath(), "TUnit_3882_External");
    private Process? _process;

    [Before(Test)]
    public async Task StartProcess(TestContext context)
    {
        // Create marker directory
        Directory.CreateDirectory(MarkerFileDirectory);

        // Write marker to prove Before hook ran
        var beforeMarker = Path.Combine(MarkerFileDirectory, $"before_{context.Metadata.TestName}.txt");
        await File.WriteAllTextAsync(beforeMarker, $"Before hook executed at {DateTime.Now:O}");

        // Start a long-running process (ping continuously)
        // Using ping instead of notepad for CI compatibility
        _process = Process.Start(new ProcessStartInfo
        {
            FileName = "ping",
            Arguments = OperatingSystem.IsWindows() ? "-t 127.0.0.1" : "127.0.0.1",
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    [Test]
    // NO [Timeout] attribute - test runs indefinitely until cancelled externally
    public async Task Test_ThatGets_Cancelled_Externally(CancellationToken cancellationToken)
    {
        // This test delays indefinitely, only stops via external cancellation
        await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
    }

    [After(Test)]
    public async Task StopProcess(TestContext context)
    {
        try
        {
            // Write marker to prove After hook ran EVEN ON EXTERNAL CANCELLATION
            var afterMarker = Path.Combine(MarkerFileDirectory, $"after_{context.Metadata.TestName}.txt");
            await File.WriteAllTextAsync(afterMarker, $"After hook executed at {DateTime.Now:O} - Outcome: {context.Execution.Result?.State}");
        }
        catch (Exception ex)
        {
            // Don't let marker file creation failure prevent process cleanup
            Console.WriteLine($"[AfterTest] Failed to write marker file: {ex.Message}");
        }

        // Clean up the process - critical for test isolation
        if (_process != null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);

                    // Wait for process exit with timeout to avoid hanging
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await _process.WaitForExitAsync(cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Process didn't exit in time, but we tried
                Console.WriteLine("[AfterTest] Process kill timed out after 5 seconds");
            }
            catch (Exception ex)
            {
                // Log but don't fail - process might already be gone
                Console.WriteLine($"[AfterTest] Process cleanup warning: {ex.Message}");
            }
            finally
            {
                try
                {
                    _process?.Dispose();
                }
                catch
                {
                    // Best effort disposal
                }
            }
        }
    }
}

/// <summary>
/// Tests for Session-level After hooks with external cancellation
/// </summary>
public class ExternalSessionLevelCancellationTests
{
    private static readonly string SessionMarkerFile = Path.Combine(Path.GetTempPath(), "TUnit_3882_External_Session_After.txt");

    [Before(TestSession)]
    public static async Task SessionSetup(TestSessionContext context)
    {
        await File.WriteAllTextAsync(
            Path.Combine(Path.GetTempPath(), "TUnit_3882_External_Session_Before.txt"),
            $"Session Before hook executed at {DateTime.Now:O}");
    }

    [After(TestSession)]
    public static async Task SessionCleanup(TestSessionContext context)
    {
        // This should run even if tests are cancelled externally
        try
        {
            await File.WriteAllTextAsync(
                SessionMarkerFile,
                $"Session After hook executed at {DateTime.Now:O}");
            Console.WriteLine($"[AfterTestSession] Session After hook completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AfterTestSession] Failed to write marker file: {ex.Message}");
            throw; // Re-throw to signal failure, but after logging
        }
    }

    [Test]
    // NO [Timeout] attribute - test runs indefinitely until cancelled externally
    public async Task SessionTest_ThatGets_Cancelled_Externally(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
    }
}

/// <summary>
/// Tests for Assembly-level After hooks with external cancellation
/// </summary>
public class ExternalAssemblyLevelCancellationTests
{
    private static readonly string AssemblyMarkerFile = Path.Combine(Path.GetTempPath(), "TUnit_3882_External_Assembly_After.txt");

    [Before(Assembly)]
    public static async Task AssemblySetup(AssemblyHookContext context)
    {
        await File.WriteAllTextAsync(
            Path.Combine(Path.GetTempPath(), "TUnit_3882_External_Assembly_Before.txt"),
            $"Assembly Before hook executed at {DateTime.Now:O}");
    }

    [After(Assembly)]
    public static async Task AssemblyCleanup(AssemblyHookContext context)
    {
        // This should run even if tests are cancelled externally
        try
        {
            await File.WriteAllTextAsync(
                AssemblyMarkerFile,
                $"Assembly After hook executed at {DateTime.Now:O}");
            Console.WriteLine($"[AfterAssembly] Assembly After hook completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AfterAssembly] Failed to write marker file: {ex.Message}");
            throw; // Re-throw to signal failure, but after logging
        }
    }

    [Test]
    // NO [Timeout] attribute - test runs indefinitely until cancelled externally
    public async Task AssemblyTest_ThatGets_Cancelled_Externally(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
    }
}

/// <summary>
/// Tests for Class-level After hooks with external cancellation
/// </summary>
public class ExternalClassLevelCancellationTests
{
    private static readonly string ClassMarkerFile = Path.Combine(Path.GetTempPath(), "TUnit_3882_External_Class_After.txt");

    [Before(Class)]
    public static async Task ClassSetup(ClassHookContext context)
    {
        await File.WriteAllTextAsync(
            Path.Combine(Path.GetTempPath(), "TUnit_3882_External_Class_Before.txt"),
            $"Class Before hook executed at {DateTime.Now:O}");
    }

    [After(Class)]
    public static async Task ClassCleanup(ClassHookContext context)
    {
        // This should run even if tests are cancelled externally
        try
        {
            await File.WriteAllTextAsync(
                ClassMarkerFile,
                $"Class After hook executed at {DateTime.Now:O}");
            Console.WriteLine($"[AfterClass] Class After hook completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AfterClass] Failed to write marker file: {ex.Message}");
            throw; // Re-throw to signal failure, but after logging
        }
    }

    [Test]
    // NO [Timeout] attribute - test runs indefinitely until cancelled externally
    public async Task ClassTest_ThatGets_Cancelled_Externally(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
    }
}
