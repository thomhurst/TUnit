using TUnit.Core.Enums;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Simplified interface for test end event receivers
/// </summary>
public interface ITestEndEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test ends
    /// </summary>
    ValueTask OnTestEnd(TestContext context);
    
#if NET
    /// <summary>
    /// Gets the stage at which this event receiver executes relative to instance-level hooks.
    /// Default is Late (runs after [After(Test)] hooks).
    /// </summary>
    HookStage HookStage => HookStage.Late;
#endif
}
