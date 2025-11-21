using CliWrap;
using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Validates that After hooks execute even when tests are cancelled EXTERNALLY (Issue #3882).
/// These tests start the test process asynchronously, cancel it mid-execution (simulating Ctrl+C or Stop button),
/// and verify that After hooks still execute by checking for marker files.
/// </summary>
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

        var cts = new CancellationTokenSource();

        var command = testMode switch
        {
            TestMode.SourceGenerated => Cli.Wrap("dotnet")
                .WithArguments(
                [
                    "run",
                    "--no-build",
                    "-f", GetEnvironmentVariable,
                    "--configuration", "Release",
                    "--treenode-filter", filter,
                    "--report-trx", "--report-trx-filename", trxFilename,
                    "--diagnostic-verbosity", "Debug",
                    "--diagnostic", "--diagnostic-file-prefix", $"log_ExternalCancellation_{GetType().Name}_",
                ])
                .WithWorkingDirectory(testProject.DirectoryName!)
                .WithValidation(CommandResultValidation.None),

            TestMode.Reflection => Cli.Wrap("dotnet")
                .WithArguments(
                [
                    "run",
                    "--no-build",
                    "-f", GetEnvironmentVariable,
                    "--configuration", "Release",
                    "--treenode-filter", filter,
                    "--report-trx", "--report-trx-filename", trxFilename,
                    "--diagnostic-verbosity", "Debug",
                    "--diagnostic", "--diagnostic-file-prefix", $"log_ExternalCancellation_{GetType().Name}_",
                    "--reflection"
                ])
                .WithWorkingDirectory(testProject.DirectoryName!)
                .WithValidation(CommandResultValidation.None),

            // Skip AOT and SingleFile modes for external cancellation (only test in CI)
            TestMode.AOT => null!,
            TestMode.SingleFileApplication => null!,
            _ => throw new ArgumentOutOfRangeException(nameof(testMode), testMode, null)
        };

        // Skip AOT and SingleFile modes
        if (command == null)
        {
            return;
        }

        var executeTask = command.ExecuteAsync(cts.Token);

        try
        {
            // Wait for process to start and hooks to register
            // This ensures Before hooks have executed and After hooks are registered via CancellationToken.Register()
            await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);

            // Cancel externally (simulates Ctrl+C, VS Test Explorer Stop button)
            // This should trigger the CancellationToken.Register() callbacks that execute After hooks
            Console.WriteLine($"[ExternalCancellation] Cancelling test process for filter: {filter}");
            cts.Cancel();

            // Wait for the process to terminate (expected to throw OperationCanceledException)
            await executeTask;
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

        // Wait for After hooks to complete
        // After hooks execute via CancellationToken.Register() callbacks, which may take a moment
        // Need generous time for: signal delivery -> cancellation -> After hooks -> file write -> process termination
        await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);

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
