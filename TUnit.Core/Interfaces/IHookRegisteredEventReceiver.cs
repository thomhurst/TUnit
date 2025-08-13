namespace TUnit.Core.Interfaces;

/// <summary>
/// Interface for hook registered event receivers
/// </summary>
public interface IHookRegisteredEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a hook is registered
    /// </summary>
    ValueTask OnHookRegistered(HookRegisteredContext context);
}