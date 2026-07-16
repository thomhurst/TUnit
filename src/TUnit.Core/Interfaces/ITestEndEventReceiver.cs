namespace TUnit.Core.Interfaces;

using TUnit.Core.Enums;

/// <summary>
/// Defines an event receiver that is notified when a test has completed execution.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to perform custom logic after a test completes, such as
/// recording test results, cleaning up per-test resources, publishing metrics,
/// or capturing diagnostic information on failure.
/// </para>
/// <para>
/// This event fires regardless of whether the test passed, failed, or threw an exception.
/// The test result information is available through the <paramref name="context"/> parameter.
/// </para>
/// <para>
/// On .NET 8.0+, the <see cref="Stage"/> property controls whether the receiver runs
/// before or after instance-level <c>[After(Test)]</c> hooks. The default is
/// <see cref="EventReceiverStage.Late"/> for backward compatibility.
/// </para>
/// <para>
/// The <see cref="IEventReceiver.Order"/> property can be used to control the execution order
/// when multiple implementations of this interface exist.
/// </para>
/// </remarks>
public interface ITestEndEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test has completed execution.
    /// </summary>
    /// <param name="context">The test context containing information about the completed test, including its result.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask OnTestEnd(TestContext context);

    /// <summary>
    /// Gets the execution stage of this event receiver relative to instance-level hooks.
    /// </summary>
    /// <remarks>
    /// Early stage executes before [After(Test)] hooks, Late stage executes after.
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
