namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines an event receiver that is notified when a lifecycle hook is registered with the test framework.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to perform custom logic when hooks (such as <c>[Before(Test)]</c>,
/// <c>[After(Class)]</c>, etc.) are registered. This is useful for modifying hook behavior,
/// such as applying timeouts to hooks or logging hook registration.
/// </para>
/// <para>
/// Built-in attributes such as <see cref="TimeoutAttribute"/> implement this interface to
/// apply their configuration to hooks in addition to tests.
/// </para>
/// <para>
/// The <see cref="HookRegisteredContext"/> parameter provides access to the hook's metadata
/// and allows modification of hook properties such as timeout values.
/// </para>
/// <para>
/// The <see cref="IEventReceiver.Order"/> property can be used to control the execution order
/// when multiple implementations of this interface exist.
/// </para>
/// </remarks>
public interface IHookRegisteredEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a lifecycle hook is registered with the test framework.
    /// </summary>
    /// <param name="context">The hook registered context containing information about the hook,
    /// including its method metadata and configurable properties such as timeout.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask OnHookRegistered(HookRegisteredContext context);
}