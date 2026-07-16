namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines an event receiver that is notified when a test is discovered during the discovery phase.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to perform custom logic when a test is discovered, such as
/// modifying test metadata, setting timeouts, configuring retry logic, or conditionally
/// skipping tests based on runtime conditions.
/// </para>
/// <para>
/// This is one of the most commonly implemented event receivers for third-party extensions,
/// as it allows modifying test behavior before execution begins. Many built-in attributes
/// such as <see cref="RetryAttribute"/> and <see cref="TimeoutAttribute"/> implement this interface.
/// </para>
/// <para>
/// The <see cref="DiscoveredTestContext"/> parameter provides methods to modify the test's
/// configuration, such as setting retry limits, timeouts, and custom properties.
/// </para>
/// <para>
/// The <see cref="IEventReceiver.Order"/> property can be used to control the execution order
/// when multiple implementations of this interface exist.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ConditionalSkipReceiver : ITestDiscoveryEventReceiver
/// {
///     public int Order => 0;
///
///     public ValueTask OnTestDiscovered(DiscoveredTestContext context)
///     {
///         if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
///         {
///             context.SkipTest("This test only runs on Windows");
///         }
///         return ValueTask.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface ITestDiscoveryEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test is discovered during the test discovery phase.
    /// </summary>
    /// <param name="context">The discovered test context, which provides methods to modify the test's
    /// configuration such as retry limits, timeouts, and skip conditions.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask OnTestDiscovered(DiscoveredTestContext context);
}
