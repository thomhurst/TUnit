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

    // Indexer state is keyed by the index arguments too, so stub[1] and stub[2] round-trip
    // independently. Defaults stay index-independent (shared via _returnCache) for stable
    // identity across indices.
    private readonly ConcurrentDictionary<(int Slot, IndexKey Key), object?> _indexerValues = new();

    /// <summary>Indexer getter: an explicitly set value for these exact indices wins; otherwise
    /// the cached default for the slot.</summary>
    protected object? GetIndexerValue(int slot, object?[] indices, Type propertyType)
    {
        if (_indexerValues.TryGetValue((slot, new IndexKey(indices)), out var value))
        {
            return value;
        }

        return _returnCache.GetOrAdd((slot, propertyType), static key => RuntimeStubDefaults.GetDefault(key.ReturnType));
    }

    /// <summary>Indexer setter: remembers the value per index-argument combination.</summary>
    protected void SetIndexerValue(int slot, object?[] indices, object? value)
        => _indexerValues[(slot, new IndexKey(indices))] = value;

    internal void ResetState()
    {
        _propertyValues.Clear();
        _indexerValues.Clear();
        _returnCache.Clear();
    }

    /// <summary>Structural-equality wrapper over an indexer's boxed index arguments.</summary>
    private readonly struct IndexKey(object?[] indices) : IEquatable<IndexKey>
    {
        private readonly object?[] _indices = indices;

        public bool Equals(IndexKey other)
        {
            if (_indices.Length != other._indices.Length)
            {
                return false;
            }

            for (var i = 0; i < _indices.Length; i++)
            {
                if (!Equals(_indices[i], other._indices[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj) => obj is IndexKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                foreach (var index in _indices)
                {
                    hash = hash * 31 + (index?.GetHashCode() ?? 0);
                }

                return hash;
            }
        }
    }
}
