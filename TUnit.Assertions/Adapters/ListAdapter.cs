using TUnit.Assertions.Abstractions;

namespace TUnit.Assertions.Adapters;

/// <summary>
/// Adapter for IList&lt;T&gt; collections.
/// Provides index-based access in addition to standard collection operations.
/// </summary>
/// <typeparam name="TItem">The type of items in the list.</typeparam>
public readonly struct ListAdapter<TItem> : ICollectionAdapter<TItem>, IContainsCheck<TItem>, IIndexable<TItem>
{
    private readonly IList<TItem>? _source;

    /// <summary>
    /// Creates a new adapter for the specified list.
    /// </summary>
    public ListAdapter(IList<TItem>? source)
    {
        _source = source;
    }

    /// <inheritdoc />
    public string? Description => _source?.GetType().Name;

    /// <inheritdoc />
    public int Count => _source?.Count ?? 0;

    /// <inheritdoc />
    public bool IsEmpty => _source is null || _source.Count == 0;

    /// <inheritdoc />
    public int Length => Count;

    /// <inheritdoc />
    public TItem this[int index]
    {
        get
        {
            if (_source is null)
            {
                throw new InvalidOperationException("Cannot access index on null list");
            }

            return _source[index];
        }
    }

    /// <inheritdoc />
    public IEnumerable<TItem> AsEnumerable() => _source ?? [];

    /// <inheritdoc />
    public bool Contains(TItem item, IEqualityComparer<TItem>? comparer = null)
    {
        if (_source is null)
        {
            return false;
        }

        comparer ??= EqualityComparer<TItem>.Default;

        // Optimize for default comparer - use built-in Contains
        if (comparer == EqualityComparer<TItem>.Default)
        {
            return _source.Contains(item);
        }

        // Custom comparer - enumerate
        foreach (var element in _source)
        {
            if (comparer.Equals(element, item))
            {
                return true;
            }
        }

        return false;
    }
}
