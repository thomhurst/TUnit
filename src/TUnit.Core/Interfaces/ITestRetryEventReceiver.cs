namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines an event receiver that is notified when a test is about to be retried after a failure.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to perform custom logic before a test retry attempt, such as
/// resetting shared state, logging retry information, or adjusting test configuration
/// between attempts.
/// </para>
/// <para>
/// This event is only triggered when a test has a <see cref="RetryAttribute"/> (or a derived attribute)
/// applied to it and the test has failed. The event fires before each retry attempt, not before the
/// initial test execution.
/// </para>
/// <para>
/// The <see cref="IEventReceiver.Order"/> property can be used to control the execution order
/// when multiple implementations of this interface exist.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class RetryLoggingReceiver : ITestRetryEventReceiver
/// {
///     public async ValueTask OnTestRetry(TestContext context, int retryAttempt)
///     {
///         await context.OutputWriter.WriteLineAsync(
///             $"Retrying test {context.TestDetails.TestName}, attempt {retryAttempt}");
///     }
/// }
/// </code>
/// </example>
public interface ITestRetryEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test is about to be retried after a failure.
    /// </summary>
    /// <param name="context">The test context containing information about the test being retried.</param>
    /// <param name="retryAttempt">The 1-based retry attempt number. The first retry is 1, the second is 2, and so on.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask OnTestRetry(TestContext context, int retryAttempt);
}
