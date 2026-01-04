using TUnit.Assertions.Abstractions;

namespace TUnit.Assertions.Adapters;

/// <summary>
/// Adapter for IEnumerable&lt;T&gt; collections.
/// This is the default adapter for standard .NET collections.
/// </summary>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
public readonly struct EnumerableAdapter<TItem> : ICollectionAdapter<TItem>, IContainsCheck<TItem>
{
    private readonly IEnumerable<TItem>? _source;

    /// <summary>
    /// Creates a new adapter for the specified enumerable.
    /// </summary>
    public EnumerableAdapter(IEnumerable<TItem>? source)
    {
        _source = source;
    }

    /// <inheritdoc />
    public string? Description => _source?.GetType().Name;

    /// <inheritdoc />
    public int Count
    {
        get
        {
            if (_source is null)
            {
                return 0;
            }

            // Optimize for known collection types
            return _source switch
            {
                ICollection<TItem> collection => collection.Count,
                IReadOnlyCollection<TItem> readOnlyCollection => readOnlyCollection.Count,
                _ => _source.Count()
            };
        }
    }

    /// <inheritdoc />
    public bool IsEmpty
    {
        get
        {
            if (_source is null)
            {
                return true;
            }

            // Optimize for known collection types
            return _source switch
            {
                ICollection<TItem> collection => collection.Count == 0,
                IReadOnlyCollection<TItem> readOnlyCollection => readOnlyCollection.Count == 0,
                _ => !_source.Any()
            };
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

        // Optimize for sets with default comparer
        if (comparer == EqualityComparer<TItem>.Default)
        {
            if (_source is ISet<TItem> set)
            {
                return set.Contains(item);
            }

            if (_source is ICollection<TItem> collection)
            {
                return collection.Contains(item);
            }
        }

        // Fallback to enumeration
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
