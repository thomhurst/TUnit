using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Diagnostics;

/// <summary>
/// Diagnostic report for a mock instance. Reports unused setups, unmatched calls, and coverage metrics.
/// </summary>
public sealed class MockDiagnostics
{
    /// <summary>Setups that were registered but never triggered (InvokeCount == 0).</summary>
    public IReadOnlyList<SetupInfo> UnusedSetups { get; }

    /// <summary>Calls that matched no setup (fell through to default behavior).</summary>
    public IReadOnlyList<CallRecord> UnmatchedCalls { get; }

    /// <summary>Total number of registered setups.</summary>
    public int TotalSetups { get; }

    /// <summary>Number of setups that were actually invoked at least once.</summary>
    public int ExercisedSetups { get; }

    public MockDiagnostics(
        IReadOnlyList<SetupInfo> unusedSetups,
        IReadOnlyList<CallRecord> unmatchedCalls,
        int totalSetups,
        int exercisedSetups)
    {
        UnusedSetups = unusedSetups;
        UnmatchedCalls = unmatchedCalls;
        TotalSetups = totalSetups;
        ExercisedSetups = exercisedSetups;
    }
}
