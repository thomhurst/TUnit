using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Attribute that specifies a performance baseline for a test.
/// When the test exceeds the specified maximum duration, a warning is emitted.
/// If <c>--performance-baseline-fail</c> is enabled, the test will be marked as failed instead.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute to track test execution times and detect performance regressions.
/// The attribute measures the actual test body execution time (from TestStart to TestEnd).
/// </para>
///
/// <para>
/// The attribute can be applied at different levels:
/// </para>
/// <list type="bullet">
/// <item>Method level: Sets baseline for a specific test method</item>
/// <item>Class level: Sets baseline for all test methods in the class</item>
/// <item>Assembly level: Sets baseline for all test methods in the assembly</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Set a 500ms performance baseline
/// [Test, PerformanceBaseline(MaxDurationMs = 500)]
/// public async Task FastOperation()
/// {
///     await DoWork(); // Warning if this takes longer than 500ms
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class PerformanceBaselineAttribute : TUnitAttribute, ITestEndEventReceiver
{
    /// <summary>
    /// When set to <c>true</c>, performance baseline violations will cause the test to fail
    /// instead of emitting a warning. This is set by the engine when <c>--performance-baseline-fail</c> is specified.
    /// </summary>
    internal static bool FailOnViolation { get; set; }

    /// <summary>
    /// Gets or sets the maximum expected duration in milliseconds.
    /// If the test exceeds this duration, a warning or failure is produced.
    /// </summary>
    public int MaxDurationMs { get; set; }

    /// <inheritdoc />
#if NET
    public int Order => int.MaxValue; // Run after all other end receivers
#else
    public int Order { get; } = int.MaxValue; // Run after all other end receivers
#endif

    /// <inheritdoc />
    public ValueTask OnTestEnd(TestContext context)
    {
        var result = context.Execution.Result;

        if (result is null)
        {
            return default;
        }

        // Only check passing tests - no point flagging a failed test for performance
        if (result.State != TestState.Passed)
        {
            return default;
        }

        var duration = result.Duration;

        if (duration is null)
        {
            return default;
        }

        var maxDuration = TimeSpan.FromMilliseconds(MaxDurationMs);

        if (duration.Value <= maxDuration)
        {
            return default;
        }

        var message = $"Performance baseline exceeded: test took {duration.Value.TotalMilliseconds:F1}ms but baseline is {MaxDurationMs}ms";

        if (FailOnViolation)
        {
            context.Execution.OverrideResult(TestState.Failed, message);
        }
        else
        {
            // Emit as a warning via test output
            context.Output.WriteError($"[PERF WARNING] {message}");
        }

        return default;
    }
}
