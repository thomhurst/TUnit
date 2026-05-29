namespace TUnit.Core;

/// <summary>
/// Records the outcome of a single test execution attempt when a test is retried.
/// Exposed via <see cref="Interfaces.ITestExecution.RetryAttempts"/> so reporters and user
/// code can show retry/flaky history (e.g. the HTML report's attempt timeline).
/// </summary>
public readonly record struct RetryAttemptRecord
{
    /// <summary>
    /// The final state this attempt reached (typically <see cref="TestState.Failed"/> or
    /// <see cref="TestState.Timeout"/>, since only attempts that triggered a retry are recorded).
    /// </summary>
    public required TestState State { get; init; }

    /// <summary>
    /// How long this attempt took to execute.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// The full type name of the exception that ended this attempt, or null if none.
    /// </summary>
    public string? ExceptionType { get; init; }

    /// <summary>
    /// The message of the exception that ended this attempt, or null if none.
    /// </summary>
    public string? ExceptionMessage { get; init; }

    /// <summary>
    /// The stack trace of the exception that ended this attempt, or null if none.
    /// </summary>
    public string? ExceptionStackTrace { get; init; }
}
