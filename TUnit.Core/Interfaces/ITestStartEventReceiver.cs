namespace TUnit.Core.Interfaces;

using TUnit.Core.Enums;

/// <summary>
/// Simplified interface for test start event receivers
/// </summary>
public interface ITestStartEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test starts
    /// </summary>
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
