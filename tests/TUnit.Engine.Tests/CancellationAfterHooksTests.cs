using Shouldly;
using TUnit.Engine.Tests.Enums;
using TUnit.Engine.Tests.Extensions;

namespace TUnit.Engine.Tests;

/// <summary>
/// Validates that After hooks execute even when tests are cancelled (Issue #3882).
/// These tests run the cancellation test scenarios and verify that After hooks created marker files.
/// </summary>
[Retry(3)]
public class CancellationAfterHooksTests(TestMode testMode) : InvokableTestBase(testMode)
{
    private static readonly string TempPath = Path.GetTempPath();

    [Test]
    public async Task TestLevel_AfterHook_Runs_OnCancellation()
    {
        var afterMarkerFile = Path.Combine(TempPath, "TUnit_3882_Tests", "after_Test_ThatGets_Cancelled.txt");

        // Clean up any existing marker files
        if (File.Exists(afterMarkerFile))
        {
            File.Delete(afterMarkerFile);
        }

        await RunTestsWithFilter(
            "/*/*/CancellationAfterHooksTests/*",
            [
                // Test run completes even though the test itself fails (timeout is expected)
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                // Test should fail due to timeout
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                // After hook should have created the marker file - this proves After hooks ran on cancellation
                _ => File.Exists(afterMarkerFile).ShouldBeTrue($"After hook marker file should exist at {afterMarkerFile}")
            ]);

        // Verify marker file content
        if (File.Exists(afterMarkerFile))
        {
            var content = await File.ReadAllTextAsync(afterMarkerFile);
            content.ShouldContain("After hook executed");
        }
    }

    [Test]
    public async Task SessionLevel_AfterHook_Runs_OnCancellation()
    {
        var afterMarkerFile = Path.Combine(TempPath, "TUnit_3882_Session_After.txt");

        // Clean up any existing marker files
        if (File.Exists(afterMarkerFile))
        {
            File.Delete(afterMarkerFile);
        }

        await RunTestsWithFilter(
            "/*/*/SessionLevelCancellationTests/*",
            [
                // After Session hook should have created the marker file - this proves Session After hooks ran on cancellation
                _ => File.Exists(afterMarkerFile).ShouldBeTrue($"Session After hook marker file should exist at {afterMarkerFile}")
            ]);

        // Verify marker file content
        if (File.Exists(afterMarkerFile))
        {
            var content = await File.ReadAllTextAsync(afterMarkerFile);
            content.ShouldContain("Session After hook executed");
        }
    }

    [Test]
    public async Task AssemblyLevel_AfterHook_Runs_OnCancellation()
    {
        var afterMarkerFile = Path.Combine(TempPath, "TUnit_3882_Assembly_After.txt");

        // Clean up any existing marker files
        if (File.Exists(afterMarkerFile))
        {
            File.Delete(afterMarkerFile);
        }

        await RunTestsWithFilter(
            "/*/*/AssemblyLevelCancellationTests/*",
            [
                // After Assembly hook should have created the marker file - this proves Assembly After hooks ran on cancellation
                _ => File.Exists(afterMarkerFile).ShouldBeTrue($"Assembly After hook marker file should exist at {afterMarkerFile}")
            ]);

        // Verify marker file content
        if (File.Exists(afterMarkerFile))
        {
            var content = await File.ReadAllTextAsync(afterMarkerFile);
            content.ShouldContain("Assembly After hook executed");
        }
    }

    [Test]
    public async Task ClassLevel_AfterHook_Runs_OnCancellation()
    {
        var afterMarkerFile = Path.Combine(TempPath, "TUnit_3882_Class_After.txt");

        // Clean up any existing marker files
        if (File.Exists(afterMarkerFile))
        {
            File.Delete(afterMarkerFile);
        }

        await RunTestsWithFilter(
            "/*/*/ClassLevelCancellationTests/*",
            [
                // After Class hook should have created the marker file - this proves Class After hooks ran on cancellation
                _ => File.Exists(afterMarkerFile).ShouldBeTrue($"Class After hook marker file should exist at {afterMarkerFile}")
            ]);

        // Verify marker file content
        if (File.Exists(afterMarkerFile))
        {
            var content = await File.ReadAllTextAsync(afterMarkerFile);
            content.ShouldContain("Class After hook executed");
        }
    }
}
