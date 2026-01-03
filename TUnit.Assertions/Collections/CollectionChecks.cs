using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.Abstractions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Collections;

/// <summary>
/// Single source of truth for all collection assertion logic.
/// All collection assertions delegate to these static methods.
/// </summary>
public static class CollectionChecks
{
    /// <summary>
    /// Checks if the collection is empty.
    /// </summary>
    public static AssertionResult CheckIsEmpty<TItem>(ICollectionAdapter<TItem> adapter)
    {
        if (adapter.IsEmpty)
        {
            return AssertionResult.Passed;
        }

        var preview = GetPreview(adapter, maxItems: 10);
        return AssertionResult.Failed($"collection contains items: [{preview}]");
    }

    /// <summary>
    /// Checks if the collection is not empty.
    /// </summary>
    public static AssertionResult CheckIsNotEmpty<TItem>(ICollectionAdapter<TItem> adapter)
    {
        if (!adapter.IsEmpty)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("it was empty");
    }

    /// <summary>
    /// Checks if the collection contains the expected item.
    /// </summary>
    public static AssertionResult CheckContains<TItem>(
        ICollectionAdapter<TItem> adapter,
        TItem expected,
        IEqualityComparer<TItem>? comparer = null)
    {
        // Use optimized path if adapter supports IContainsCheck
        if (adapter is IContainsCheck<TItem> containsCheck)
        {
            if (containsCheck.Contains(expected, comparer))
            {
                return AssertionResult.Passed;
            }
        }
        else
        {
            comparer ??= EqualityComparer<TItem>.Default;
            foreach (var item in adapter.AsEnumerable())
            {
                if (comparer.Equals(item, expected))
                {
                    return AssertionResult.Passed;
                }
            }
        }

        return AssertionResult.Failed($"the item was not found in the collection");
    }

    /// <summary>
    /// Checks if the collection does not contain the expected item.
    /// </summary>
    public static AssertionResult CheckDoesNotContain<TItem>(
        ICollectionAdapter<TItem> adapter,
        TItem expected,
        IEqualityComparer<TItem>? comparer = null)
    {
        // Use optimized path if adapter supports IContainsCheck
        if (adapter is IContainsCheck<TItem> containsCheck)
        {
            if (!containsCheck.Contains(expected, comparer))
            {
                return AssertionResult.Passed;
            }
        }
        else
        {
            comparer ??= EqualityComparer<TItem>.Default;
            var found = false;
            foreach (var item in adapter.AsEnumerable())
            {
                if (comparer.Equals(item, expected))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return AssertionResult.Passed;
            }
        }

        return AssertionResult.Failed($"the item was found in the collection");
    }

    /// <summary>
    /// Checks if the collection does not contain any item matching the predicate.
    /// </summary>
    public static AssertionResult CheckDoesNotContainPredicate<TItem>(
        ICollectionAdapter<TItem> adapter,
        Func<TItem, bool> predicate)
    {
        foreach (var item in adapter.AsEnumerable())
        {
            if (predicate(item))
            {
                return AssertionResult.Failed("found item matching predicate");
            }
        }

        return AssertionResult.Passed;
    }

    /// <summary>
    /// Checks if the collection contains an item matching the predicate.
    /// Returns the found item via out parameter.
    /// </summary>
    public static AssertionResult CheckContainsPredicate<TItem>(
        ICollectionAdapter<TItem> adapter,
        Func<TItem, bool> predicate,
        out TItem? foundItem)
    {
        foreach (var item in adapter.AsEnumerable())
        {
            if (predicate(item))
            {
                foundItem = item;
                return AssertionResult.Passed;
            }
        }

        foundItem = default;
        return AssertionResult.Failed("no item matching predicate found in collection");
    }

