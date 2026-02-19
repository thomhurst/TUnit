namespace TUnit.Core.Interfaces;

using TUnit.Core.Enums;

/// <summary>
/// Defines an event receiver that is notified when a test is about to start execution.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to perform custom logic before a test executes, such as
/// setting up per-test resources, initializing logging, recording timing information,
/// or configuring the test environment.
/// </para>
/// <para>
/// The order of test lifecycle events is:
/// <list type="number">
/// <item><see cref="ITestRegisteredEventReceiver"/> - when the test is registered</item>
/// <item><see cref="ITestDiscoveryEventReceiver"/> - when the test is discovered</item>
/// <item><see cref="ITestStartEventReceiver"/> - before the test executes (this interface)</item>
/// <item>Test method executes</item>
/// <item><see cref="ITestEndEventReceiver"/> - after the test completes</item>
/// </list>
/// </para>
/// <para>
/// On .NET 8.0+, the <see cref="Stage"/> property controls whether the receiver runs
/// before or after instance-level <c>[Before(Test)]</c> hooks. The default is
/// <see cref="EventReceiverStage.Late"/> for backward compatibility.
/// </para>
/// <para>
/// The <see cref="IEventReceiver.Order"/> property can be used to control the execution order
/// when multiple implementations of this interface exist.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class TimingReceiver : ITestStartEventReceiver, ITestEndEventReceiver
/// {
///     public async ValueTask OnTestStart(TestContext context)
///     {
///         context.ObjectBag["StartTime"] = DateTime.UtcNow;
///     }
///
///     public async ValueTask OnTestEnd(TestContext context)
///     {
///         var start = (DateTime)context.ObjectBag["StartTime"];
///         var duration = DateTime.UtcNow - start;
///         await context.OutputWriter.WriteLineAsync($"Test took {duration.TotalMilliseconds}ms");
///     }
/// }
/// </code>
/// </example>
public interface ITestStartEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test is about to start execution.
    /// </summary>
    /// <param name="context">The test context containing information about the test being executed.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask OnTestStart(TestContext context);

    /// <summary>
    /// Gets the execution stage of this event receiver relative to instance-level hooks.
    /// </summary>
    /// <remarks>
    /// Early stage executes before [Before(Test)] hooks, Late stage executes after.
    /// Default is Late for backward compatibility.
    /// This property is only available on .NET 8.0+ due to default interface member requirements.
    /// On older frameworks, all receivers execute at Late stage.
    /// </remarks>
    /// <value>
    /// The execution stage. Default is <see cref="EventReceiverStage.Late"/>.
    /// </value>
#if NET
    public EventReceiverStage Stage => EventReceiverStage.Late;
#endif
}
