using TUnit.Core;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Tracks test outcomes to detect and report flaky test behavior.
/// A test is considered flaky when it fails on initial attempts but passes on retry.
/// </summary>
internal static class FlakyTestTracker
{
    /// <summary>
    /// Records that a test passed after one or more retry attempts, indicating flaky behavior.
    /// Writes a warning to the test output so it appears in test results.
    /// </summary>
    /// <param name="testContext">The test context for the test that passed on retry.</param>
    /// <param name="successfulAttempt">The zero-based attempt number on which the test passed.</param>
    internal static void RecordPassedOnRetry(TestContext testContext, int successfulAttempt)
    {
        var testName = testContext.Metadata.TestDetails.TestName;
        var isMarkedFlaky = testContext.Metadata.TestDetails.HasAttribute<FlakyAttribute>();

        var message = isMarkedFlaky
            ? $"[Flaky] Test '{testName}' passed on retry attempt {successfulAttempt} (known flaky test)"
            : $"[Warning] Test '{testName}' passed on retry attempt {successfulAttempt} - this test may be flaky";

        var flakyAttribute = isMarkedFlaky
            ? testContext.Metadata.TestDetails.GetAttributes<FlakyAttribute>().FirstOrDefault()
            : null;

        if (flakyAttribute?.Reason is { } reason)
        {
            message += $" - Reason: {reason}";
        }

        testContext.Output.WriteLine(message);
    }

    /// <summary>
    /// Records that a test marked with <see cref="FlakyAttribute"/> has failed after exhausting all retries.
    /// Annotates the test output to indicate this is a known flaky test failure.
    /// </summary>
    /// <param name="testContext">The test context for the failed test.</param>
    /// <param name="exception">The exception that caused the final failure.</param>
    internal static void RecordFlakyFailure(TestContext testContext, Exception exception)
    {
        if (!testContext.Metadata.TestDetails.HasAttribute<FlakyAttribute>())
        {
            return;
        }

        var testName = testContext.Metadata.TestDetails.TestName;
        var flakyAttribute = testContext.Metadata.TestDetails.GetAttributes<FlakyAttribute>().FirstOrDefault();

        var message = $"[Flaky] Test '{testName}' failed - this is a known flaky test";

        if (flakyAttribute?.Reason is { } reason)
        {
            message += $" - Reason: {reason}";
        }

        testContext.Output.WriteLine(message);
    }
}
