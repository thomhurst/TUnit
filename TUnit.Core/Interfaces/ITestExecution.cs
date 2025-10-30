namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides access to test execution state and lifecycle management.
/// Accessed via <see cref="TestContext.Execution"/>.
/// </summary>
public interface ITestExecution
{
    /// <summary>
    /// Gets the current phase of test execution (Discovery, Execution, Cleanup, etc.).
    /// </summary>
    TestPhase Phase { get; }

    /// <summary>
    /// Gets the test result after execution completes, or null if the test is still running.
    /// </summary>
    TestResult? Result { get; internal set; }

    /// <summary>
    /// Gets the cancellation token for this test execution.
    /// Check <see cref="CancellationToken.IsCancellationRequested"/> to honor cancellation requests.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the timestamp when test execution started, or null if not yet started.
    /// </summary>
    DateTimeOffset? TestStart { get; internal set; }

    /// <summary>
    /// Gets the timestamp when test execution ended, or null if not yet completed.
    /// </summary>
    DateTimeOffset? TestEnd { get; internal set; }

    /// <summary>
    /// Gets the current retry attempt number (0 for first attempt, 1+ for retries).
    /// </summary>
    int CurrentRetryAttempt { get; internal set; }

    /// <summary>
    /// Gets the reason why this test was skipped, or null if not skipped.
    /// </summary>
    string? SkipReason { get; }

    /// <summary>
    /// Gets the retry function that determines whether a failed test should be retried.
    /// </summary>
    Func<TestContext, Exception, int, Task<bool>>? RetryFunc { get; }

    /// <summary>
    /// Overrides the test result with a passed state and custom reason.
    /// Useful for marking tests as passed under special conditions.
    /// </summary>
    /// <param name="reason">The reason for overriding the result</param>
    void OverrideResult(string reason);

    /// <summary>
    /// Overrides the test result with a specific state and custom reason.
    /// </summary>
    /// <param name="state">The desired test state (Passed, Failed, Skipped, etc.)</param>
    /// <param name="reason">The reason for overriding the result</param>
    void OverrideResult(TestState state, string reason);

    /// <summary>
    /// Gets or sets a custom hook executor that overrides the default execution behavior for test-level hooks.
    /// Allows wrapping hook execution in custom logic (e.g., running on a specific thread).
    /// </summary>
    IHookExecutor? CustomHookExecutor { get; set; }

    /// <summary>
    /// Gets or sets whether the test result should be reported to test runners.
    /// Defaults to true. Set to false to suppress reporting for internal or diagnostic tests.
    /// </summary>
    bool ReportResult { get; set; }

    /// <summary>
    /// Links an external cancellation token to this test's execution token.
    /// Useful for coordinating cancellation across multiple operations or tests.
    /// </summary>
    /// <param name="cancellationToken">The external cancellation token to link</param>
    void AddLinkedCancellationToken(CancellationToken cancellationToken);
}
