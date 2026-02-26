using System.ComponentModel;

namespace TUnit.Mocks.Setup.Behaviors;

/// <summary>
/// Behavior that invokes a callback with the method arguments.
/// Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class CallbackWithArgsBehavior : IBehavior
{
    private readonly Action<object?[]> _callback;

    public CallbackWithArgsBehavior(Action<object?[]> callback)
    {
        _callback = callback;
    }

    public object? Execute(object?[] arguments)
    {
        _callback(arguments);
        return null;
    }
}
