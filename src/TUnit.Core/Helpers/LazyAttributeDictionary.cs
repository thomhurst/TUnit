using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Helpers;

/// <summary>
/// An attributes-by-type view that defers building its dictionary until something enumerates or
/// looks up a list. <see cref="ContainsKey"/> is answered by scanning the (small) attribute array,
/// so the common engine query — "does this test have attribute X?" — never materializes it.
/// Most tests never read <see cref="TestDetails.AttributesByType"/> beyond that, which saves a
/// dictionary, its buckets/entries and a per-type array for every test.
/// </summary>
internal sealed class LazyAttributeDictionary : IReadOnlyDictionary<Type, IReadOnlyList<Attribute>>
{
    private readonly Attribute[] _attributes;
    private IReadOnlyDictionary<Type, IReadOnlyList<Attribute>>? _dictionary;

    public LazyAttributeDictionary(Attribute[] attributes)
    {
        _attributes = attributes;
    }

    private IReadOnlyDictionary<Type, IReadOnlyList<Attribute>> Dictionary
    {
        get
        {
            if (Volatile.Read(ref _dictionary) is { } existing)
            {
                return existing;
            }

            var created = _attributes.ToAttributeDictionary();
            return Interlocked.CompareExchange(ref _dictionary, created, null) ?? created;
        }
    }

    public bool ContainsKey(Type key)
    {
        if (Volatile.Read(ref _dictionary) is { } existing)
        {
            return existing.ContainsKey(key);
        }

        foreach (var attribute in _attributes)
        {
            if (attribute.GetType() == key)
            {
                return true;
            }
        }

        return false;
    }

#if NET
    public bool TryGetValue(Type key, [MaybeNullWhen(false)] out IReadOnlyList<Attribute> value)
#else
    public bool TryGetValue(Type key, out IReadOnlyList<Attribute> value)
#endif
        => Dictionary.TryGetValue(key, out value!);

    public IReadOnlyList<Attribute> this[Type key] => Dictionary[key];

    public IEnumerable<Type> Keys => Dictionary.Keys;

    public IEnumerable<IReadOnlyList<Attribute>> Values => Dictionary.Values;

    public int Count => Dictionary.Count;

    public IEnumerator<KeyValuePair<Type, IReadOnlyList<Attribute>>> GetEnumerator() => Dictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
