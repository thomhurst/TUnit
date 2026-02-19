using System.Diagnostics;
using CliWrap;
using Shouldly;
using TUnit.Core.Enums;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Validates that After hooks execute even when tests are cancelled EXTERNALLY (Issue #3882).
/// These tests start the test process asynchronously, cancel it mid-execution (simulating Ctrl+C or Stop button),
/// and verify that After hooks still execute by checking for marker files.
/// </summary>
/// <remarks>
/// These tests are skipped on Windows because CliWrap's graceful cancellation uses GenerateConsoleCtrlEvent,
/// which doesn't work reliably for child processes with their own console.
/// See: https://github.com/Tyrrrz/CliWrap/issues/47
/// </remarks>
[ExcludeOn(OS.Windows)]
[Retry(3)]
public class ExternalCancellationTests(TestMode testMode) : InvokableTestBase(testMode)
{
    private static readonly string TempPath = Path.GetTempPath();
    private static readonly string GetEnvironmentVariable = Environment.GetEnvironmentVariable("NET_VERSION") ?? "net10.0";

    /// <summary>
    /// Runs a test with external cancellation (simulates Ctrl+C, VS Test Explorer Stop button).
    /// </summary>
    /// <param name="filter">Test filter pattern</param>
    /// <param name="markerFile">Path to the marker file that proves After hook executed</param>
    /// <param name="expectedMarkerContent">Expected content in the marker file</param>
    private async Task RunTestWithExternalCancellation(string filter, string markerFile, string expectedMarkerContent)
    {
        // Clean up any existing marker files
        if (File.Exists(markerFile))
        {
            File.Delete(markerFile);
        }

        var testProject = Sourcy.DotNet.Projects.TUnit_TestProject;
        var guid = Guid.NewGuid().ToString("N");
        var trxFilename = guid + ".trx";

        using var gracefulCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var forcefulCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(25));

        // Use cross-platform executable detection (Linux: no extension, Windows: .exe)
        var binDir = new DirectoryInfo(Path.Combine(testProject.DirectoryName!, "bin", "Release", GetEnvironmentVariable));
        var file = binDir.GetFiles("TUnit.TestProject").FirstOrDefault()?.FullName
                   ?? binDir.GetFiles("TUnit.TestProject.exe").First().FullName;

        var command = testMode switch
        {
            TestMode.SourceGenerated => Cli.Wrap(file)
                .WithArguments(
                [
                    "--treenode-filter", filter,
                    "--report-trx", "--report-trx-filename", trxFilename,
                    "--diagnostic-verbosity", "Debug",
                    "--diagnostic", "--diagnostic-file-prefix", $"log_ExternalCancellation_{GetType().Name}_",
                ])
                .WithWorkingDirectory(testProject.DirectoryName!)
                .WithValidation(CommandResultValidation.None),

            TestMode.Reflection => Cli.Wrap(file)
                .WithArguments(
                [
                    "--treenode-filter", filter,
                    "--report-trx", "--report-trx-filename", trxFilename,
                    "--diagnostic-verbosity", "Debug",
                    "--diagnostic", "--diagnostic-file-prefix", $"log_ExternalCancellation_{GetType().Name}_",
                    "--reflection"
                ])
                .WithWorkingDirectory(testProject.DirectoryName!)
                .WithValidation(CommandResultValidation.None),

            // Skip AOT mode for external cancellation (only test in CI)
            TestMode.AOT => null,
            _ => throw new ArgumentOutOfRangeException(nameof(testMode), testMode, null)
        };

        // Skip AOT mode
        if (command == null)
        {
            return;
        }

        try
        {
            await command.ExecuteAsync(forcefulCancellationTokenSource.Token, gracefulCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected: Process was cancelled via CancellationToken
            Console.WriteLine("[ExternalCancellation] Process cancelled successfully (expected)");
        }
        catch (Exception ex)
        {
            // Log unexpected exceptions but don't fail - After hooks might still execute
            Console.WriteLine($"[ExternalCancellation] Unexpected exception: {ex.Message}");
        }

        // Verify marker file exists - this proves After hook executed even on external cancellation
        File.Exists(markerFile).ShouldBeTrue($"After hook marker file should exist at {markerFile}");

        // Verify marker file content
        var content = await File.ReadAllTextAsync(markerFile);
        content.ShouldContain(expectedMarkerContent);
    }

    [Test]
    public async Task TestLevel_AfterHook_Runs_OnExternalCancellation()
    {
        var afterMarkerFile = Path.Combine(TempPath, "TUnit_3882_External", "after_Test_ThatGets_Cancelled_Externally.txt");

        await RunTestWithExternalCancellation(
            "/*/*/ExternalCancellationTests/*",
            afterMarkerFile,
            "After hook executed");
    }

    [Test]
    public async Task SessionLevel_AfterHook_Runs_OnExternalCancellation()
    {
        var afterMarkerFile = Path.Combine(TempPath, "TUnit_3882_External_Session_After.txt");

        await RunTestWithExternalCancellation(
            "/*/*/ExternalSessionLevelCancellationTests/*",
            afterMarkerFile,
            "Session After hook executed");
    }

    [Test]
    public async Task AssemblyLevel_AfterHook_Runs_OnExternalCancellation()
    {
        var afterMarkerFile = Path.Combine(TempPath, "TUnit_3882_External_Assembly_After.txt");

        await RunTestWithExternalCancellation(
            "/*/*/ExternalAssemblyLevelCancellationTests/*",
            afterMarkerFile,
            "Assembly After hook executed");
    }

    [Test]
    public async Task ClassLevel_AfterHook_Runs_OnExternalCancellation()
    {
        var afterMarkerFile = Path.Combine(TempPath, "TUnit_3882_External_Class_After.txt");

        await RunTestWithExternalCancellation(
            "/*/*/ExternalClassLevelCancellationTests/*",
            afterMarkerFile,
            "Class After hook executed");
    }
}
