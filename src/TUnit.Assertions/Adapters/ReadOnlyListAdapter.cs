using TUnit.Assertions.Abstractions;

namespace TUnit.Assertions.Adapters;

/// <summary>
/// Adapter for IReadOnlyList&lt;T&gt; that provides index-based access.
/// </summary>
public readonly struct ReadOnlyListAdapter<TItem> : ICollectionAdapter<TItem>, IContainsCheck<TItem>, IIndexable<TItem>
{
    private readonly IReadOnlyList<TItem>? _source;

    public ReadOnlyListAdapter(IReadOnlyList<TItem>? source)
    {
        _source = source;
    }

    /// <inheritdoc />
    public string? Description => _source?.GetType().Name;

    public int Count => _source?.Count ?? 0;

    public bool IsEmpty => Count == 0;

    public TItem this[int index] => _source![index];

    public int Length => Count;

    public IEnumerable<TItem> AsEnumerable() => _source ?? [];

    public bool Contains(TItem item, IEqualityComparer<TItem>? comparer = null)
    {
        if (_source == null)
        {
            return false;
        }

        comparer ??= EqualityComparer<TItem>.Default;

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
