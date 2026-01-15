namespace TUnit.Core.Extensions;

internal static class DictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, TValue> valueFactory)
    {
        if (!dictionary.TryGetValue(key, out TValue? value))
        {
            value = valueFactory(key);
            dictionary.Add(key, value);
        }

        return value;
    }
}
