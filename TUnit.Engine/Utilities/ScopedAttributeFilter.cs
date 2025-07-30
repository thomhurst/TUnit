using System.Diagnostics.CodeAnalysis;
using TUnit.Core;

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
    [UnconditionalSuppressMessage("Trimming", "IL2075:UnrecognizedReflectionPattern",
        Justification = "IScopedAttribute types are preserved by the source generator")]
    public static List<T> FilterScopedAttributes<T>(IEnumerable<T> items) where T : class
    {
        var result = new List<T>();
        var scopedAttributesByType = new Dictionary<Type, T>();

        // First pass: collect all scoped attributes, keeping only the first occurrence of each type
        foreach (var item in items)
        {
            if (item == null)
                continue;
                
            var itemType = item.GetType();
            
            // Check if this implements IScopedAttribute<T>
            var scopedInterface = itemType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IScopedAttribute<>));
            
            if (scopedInterface != null)
            {
                // Get the generic type argument (e.g., RetryAttribute from IScopedAttribute<RetryAttribute>)
                var scopedType = scopedInterface.GetGenericArguments()[0];
                
                
                // Keep the first occurrence (which should be the most specific - method > class > assembly)
                if (!scopedAttributesByType.ContainsKey(scopedType))
                {
                    scopedAttributesByType[scopedType] = item;
                }
            }
            else
            {
                // Not a scoped attribute, include it immediately
                result.Add(item);
            }
        }

        // Second pass: add the selected scoped attributes to the result
        result.AddRange(scopedAttributesByType.Values);

        return result;
    }
    
    /// <summary>
    /// Checks if a type implements IScopedAttribute<T>
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern",
        Justification = "IScopedAttribute types are preserved by the source generator")]
    [UnconditionalSuppressMessage("Trimming", "IL2075:UnrecognizedReflectionPattern",
        Justification = "IScopedAttribute types are preserved by the source generator")]
    public static bool IsScopedAttribute(Type type)
    {
        return type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IScopedAttribute<>));
    }
    
    /// <summary>
    /// Gets the scoped attribute type from an object that implements IScopedAttribute<T>
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075:UnrecognizedReflectionPattern",
        Justification = "IScopedAttribute types are preserved by the source generator")]
    public static Type? GetScopedAttributeType(object obj)
    {
        if (obj == null)
            return null;
            
        var objType = obj.GetType();
        var scopedInterface = objType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IScopedAttribute<>));
            
        return scopedInterface?.GetGenericArguments()[0];
    }
}