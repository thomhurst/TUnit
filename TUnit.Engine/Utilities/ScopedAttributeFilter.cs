using System.Diagnostics.CodeAnalysis;
using Polyfills;
using TUnit.Core;
using TUnit.Engine.Helpers;

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
    public static List<T> FilterScopedAttributes<T>(IEnumerable<T?> items) where T : class
    {
        var result = new List<T>();
        var scopedAttributesByType = new Dictionary<Type, T>();

        // First pass: collect all scoped attributes, keeping only the first occurrence of each type
        foreach (var item in items)
        {
            if (item == null)
            {
                continue;
            }

            if (item is IScopedAttribute scopedAttribute)
            {
                scopedAttributesByType.TryAdd(scopedAttribute.ScopeType, item);
            }
            else
            {
                // Not a scoped attribute, include it immediately
                result.Add(item);
            }
        }

        result.AddRange(scopedAttributesByType.Values);

        return result;
    }
}
