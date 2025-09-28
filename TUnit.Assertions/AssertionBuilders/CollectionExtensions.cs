using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Extension methods for collection assertions that return values
/// </summary>
public static class CollectionExtensions
{
    // HasSingleItem that allows chaining - returns AssertionBuilder for the collection
    public static AssertionBuilder<T> HasSingleItem<T>(this AssertionBuilder<T> builder)
        where T : System.Collections.IEnumerable
    {
        return new AssertionBuilder<T>(async () =>
        {
            var collection = await builder.ActualValueProvider();
            if (collection == null)
                throw new InvalidOperationException("Collection was null");

            var count = 0;
            foreach (var _ in collection)
            {
                count++;
                if (count > 1)
                    throw new InvalidOperationException($"Expected single item but had more than 1 item");
            }

            if (count == 0)
                throw new InvalidOperationException("Expected single item but had 0 items");

            return collection;
        });
    }

    // For List<T> - returns the single item
    public static async Task<T> GetSingleItem<T>(this AssertionBuilder<List<T>> builder)
    {
        var list = await builder.ActualValueProvider();
        if (list == null)
            throw new InvalidOperationException("Collection was null");
        if (list.Count != 1)
            throw new InvalidOperationException($"Expected single item but had {list.Count} items");
        return list[0];
    }

    // For T[] - returns the single item
    public static async Task<T> GetSingleItem<T>(this AssertionBuilder<T[]> builder)
    {
        var array = await builder.ActualValueProvider();
        if (array == null)
            throw new InvalidOperationException("Collection was null");
        if (array.Length != 1)
            throw new InvalidOperationException($"Expected single item but had {array.Length} items");
        return array[0];
    }

    // For IEnumerable<T> - returns the single item
    public static async Task<T> GetSingleItem<T>(this AssertionBuilder<IEnumerable<T>> builder)
    {
        var enumerable = await builder.ActualValueProvider();
        if (enumerable == null)
            throw new InvalidOperationException("Collection was null");
        var list = enumerable.ToList();
        if (list.Count != 1)
            throw new InvalidOperationException($"Expected single item but had {list.Count} items");
        return list[0];
    }

    // For IList<T> - returns the single item
    public static async Task<T> GetSingleItem<T>(this AssertionBuilder<IList<T>> builder)
    {
        var list = await builder.ActualValueProvider();
        if (list == null)
            throw new InvalidOperationException("Collection was null");
        if (list.Count != 1)
            throw new InvalidOperationException($"Expected single item but had {list.Count} items");
        return list[0];
    }

    // For ICollection<T> - returns the single item
    public static async Task<T> GetSingleItem<T>(this AssertionBuilder<ICollection<T>> builder)
    {
        var collection = await builder.ActualValueProvider();
        if (collection == null)
            throw new InvalidOperationException("Collection was null");
        if (collection.Count != 1)
            throw new InvalidOperationException($"Expected single item but had {collection.Count} items");
        return collection.First();
    }

    // For IReadOnlyList<T>
    public static async Task<T> GetSingleItem<T>(this AssertionBuilder<IReadOnlyList<T>> builder)
    {
        var list = await builder.ActualValueProvider();
        if (list == null)
            throw new InvalidOperationException("Collection was null");
        if (list.Count != 1)
            throw new InvalidOperationException($"Expected single item but had {list.Count} items");
        return list[0];
    }

    // For IReadOnlyCollection<T>
    public static async Task<T> GetSingleItem<T>(this AssertionBuilder<IReadOnlyCollection<T>> builder)
    {
        var collection = await builder.ActualValueProvider();
        if (collection == null)
            throw new InvalidOperationException("Collection was null");
        if (collection.Count != 1)
            throw new InvalidOperationException($"Expected single item but had {collection.Count} items");
        return collection.First();
    }
}