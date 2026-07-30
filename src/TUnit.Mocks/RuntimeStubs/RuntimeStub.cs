using System.Collections.Concurrent;
using System.ComponentModel;

namespace TUnit.Mocks.RuntimeStubs;

/// <summary>
/// Base class for runtime-emitted interface stubs (see <see cref="RuntimeStubGenerator"/>).
/// Emitted accessor and method bodies delegate here so the generated IL stays trivial.
/// Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class RuntimeStub
{
    private readonly ConcurrentDictionary<int, object?> _propertyValues = new();
    private readonly ConcurrentDictionary<int, object?> _returnCache = new();

    /// <summary>
    /// Default return value for a method slot. Cached per slot so repeated calls hand back the
    /// same instance (matching auto-mock identity semantics).
    /// </summary>
    protected object? GetReturnValue(int slot, Type returnType)
        => _returnCache.GetOrAdd(slot, static (_, t) => RuntimeStubDefaults.GetDefault(t), returnType);

    /// <summary>
    /// Property getter: an explicitly set value wins; otherwise the cached default for the slot.
    /// </summary>
    protected object? GetPropertyValue(int slot, Type propertyType)
    {
        if (_propertyValues.TryGetValue(slot, out var value))
        {
            return value;
        }

        return _returnCache.GetOrAdd(slot, static (_, t) => RuntimeStubDefaults.GetDefault(t), propertyType);
    }

    /// <summary>Property setter: remembers the value so a later get round-trips it.</summary>
    protected void SetPropertyValue(int slot, object? value)
        => _propertyValues[slot] = value;

    internal void ResetState()
    {
        _propertyValues.Clear();
        _returnCache.Clear();
    }
}
