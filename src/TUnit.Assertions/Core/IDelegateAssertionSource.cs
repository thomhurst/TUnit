namespace TUnit.Assertions.Core;

/// <summary>
/// Marker interface for delegate-based assertion sources.
/// Only assertions on delegates (Func, Action, async delegates) implement this interface.
/// This enables type-safe extension methods like Throws() that only make sense for delegates.
/// </summary>
/// <typeparam name="TValue">The type of value returned by the delegate</typeparam>
public interface IDelegateAssertionSource<TValue> : IAssertionSource<TValue>
{
}
