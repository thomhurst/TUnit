using System.Diagnostics;

namespace TUnit.TestProject.Bugs._3882;

/// <summary>
/// Tests for issue #3882: After Test hook is not run when test is cancelled
/// https://github.com/thomhurst/TUnit/issues/3882
///
/// This test demonstrates that After hooks now execute even when tests are cancelled.
/// The Before hook starts a process, the test delays, and the After hook cleans up the process.
/// When cancelled via Test Explorer or timeout, the After hook should still execute.
/// </summary>
public class CancellationAfterHooksTests
{
    private static readonly string MarkerFileDirectory = Path.Combine(Path.GetTempPath(), "TUnit_3882_Tests");

    [Before(Test)]
    public async Task StartProcess(TestContext context)
    {
        // Create marker directory
        Directory.CreateDirectory(MarkerFileDirectory);

        // Write marker to prove Before hook ran
        var beforeMarker = Path.Combine(MarkerFileDirectory, $"before_{context.Metadata.TestName}.txt");
        await File.WriteAllTextAsync(beforeMarker, $"Before hook executed at {DateTime.Now:O}");
    }

    [Test]
    [Timeout(2000)] // 2 second timeout to force cancellation
    public async Task Test_ThatGets_Cancelled(CancellationToken cancellationToken)
    {
        // This test delays longer than the timeout, causing cancellation
        await Task.Delay(10000, cancellationToken);
    }

    [After(Test)]
    public async Task StopProcess(TestContext context)
    {
        try
        {
            // Write marker to prove After hook ran EVEN ON CANCELLATION
            var afterMarker = Path.Combine(MarkerFileDirectory, $"after_{context.Metadata.TestName}.txt");
            await File.WriteAllTextAsync(afterMarker, $"After hook executed at {DateTime.Now:O} - Outcome: {context.Execution.Result?.State}");
        }
        catch (Exception ex)
        {
            // Don't let marker file creation failure prevent process cleanup
            Console.WriteLine($"[AfterTest] Failed to write marker file: {ex.Message}");
        }
    }
}

/// <summary>
/// Tests for Session-level After hooks with cancellation
/// </summary>
public class SessionLevelCancellationTests
{
    private static readonly string SessionMarkerFile = Path.Combine(Path.GetTempPath(), "TUnit_3882_Session_After.txt");

    [Before(TestSession)]
    public static async Task SessionSetup(TestSessionContext context)
    {
        await File.WriteAllTextAsync(
            Path.Combine(Path.GetTempPath(), "TUnit_3882_Session_Before.txt"),
            $"Session Before hook executed at {DateTime.Now:O}");
    }

    [After(TestSession)]
    public static async Task SessionCleanup(TestSessionContext context)
    {
        // This should run even if tests are cancelled
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
    [Timeout(1000)]
    public async Task SessionTest_ThatGets_Cancelled(CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken);
    }
}

/// <summary>
/// Tests for Assembly-level After hooks with cancellation
/// </summary>
public class AssemblyLevelCancellationTests
{
    private static readonly string AssemblyMarkerFile = Path.Combine(Path.GetTempPath(), "TUnit_3882_Assembly_After.txt");

    [Before(Assembly)]
    public static async Task AssemblySetup(AssemblyHookContext context)
    {
        await File.WriteAllTextAsync(
            Path.Combine(Path.GetTempPath(), "TUnit_3882_Assembly_Before.txt"),
            $"Assembly Before hook executed at {DateTime.Now:O}");
    }

    [After(Assembly)]
    public static async Task AssemblyCleanup(AssemblyHookContext context)
    {
        // This should run even if tests are cancelled
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
    [Timeout(1000)]
    public async Task AssemblyTest_ThatGets_Cancelled(CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken);
    }
}

/// <summary>
/// Tests for Class-level After hooks with cancellation
/// </summary>
public class ClassLevelCancellationTests
{
    private static readonly string ClassMarkerFile = Path.Combine(Path.GetTempPath(), "TUnit_3882_Class_After.txt");

    [Before(Class)]
    public static async Task ClassSetup(ClassHookContext context)
    {
        await File.WriteAllTextAsync(
            Path.Combine(Path.GetTempPath(), "TUnit_3882_Class_Before.txt"),
            $"Class Before hook executed at {DateTime.Now:O}");
    }

    [After(Class)]
    public static async Task ClassCleanup(ClassHookContext context)
    {
        // This should run even if tests are cancelled
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
    [Timeout(1000)]
    public async Task ClassTest_ThatGets_Cancelled(CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken);
    }
}
