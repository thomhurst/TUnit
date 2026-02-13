using TUnit.Core;

namespace CloudShop.Tests.Hooks;

/// <summary>
/// Assembly-level and per-test hooks for test infrastructure management.
///
/// Showcases:
/// - [Before(HookType.Assembly)] for one-time setup
/// - [AfterEvery(HookType.Test)] for per-test logging
/// - TestContext for accessing test metadata and results
/// </summary>
public static class TestHooks
{
    [Before(HookType.Assembly)]
    public static void LogTestRunStart()
    {
        Console.WriteLine("========================================");
        Console.WriteLine(" CloudShop Integration Tests Starting");
        Console.WriteLine($" {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
        Console.WriteLine("========================================");
    }

    [AfterEvery(HookType.Test)]
    public static void LogTestResult(TestContext context)
    {
        var status = context.Execution.Result?.State switch
        {
            TestState.Passed => "PASS",
            TestState.Failed => "FAIL",
            TestState.Skipped => "SKIP",
            _ => "UNKNOWN"
        };

        Console.WriteLine($"[{status}] {context.Metadata.DisplayName} ({context.Execution.Result?.Duration?.TotalMilliseconds:F0}ms)");
    }

    [After(HookType.Assembly)]
    public static void LogTestRunEnd()
    {
        Console.WriteLine("========================================");
        Console.WriteLine(" CloudShop Integration Tests Complete");
        Console.WriteLine($" {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
        Console.WriteLine("========================================");
    }
}
