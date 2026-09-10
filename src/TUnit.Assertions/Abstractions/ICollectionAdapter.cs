namespace TUnit.Assertions.Abstractions;

/// <summary>
/// Core trait: something that can provide items as an enumerable.
/// This is the fallback path when specialized methods aren't available.
/// </summary>
/// <typeparam name="TItem">The type of items in the sequence.</typeparam>
public interface IItemSequence<out TItem>
{
    /// <summary>
    /// Enumerates items. For Memory/Span types, this may allocate.
    /// </summary>
    IEnumerable<TItem> AsEnumerable();
}

/// <summary>
/// Trait: knows its count without full enumeration.
/// Enables optimized Count() assertions.
/// </summary>
public interface ICountable
{
    /// <summary>
    /// The number of items in the collection.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Whether the collection is empty.
    /// </summary>
    bool IsEmpty { get; }
}

/// <summary>
/// Trait: can check for item containment.
/// Enables optimized Contains() without full enumeration when possible.
/// </summary>
/// <typeparam name="TItem">The type of items to check for.</typeparam>
public interface IContainsCheck<TItem>
{
    /// <summary>
    /// Checks if the collection contains the specified item.
    /// </summary>
    bool Contains(TItem item, IEqualityComparer<TItem>? comparer = null);
}

/// <summary>
/// Trait: supports indexed access to items.
/// Enables optimized index-based operations.
/// </summary>
/// <typeparam name="TItem">The type of items.</typeparam>
public interface IIndexable<out TItem>
{
    /// <summary>
    /// Gets the item at the specified index.
    /// </summary>
    TItem this[int index] { get; }

    /// <summary>
    /// The number of items accessible by index.
    /// </summary>
    int Length { get; }
}

/// <summary>
/// Combined interface for collection adapters.
/// Adapters wrap different collection types (IEnumerable, Memory, etc.)
/// to provide unified access for assertion logic.
/// </summary>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
public interface ICollectionAdapter<out TItem> : IItemSequence<TItem>, ICountable
{
    /// <summary>
    /// String representation for error messages.
    /// </summary>
    string? Description { get; }
}

/// <summary>
/// Trait: can check for key existence in a dictionary-like collection.
/// Enables optimized ContainsKey() without full enumeration.
/// </summary>
/// <typeparam name="TKey">The type of keys.</typeparam>
public interface IKeyLookup<in TKey>
{
    /// <summary>
    /// Checks if the collection contains the specified key.
    /// </summary>
    bool ContainsKey(TKey key);
}

/// <summary>
/// Trait: provides key-value access for dictionary-like collections.
/// Enables dictionary-specific assertions like ContainsValue, HasKey().WithValue().
/// </summary>
/// <typeparam name="TKey">The type of keys.</typeparam>
/// <typeparam name="TValue">The type of values.</typeparam>
public interface IKeyValueAccess<TKey, TValue> : IKeyLookup<TKey>
{
    /// <summary>
    /// Gets all keys in the dictionary.
    /// </summary>
    IEnumerable<TKey> Keys { get; }

    /// <summary>
    /// Gets all values in the dictionary.
    /// </summary>
    IEnumerable<TValue> Values { get; }

    /// <summary>
    /// Tries to get the value associated with the specified key.
    /// </summary>
    bool TryGetValue(TKey key, out TValue? value);

    /// <summary>
    /// Checks if the dictionary contains the specified value.
    /// </summary>
    bool ContainsValue(TValue value, IEqualityComparer<TValue>? comparer = null);
}

/// <summary>
/// Trait: provides set-specific operations.
/// Enables assertions like IsSubsetOf, IsSupersetOf, Overlaps.
/// </summary>
/// <typeparam name="TItem">The type of items in the set.</typeparam>
public interface ISetOperations<TItem>
{
    /// <summary>
    /// Determines whether the current set is a subset of a specified collection.
    /// </summary>
    bool IsSubsetOf(IEnumerable<TItem> other);

    /// <summary>
    /// Determines whether the current set is a superset of a specified collection.
    /// </summary>
    bool IsSupersetOf(IEnumerable<TItem> other);

    /// <summary>
    /// Determines whether the current set is a proper subset of a specified collection.
    /// </summary>
    bool IsProperSubsetOf(IEnumerable<TItem> other);

    /// <summary>
    /// Determines whether the current set is a proper superset of a specified collection.
    /// </summary>
    bool IsProperSupersetOf(IEnumerable<TItem> other);

    /// <summary>
    /// Determines whether the current set overlaps with the specified collection.
    /// </summary>
    bool Overlaps(IEnumerable<TItem> other);

    /// <summary>
    /// Determines whether the current set and the specified collection contain the same elements.
    /// </summary>
    bool SetEquals(IEnumerable<TItem> other);
}

/// <summary>
/// Combined interface for dictionary adapters.
/// Adapters wrap different dictionary types (IDictionary, IReadOnlyDictionary, etc.)
/// to provide unified access for assertion logic.
/// </summary>
/// <typeparam name="TKey">The type of keys.</typeparam>
/// <typeparam name="TValue">The type of values.</typeparam>
public interface IDictionaryAdapter<TKey, TValue>
    : ICollectionAdapter<KeyValuePair<TKey, TValue>>, IKeyValueAccess<TKey, TValue>
{
}

/// <summary>
/// Combined interface for set adapters.
/// Adapters wrap different set types (ISet, IReadOnlySet, HashSet, etc.)
/// to provide unified access for assertion logic.
/// </summary>
/// <typeparam name="TItem">The type of items in the set.</typeparam>
public interface ISetAdapter<TItem>
    : ICollectionAdapter<TItem>, ISetOperations<TItem>, IContainsCheck<TItem>
{
}
