#if NET
using System.Runtime.InteropServices;
#endif

namespace TUnit.Engine.Services;

/// <summary>
/// Helpers for the common "group values into a <see cref="Dictionary{TKey, TValue}"/> of lists" pattern.
/// On net6+ this collapses the two hash lookups (TryGetValue + indexer set) into a single
/// ref-returning lookup via <see cref="CollectionsMarshal.GetValueRefOrAddDefault{TKey, TValue}"/>.
/// </summary>
internal static class DictionaryListExtensions
{
    /// <summary>
    /// Appends <paramref name="value"/> to the list stored under <paramref name="key"/>,
    /// creating the list on first use.
    /// </summary>
    public static void AddToList<TKey, TValue>(this Dictionary<TKey, List<TValue>> dictionary, TKey key, TValue value)
        where TKey : notnull
    {
#if NET
        ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out _);
        list ??= [];
        list.Add(value);
#else
        if (!dictionary.TryGetValue(key, out var list))
        {
            list = [];
            dictionary[key] = list;
        }

        list.Add(value);
#endif
    }
}