    /// <summary>
    /// Checks if the collection has the expected count.
    /// </summary>
    public static AssertionResult CheckCount<TItem>(ICollectionAdapter<TItem> adapter, int expected)
    {
        var actual = adapter.Count;
        if (actual == expected)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"found {actual}");
    }

    /// <summary>
    /// Checks if the collection has exactly one item.
    /// </summary>
    public static AssertionResult CheckHasSingleItem<TItem>(ICollectionAdapter<TItem> adapter)
    {
        var count = adapter.Count;
        if (count == 1)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"it had {count} item(s)");
    }

    /// <summary>
    /// Checks if all items satisfy the predicate.
    /// </summary>
    public static AssertionResult CheckAll<TItem>(
        ICollectionAdapter<TItem> adapter,
        Func<TItem, bool> predicate,
        string predicateExpression)
    {
        var index = 0;
        foreach (var item in adapter.AsEnumerable())
        {
            if (!predicate(item))
            {
                return AssertionResult.Failed($"item at index {index} with value [{item}] does not satisfy predicate");
            }
            index++;
        }

        return AssertionResult.Passed;
    }

    /// <summary>
    /// Checks if any item satisfies the predicate.
    /// </summary>
    public static AssertionResult CheckAny<TItem>(
        ICollectionAdapter<TItem> adapter,
        Func<TItem, bool> predicate)
    {
        foreach (var item in adapter.AsEnumerable())
        {
            if (predicate(item))
            {
                return AssertionResult.Passed;
            }
        }

        return AssertionResult.Failed("no item satisfied the condition");
    }

    /// <summary>
    /// Checks if the collection is in ascending order.
    /// </summary>
    public static AssertionResult CheckIsInOrder<TItem>(
        ICollectionAdapter<TItem> adapter,
        IComparer<TItem>? comparer = null)
    {
        comparer ??= Comparer<TItem>.Default;

        using var enumerator = adapter.AsEnumerable().GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return AssertionResult.Passed; // Empty collection is ordered
        }

        var previous = enumerator.Current;
        var index = 1;

        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            if (comparer.Compare(previous, current) > 0)
            {
                return AssertionResult.Failed(
                    $"item at index {index} ({current}) was less than previous item ({previous})");
            }
            previous = current;
            index++;
        }

        return AssertionResult.Passed;
    }

    /// <summary>
    /// Checks if the collection is in descending order.
    /// </summary>
    public static AssertionResult CheckIsInDescendingOrder<TItem>(
        ICollectionAdapter<TItem> adapter,
        IComparer<TItem>? comparer = null)
    {
        comparer ??= Comparer<TItem>.Default;

        using var enumerator = adapter.AsEnumerable().GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return AssertionResult.Passed; // Empty collection is ordered
        }

        var previous = enumerator.Current;
        var index = 1;

        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            if (comparer.Compare(previous, current) < 0)
            {
                return AssertionResult.Failed(
                    $"item at index {index} ({current}) was greater than previous item ({previous})");
            }
            previous = current;
            index++;
        }

        return AssertionResult.Passed;
    }

    /// <summary>
    /// Checks if all items in the collection are distinct.
    /// </summary>
    public static AssertionResult CheckHasDistinctItems<TItem>(
        ICollectionAdapter<TItem> adapter,
        IEqualityComparer<TItem>? comparer = null)
    {
        comparer ??= EqualityComparer<TItem>.Default;
        var seen = new HashSet<TItem>(comparer);
        var index = 0;

        foreach (var item in adapter.AsEnumerable())
        {
            if (!seen.Add(item))
            {
                return AssertionResult.Failed($"duplicate item found at index {index}: {item}");
            }
            index++;
        }

        return AssertionResult.Passed;
    }

    /// <summary>
    /// Gets a preview string of the first few items in the collection.
    /// Format matches the original: "1, 2, 3, 4, 5, 6, 7, 8, 9, 10, and 5 more..."
    /// </summary>
    private static string GetPreview<TItem>(ICollectionAdapter<TItem> adapter, int maxItems)
    {
        var totalCount = adapter.Count;
        var items = adapter.AsEnumerable().Take(maxItems).ToList();

        var preview = string.Join(", ", items);

        if (totalCount > maxItems)
        {
            var remainingCount = totalCount - maxItems;
            preview += $", and {remainingCount} more...";
        }

        return preview;
    }

    // ========================================
    // Dictionary-specific checks
    // ========================================

    /// <summary>
    /// Checks if the dictionary contains the specified key.
    /// </summary>
    public static AssertionResult CheckContainsKey<TKey, TValue>(
        IDictionaryAdapter<TKey, TValue> adapter,
        TKey key)
    {
        if (adapter.ContainsKey(key))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"the key [{key}] was not found in the dictionary");
    }

    /// <summary>
    /// Checks if the dictionary does not contain the specified key.
    /// </summary>
    public static AssertionResult CheckDoesNotContainKey<TKey, TValue>(
        IDictionaryAdapter<TKey, TValue> adapter,
        TKey key)
    {
        if (!adapter.ContainsKey(key))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"the key [{key}] was found in the dictionary");
    }

    /// <summary>
    /// Checks if the dictionary contains the specified value.
    /// </summary>
    public static AssertionResult CheckContainsValue<TKey, TValue>(
        IDictionaryAdapter<TKey, TValue> adapter,
        TValue value,
        IEqualityComparer<TValue>? comparer = null)
    {
        if (adapter.ContainsValue(value, comparer))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"the value [{value}] was not found in the dictionary");
    }

    /// <summary>
    /// Checks if the dictionary does not contain the specified value.
    /// </summary>
    public static AssertionResult CheckDoesNotContainValue<TKey, TValue>(
        IDictionaryAdapter<TKey, TValue> adapter,
        TValue value,
        IEqualityComparer<TValue>? comparer = null)
    {
        if (!adapter.ContainsValue(value, comparer))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"the value [{value}] was found in the dictionary");
    }

    /// <summary>
    /// Checks if the dictionary contains the specified key with the specified value.
    /// </summary>
    public static AssertionResult CheckContainsKeyWithValue<TKey, TValue>(
        IDictionaryAdapter<TKey, TValue> adapter,
        TKey key,
        TValue expectedValue,
        IEqualityComparer<TValue>? comparer = null)
    {
        if (!adapter.TryGetValue(key, out var actualValue))
        {
            return AssertionResult.Failed($"the key [{key}] was not found in the dictionary");
        }

        comparer ??= EqualityComparer<TValue>.Default;
        if (comparer.Equals(actualValue!, expectedValue))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"the key [{key}] had value [{actualValue}] instead of [{expectedValue}]");
    }

    /// <summary>
    /// Checks if all keys in the dictionary satisfy the predicate.
    /// </summary>
    public static AssertionResult CheckAllKeys<TKey, TValue>(
        IDictionaryAdapter<TKey, TValue> adapter,
        Func<TKey, bool> predicate)
    {
        foreach (var key in adapter.Keys)
        {
            if (!predicate(key))
            {
                return AssertionResult.Failed($"key [{key}] does not satisfy the predicate");
            }
        }

        return AssertionResult.Passed;
    }

    /// <summary>
    /// Checks if all values in the dictionary satisfy the predicate.
    /// </summary>
    public static AssertionResult CheckAllValues<TKey, TValue>(
        IDictionaryAdapter<TKey, TValue> adapter,
        Func<TValue, bool> predicate)
    {
        foreach (var value in adapter.Values)
        {
            if (!predicate(value))
            {
                return AssertionResult.Failed($"value [{value}] does not satisfy the predicate");
            }
        }

        return AssertionResult.Passed;
    }

    /// <summary>
    /// Checks if any key in the dictionary satisfies the predicate.
    /// </summary>
    public static AssertionResult CheckAnyKey<TKey, TValue>(
        IDictionaryAdapter<TKey, TValue> adapter,
        Func<TKey, bool> predicate)
    {
        foreach (var key in adapter.Keys)
        {
            if (predicate(key))
            {
                return AssertionResult.Passed;
            }
        }

        return AssertionResult.Failed("no key satisfied the predicate");
    }

    /// <summary>
    /// Checks if any value in the dictionary satisfies the predicate.
    /// </summary>
    public static AssertionResult CheckAnyValue<TKey, TValue>(
        IDictionaryAdapter<TKey, TValue> adapter,
        Func<TValue, bool> predicate)
    {
        foreach (var value in adapter.Values)
        {
            if (predicate(value))
            {
                return AssertionResult.Passed;
            }
        }

        return AssertionResult.Failed("no value satisfied the predicate");
    }

    // ========================================
    // Set-specific checks
    // ========================================

    /// <summary>
    /// Checks if the set is a subset of the specified collection.
    /// </summary>
    public static AssertionResult CheckIsSubsetOf<TItem>(
        ISetAdapter<TItem> adapter,
        IEnumerable<TItem> other)
    {
        if (adapter.IsSubsetOf(other))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("the set is not a subset of the specified collection");
    }

    /// <summary>
    /// Checks if the set is a superset of the specified collection.
    /// </summary>
    public static AssertionResult CheckIsSupersetOf<TItem>(
        ISetAdapter<TItem> adapter,
        IEnumerable<TItem> other)
    {
        if (adapter.IsSupersetOf(other))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("the set is not a superset of the specified collection");
    }

    /// <summary>
    /// Checks if the set is a proper subset of the specified collection.
    /// </summary>
    public static AssertionResult CheckIsProperSubsetOf<TItem>(
        ISetAdapter<TItem> adapter,
        IEnumerable<TItem> other)
    {
        if (adapter.IsProperSubsetOf(other))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("the set is not a proper subset of the specified collection");
    }

    /// <summary>
    /// Checks if the set is a proper superset of the specified collection.
    /// </summary>
    public static AssertionResult CheckIsProperSupersetOf<TItem>(
        ISetAdapter<TItem> adapter,
        IEnumerable<TItem> other)
    {
        if (adapter.IsProperSupersetOf(other))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("the set is not a proper superset of the specified collection");
    }

    /// <summary>
    /// Checks if the set overlaps with the specified collection.
    /// </summary>
    public static AssertionResult CheckOverlaps<TItem>(
        ISetAdapter<TItem> adapter,
        IEnumerable<TItem> other)
    {
        if (adapter.Overlaps(other))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("the set does not overlap with the specified collection");
    }

    /// <summary>
    /// Checks if the set does not overlap with the specified collection.
    /// </summary>
    public static AssertionResult CheckDoesNotOverlap<TItem>(
        ISetAdapter<TItem> adapter,
        IEnumerable<TItem> other)
    {
        if (!adapter.Overlaps(other))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("the set overlaps with the specified collection");
    }

    /// <summary>
    /// Checks if the set equals the specified collection (same elements, regardless of order).
    /// </summary>
    public static AssertionResult CheckSetEquals<TItem>(
        ISetAdapter<TItem> adapter,
        IEnumerable<TItem> other)
    {
        if (adapter.SetEquals(other))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("the set does not equal the specified collection");
    }
}
