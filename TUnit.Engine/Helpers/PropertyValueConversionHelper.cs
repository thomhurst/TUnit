using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Helpers;

internal static class PropertyValueConversionHelper
{
    private static readonly ConcurrentDictionary<(Type Source, Type Target), bool> RequiresConversionCache = new();

    /// <summary>
    /// Converts a resolved property value to the target property type if needed.
    /// This handles implicit/explicit conversion operators at runtime, which is necessary when:
    /// - The source generator doesn't know the data source type (e.g., custom data sources)
    /// - The data source yields a type that differs from the property type but has a conversion operator
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "CastHelper handles AOT scenarios with proper fallbacks")]
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "PropertyType is preserved through source generation or reflection discovery")]
    public static object? ConvertIfNeeded(object? value, Type targetType)
    {
        if (value == null)
        {
            return null;
        }

        var valueType = value.GetType();
        var requiresConversion = RequiresConversionCache.GetOrAdd(
            (valueType, targetType),
            static key => !key.Item1.IsAssignableTo(key.Item2));

        if (!requiresConversion)
        {
            return value;
        }

        // Use CastHelper which supports implicit/explicit operators, IConvertible, etc.
        return CastHelper.Cast(targetType, value);
    }
}
