using TUnit.Core;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Tracks test results at the session level to determine overall session outcome.
/// Single Responsibility: Session-level result tracking.
/// </summary>
internal sealed class SessionResultTracker
{
    private volatile bool _hasAnyPassedTest;
    private volatile bool _hasAnyFailedTest;
    private volatile bool _hasAnySkippedTest;

    public void RecordTestResult(TestState state)
    {
        switch (state)
        {
            case TestState.Passed:
                _hasAnyPassedTest = true;
                break;
            case TestState.Failed:
                _hasAnyFailedTest = true;
                break;
            case TestState.Skipped:
                _hasAnySkippedTest = true;
                break;
        }
    }

    /// <summary>
    /// Returns true if all tests were skipped (no passed or failed tests, but at least one skipped test).
    /// This indicates the session should be marked as failed per TUnit requirements.
    /// </summary>
    public bool ShouldMarkSessionAsFailedDueToSkippedTests()
    {
        return !_hasAnyPassedTest && !_hasAnyFailedTest && _hasAnySkippedTest;
    }

    public bool HasAnyTestsRun()
    {
        return _hasAnyPassedTest || _hasAnyFailedTest || _hasAnySkippedTest;
    }
}