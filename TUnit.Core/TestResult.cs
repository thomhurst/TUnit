using System.Text.Json.Serialization;

namespace TUnit.Core;

/// <summary>
/// Represents the outcome of a test execution, including the state, timing, exception information, and output.
/// Access via <see cref="Interfaces.ITestExecution.Result"/> on <see cref="TestContext.Execution"/>.
/// </summary>
public record TestResult
{
    /// <summary>
    /// Gets the final state of the test (Passed, Failed, Skipped, Timeout, or Cancelled).
    /// </summary>
    public required TestState State { get; init; }

    /// <summary>
    /// Gets the timestamp when test execution started, or <c>null</c> if the test did not start.
    /// </summary>
    public required DateTimeOffset? Start { get; init; }

    /// <summary>
    /// Gets the timestamp when test execution ended, or <c>null</c> if the test did not complete.
    /// </summary>
    public required DateTimeOffset? End { get; init; }

    /// <summary>
    /// Gets the duration of test execution, or <c>null</c> if the test did not complete.
    /// </summary>
    public required TimeSpan? Duration { get; init; }

    /// <summary>
    /// Gets the exception that caused the test to fail, or <c>null</c> if the test passed or was skipped.
    /// </summary>
    public required Exception? Exception { get; init; }

    /// <summary>
    /// Gets the name of the computer where the test was executed.
    /// </summary>
    public required string ComputerName { get; init; }

    /// <summary>
    /// Gets the captured standard output from the test execution.
    /// </summary>
    public string? Output { get; internal set; }

    [JsonIgnore]
    internal TestContext? TestContext { get; init; }

    /// <summary>
    /// The reason provided when this result was overridden.
    /// </summary>
    public string? OverrideReason { get; init; }

    /// <summary>
    /// Indicates whether this result was explicitly overridden via <see cref="TestContext.Execution.OverrideResult"/>.
    /// </summary>
    public bool IsOverridden { get; init; }

    /// <summary>
    /// The original exception that occurred before the result was overridden.
    /// Useful for debugging and audit trails when a test failure is overridden to pass or skip.
    /// </summary>
    public Exception? OriginalException { get; init; }
}
