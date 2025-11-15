namespace TUnit.Core.Interfaces;

using TUnit.Core.Enums;

/// <summary>
/// Simplified interface for test end event receivers
/// </summary>
public interface ITestEndEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test ends
    /// </summary>
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
