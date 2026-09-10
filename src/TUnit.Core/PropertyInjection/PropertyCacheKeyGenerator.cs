using System.Reflection;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core.PropertyInjection;

/// <summary>
/// Generates consistent cache keys for property injection values.
/// Centralizes cache key generation to ensure consistency across the codebase (DRY principle).
/// </summary>
/// <remarks>
/// Cache keys are formatted as "{DeclaringTypeName}.{PropertyName}" to uniquely identify
/// properties across different types. This format is used for storing and retrieving
/// injected property values in test contexts.
/// </remarks>
public static class PropertyCacheKeyGenerator
{
    /// <summary>
    /// Generates a cache key from source-generated property metadata.
    /// </summary>
    /// <param name="metadata">The property injection metadata from source generation.</param>
    /// <returns>A unique cache key string for the property.</returns>
    public static string GetCacheKey(PropertyInjectionMetadata metadata)
    {
        return $"{metadata.ContainingType.FullName}.{metadata.PropertyName}";
    }

    /// <summary>
    /// Generates a cache key from a PropertyInfo (reflection-based properties).
    /// </summary>
    /// <param name="property">The PropertyInfo from reflection.</param>
    /// <returns>A unique cache key string for the property.</returns>
    public static string GetCacheKey(PropertyInfo property)
    {
        return $"{property.DeclaringType!.FullName}.{property.Name}";
    }
}
