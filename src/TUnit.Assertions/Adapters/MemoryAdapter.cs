#if NET5_0_OR_GREATER
using TUnit.Assertions.Abstractions;

namespace TUnit.Assertions.Adapters;

/// <summary>
/// Adapter for Memory&lt;T&gt; types.
/// Provides efficient access to memory-based collections.
/// </summary>
/// <typeparam name="TItem">The type of items in the memory.</typeparam>
public readonly struct MemoryAdapter<TItem> : ICollectionAdapter<TItem>, IIndexable<TItem>, IContainsCheck<TItem>
{
    private readonly Memory<TItem> _source;

    /// <summary>
    /// Creates a new adapter for the specified Memory.
    /// </summary>
    public MemoryAdapter(Memory<TItem> source)
    {
        _source = source;
    }

    /// <inheritdoc />
    public string? Description => $"Memory<{typeof(TItem).Name}>[{_source.Length}]";

    /// <inheritdoc />
    public int Count => _source.Length;

    /// <inheritdoc />
    public bool IsEmpty => _source.Length == 0;

    /// <inheritdoc />
    public TItem this[int index] => _source.Span[index];

    /// <inheritdoc />
    public int Length => _source.Length;

    /// <inheritdoc />
    public IEnumerable<TItem> AsEnumerable() => _source.ToArray();

    /// <inheritdoc />
    public bool Contains(TItem item, IEqualityComparer<TItem>? comparer = null)
    {
        comparer ??= EqualityComparer<TItem>.Default;
        var span = _source.Span;

        for (var i = 0; i < span.Length; i++)
        {
            if (comparer.Equals(span[i], item))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Adapter for ReadOnlyMemory&lt;T&gt; types.
/// Provides efficient access to read-only memory-based collections.
/// </summary>
/// <typeparam name="TItem">The type of items in the memory.</typeparam>
public readonly struct ReadOnlyMemoryAdapter<TItem> : ICollectionAdapter<TItem>, IIndexable<TItem>, IContainsCheck<TItem>
{
    private readonly ReadOnlyMemory<TItem> _source;

    /// <summary>
    /// Creates a new adapter for the specified ReadOnlyMemory.
    /// </summary>
    public ReadOnlyMemoryAdapter(ReadOnlyMemory<TItem> source)
    {
        _source = source;
    }

    /// <inheritdoc />
    public string? Description => $"ReadOnlyMemory<{typeof(TItem).Name}>[{_source.Length}]";

    /// <inheritdoc />
    public int Count => _source.Length;

    /// <inheritdoc />
    public bool IsEmpty => _source.Length == 0;

    /// <inheritdoc />
    public TItem this[int index] => _source.Span[index];

    /// <inheritdoc />
    public int Length => _source.Length;

    /// <inheritdoc />
    public IEnumerable<TItem> AsEnumerable() => _source.ToArray();

    /// <inheritdoc />
    public bool Contains(TItem item, IEqualityComparer<TItem>? comparer = null)
    {
        comparer ??= EqualityComparer<TItem>.Default;
        var span = _source.Span;

        for (var i = 0; i < span.Length; i++)
        {
            if (comparer.Equals(span[i], item))
            {
                return true;
            }
        }

        return false;
    }
}
#endif
