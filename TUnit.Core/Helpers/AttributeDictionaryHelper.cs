using System.Collections.ObjectModel;

namespace TUnit.Core.Helpers;

/// <summary>
/// Helper methods for working with attribute dictionaries.
/// </summary>
public static class AttributeDictionaryHelper
{
    private static readonly IReadOnlyDictionary<Type, IReadOnlyList<Attribute>> EmptyDictionary =
        new ReadOnlyDictionary<Type, IReadOnlyList<Attribute>>(new Dictionary<Type, IReadOnlyList<Attribute>>());

    /// <summary>
    /// Converts an array of attributes to a read-only dictionary grouped by type.
    /// </summary>
    public static IReadOnlyDictionary<Type, IReadOnlyList<Attribute>> ToAttributeDictionary(this Attribute[] attributes)
    {
        if (attributes.Length == 0)
        {
            return EmptyDictionary;
        }

        var result = new Dictionary<Type, IReadOnlyList<Attribute>>();

        foreach (var attr in attributes)
        {
            var type = attr.GetType();
            if (!result.TryGetValue(type, out var list))
            {
                var newList = new List<Attribute> { attr };
                result[type] = newList;
            }
            else
            {
                ((List<Attribute>)list).Add(attr);
            }
        }

        return new ReadOnlyDictionary<Type, IReadOnlyList<Attribute>>(result);
    }

    /// <summary>
    /// Gets an empty read-only attribute dictionary.
    /// </summary>
    public static IReadOnlyDictionary<Type, IReadOnlyList<Attribute>> Empty => EmptyDictionary;
}
