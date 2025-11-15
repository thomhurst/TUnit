using TUnit.Core.Enums;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Simplified interface for test start event receivers
/// </summary>
public interface ITestStartEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test starts
    /// </summary>
    ValueTask OnTestStart(TestContext context);
    
#if NET
    /// <summary>
    /// Gets the stage at which this event receiver executes relative to instance-level hooks.
    /// Default is Late (runs after [Before(Test)] hooks).
    /// </summary>
    HookStage HookStage => HookStage.Late;
#endif
}
