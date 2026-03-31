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
    /// <list type="bullet">
    /// <item><description>The source generator doesn't know the data source type (e.g., custom data sources)</description></item>
    /// <item><description>The data source yields a type that differs from the property type but has a conversion operator</description></item>
    /// </list>
    /// <para>
    /// <b>AOT limitation:</b> In Native AOT mode, reflection-based operator discovery is not available.
    /// <c>CastHelper.Cast</c> will throw an informative <see cref="InvalidCastException"/> when a
    /// runtime conversion is needed but cannot be performed without reflection. This affects custom
    /// data sources that produce types requiring user-defined conversion operators.
    /// </para>
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "CastHelper handles AOT scenarios by throwing an informative InvalidCastException when reflection-based conversion is unavailable")]
    public static object? ConvertIfNeeded(
        object? value,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
            | DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicMethods)] Type targetType)
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
