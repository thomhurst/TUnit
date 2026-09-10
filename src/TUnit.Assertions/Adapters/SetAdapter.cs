using TUnit.Assertions.Abstractions;

namespace TUnit.Assertions.Adapters;

/// <summary>
/// Zero-allocation struct adapter for ISet&lt;T&gt;.
/// Provides unified access to set operations for assertion logic.
/// </summary>
/// <typeparam name="TItem">The type of items in the set.</typeparam>
public readonly struct SetAdapter<TItem> : ISetAdapter<TItem>
{
    private readonly ISet<TItem>? _source;

    /// <summary>
    /// Creates a new adapter for the specified set.
    /// </summary>
    public SetAdapter(ISet<TItem>? source)
    {
        _source = source;
    }

    /// <inheritdoc />
    public int Count => _source?.Count ?? 0;

    /// <inheritdoc />
    public bool IsEmpty => _source == null || _source.Count == 0;

    /// <inheritdoc />
    public string? Description => null;

    /// <inheritdoc />
    public IEnumerable<TItem> AsEnumerable()
        => _source ?? Enumerable.Empty<TItem>();

    /// <inheritdoc />
    public bool Contains(TItem item, IEqualityComparer<TItem>? comparer = null)
    {
        if (_source == null)
        {
            return false;
        }

        // If no custom comparer, use the set's native Contains (most efficient)
        if (comparer == null)
        {
            return _source.Contains(item);
        }

        // With custom comparer, we need to iterate
        foreach (var element in _source)
        {
            if (comparer.Equals(element, item))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public bool IsSubsetOf(IEnumerable<TItem> other)
        => _source?.IsSubsetOf(other) ?? true; // Empty set is subset of anything

    /// <inheritdoc />
    public bool IsSupersetOf(IEnumerable<TItem> other)
        => _source?.IsSupersetOf(other) ?? !other.Any(); // Empty set is only superset of empty

    /// <inheritdoc />
    public bool IsProperSubsetOf(IEnumerable<TItem> other)
        => _source?.IsProperSubsetOf(other) ?? other.Any(); // Empty is proper subset if other has elements

    /// <inheritdoc />
    public bool IsProperSupersetOf(IEnumerable<TItem> other)
        => _source?.IsProperSupersetOf(other) ?? false; // Empty set cannot be proper superset

    /// <inheritdoc />
    public bool Overlaps(IEnumerable<TItem> other)
        => _source?.Overlaps(other) ?? false; // Empty set doesn't overlap anything

    /// <inheritdoc />
    public bool SetEquals(IEnumerable<TItem> other)
        => _source?.SetEquals(other) ?? !other.Any(); // Empty equals empty
}

#if NET5_0_OR_GREATER
/// <summary>
/// Zero-allocation struct adapter for IReadOnlySet&lt;T&gt;.
/// Provides unified access to set operations for assertion logic.
/// </summary>
/// <typeparam name="TItem">The type of items in the set.</typeparam>
public readonly struct ReadOnlySetAdapter<TItem> : ISetAdapter<TItem>
{
    private readonly IReadOnlySet<TItem>? _source;

    /// <summary>
    /// Creates a new adapter for the specified read-only set.
    /// </summary>
    public ReadOnlySetAdapter(IReadOnlySet<TItem>? source)
    {
        _source = source;
    }

    /// <inheritdoc />
    public int Count => _source?.Count ?? 0;

    /// <inheritdoc />
    public bool IsEmpty => _source == null || _source.Count == 0;

    /// <inheritdoc />
    public string? Description => null;

    /// <inheritdoc />
    public IEnumerable<TItem> AsEnumerable()
        => _source ?? Enumerable.Empty<TItem>();

    /// <inheritdoc />
    public bool Contains(TItem item, IEqualityComparer<TItem>? comparer = null)
    {
        if (_source == null)
        {
            return false;
        }

        // If no custom comparer, use the set's native Contains (most efficient)
        if (comparer == null)
        {
            return _source.Contains(item);
        }

        // With custom comparer, we need to iterate
        foreach (var element in _source)
        {
            if (comparer.Equals(element, item))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public bool IsSubsetOf(IEnumerable<TItem> other)
        => _source?.IsSubsetOf(other) ?? true;

    /// <inheritdoc />
    public bool IsSupersetOf(IEnumerable<TItem> other)
        => _source?.IsSupersetOf(other) ?? !other.Any();

    /// <inheritdoc />
    public bool IsProperSubsetOf(IEnumerable<TItem> other)
        => _source?.IsProperSubsetOf(other) ?? other.Any();

    /// <inheritdoc />
    public bool IsProperSupersetOf(IEnumerable<TItem> other)
        => _source?.IsProperSupersetOf(other) ?? false;

    /// <inheritdoc />
    public bool Overlaps(IEnumerable<TItem> other)
        => _source?.Overlaps(other) ?? false;

    /// <inheritdoc />
    public bool SetEquals(IEnumerable<TItem> other)
        => _source?.SetEquals(other) ?? !other.Any();
}
#endif
