using TUnit.Core;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Utilities;

/// <summary>
/// Provides filtering logic for attributes that implement IScopedAttribute<T>
/// </summary>
internal static class ScopedAttributeFilter
{
    /// <summary>
    /// Filters a collection of objects to ensure only one instance per IScopedAttribute<T> type is included
    /// </summary>
    /// <typeparam name="T">The type of objects to filter</typeparam>
    /// <param name="items">The collection of items to filter</param>
    /// <returns>A filtered collection with only one instance per scoped attribute type</returns>
    public static T[] FilterScopedAttributes<T>(IEnumerable<T?> items) where T : class
    {
        var vlb = new ValueListBuilder<T>([null,null,null,null]);
        Dictionary<Type, T>? scopedAttributesByType = null;

        // First pass: collect all scoped attributes, keeping only the first occurrence of each type
        foreach (var item in items)
        {
            if (item == null)
            {
                continue;
            }

            if (item is IScopedAttribute scopedAttribute)
            {
                scopedAttributesByType ??= new Dictionary<Type, T>();
                scopedAttributesByType.TryAdd(scopedAttribute.ScopeType, item);
            }
            else
            {
                // Not a scoped attribute, include it immediately
                vlb.Append(item);
            }
        }

        if (scopedAttributesByType != null)
        {
            foreach (var value in scopedAttributesByType.Values)
            {
                vlb.Append(value);
            }
        }

        var result = vlb.AsSpan().ToArray();
        vlb.Dispose();
        return result;
    }
}
