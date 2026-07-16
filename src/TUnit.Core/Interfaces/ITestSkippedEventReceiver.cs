namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines an event receiver that is notified when a test is skipped.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to perform custom logic when a test is skipped, such as
/// logging skip reasons, collecting metrics on skipped tests, or performing conditional
/// cleanup of resources that were prepared for the test.
/// </para>
/// <para>
/// A test can be skipped via the <see cref="SkipAttribute"/>, by calling
/// <see cref="TestContext.SkipTest(string)"/> during execution, or through other
/// framework mechanisms that mark a test as skipped.
/// </para>
/// <para>
/// The <see cref="IEventReceiver.Order"/> property can be used to control the execution order
/// when multiple implementations of this interface exist.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class SkipTrackingReceiver : ITestSkippedEventReceiver
/// {
///     public async ValueTask OnTestSkipped(TestContext context)
///     {
///         await context.OutputWriter.WriteLineAsync(
///             $"Test {context.TestDetails.TestName} was skipped");
///     }
/// }
/// </code>
/// </example>
public interface ITestSkippedEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test is skipped.
    /// </summary>
    /// <param name="context">The test context containing information about the skipped test,
    /// including the skip reason accessible via the context.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask OnTestSkipped(TestContext context);
}
