using System.ComponentModel;

namespace TUnit.Mocks;

/// <summary>
/// Implemented by generated mock impl classes to allow event raising from the engine.
/// Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IRaisable
{
    /// <summary>
    /// Raises the named event with the specified arguments.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    void RaiseEvent(string eventName, object? args);
}
