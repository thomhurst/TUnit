namespace TUnit.Core;

/// <summary>
/// Represents the message bus for TUnit.
/// </summary>
internal interface ITUnitMessageBus
{
    /// <summary>
    /// Sends a discovered message for the specified test context.
    /// </summary>
    /// <param name="testContext">The test context.</param>
    ValueTask Discovered(TestContext testContext);

    /// <summary>
    /// Sends an in-progress message for the specified test context.
    /// </summary>
    /// <param name="testContext">The test context.</param>
    ValueTask InProgress(TestContext testContext);

    /// <summary>
    /// Sends a passed message for the specified test context.
    /// </summary>
    /// <param name="testContext">The test context.</param>
    /// <param name="start">The start time of the test.</param>
    ValueTask Passed(TestContext testContext, DateTimeOffset start);

    /// <summary>
    /// Sends a failed message for the specified test context.
    /// </summary>
    /// <param name="testContext">The test context.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="start">The start time of the test.</param>
    ValueTask Failed(TestContext testContext, Exception exception, DateTimeOffset start);

    /// <summary>
    /// Sends a skipped message for the specified test context.
    /// </summary>
    /// <param name="testContext">The test context.</param>
    /// <param name="reason">The reason for skipping the test.</param>
    ValueTask Skipped(TestContext testContext, string reason);

    /// <summary>
    /// Sends a cancelled message for the specified test context.
    /// </summary>
    /// <param name="testContext">The test context.</param>
    /// <param name="start">The time that the test started</param>
    ValueTask Cancelled(TestContext testContext, DateTimeOffset start);

    /// <summary>
    /// Sends a session artifact message.
    /// </summary>
    /// <param name="artifact">The artifact.</param>
    ValueTask SessionArtifact(Artifact artifact);
}
