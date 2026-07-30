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

    // Keyed by (slot, closed return type): a generic member like T Get<T>() shares one slot
    // across every instantiation, so the type must disambiguate or Get<string>() would hand
    // back Get<int>()'s cached boxed value and fail the emitted unbox/cast.
    private readonly ConcurrentDictionary<(int Slot, Type ReturnType), object?> _returnCache = new();

    /// <summary>
    /// Default return value for a method slot. Cached per slot and closed return type so
    /// repeated calls hand back the same instance (matching auto-mock identity semantics).
    /// </summary>
    protected object? GetReturnValue(int slot, Type returnType)
        => _returnCache.GetOrAdd((slot, returnType), static key => RuntimeStubDefaults.GetDefault(key.ReturnType));

    /// <summary>
    /// Property getter: an explicitly set value wins; otherwise the cached default for the slot.
    /// </summary>
    protected object? GetPropertyValue(int slot, Type propertyType)
    {
        if (_propertyValues.TryGetValue(slot, out var value))
        {
            return value;
        }

        return _returnCache.GetOrAdd((slot, propertyType), static key => RuntimeStubDefaults.GetDefault(key.ReturnType));
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
