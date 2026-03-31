using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Converts property values to target types at runtime using implicit/explicit operators.
/// Needed when the source generator doesn't know the data source type (e.g., custom data sources).
/// In Native AOT mode, CastHelper.Cast throws InvalidCastException when reflection-based
/// operator discovery is unavailable.
/// </summary>
internal static class PropertyValueConversionHelper
{
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "CastHelper handles AOT scenarios by throwing an informative InvalidCastException when reflection-based conversion is unavailable")]
    public static object? ConvertIfNeeded(
        object? value,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
            | DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicMethods)] Type targetType)
    {
        if (value is null || value.GetType().IsAssignableTo(targetType))
        {
            return value;
        }

        return CastHelper.Cast(targetType, value);
    }
}
