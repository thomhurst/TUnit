using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.PropertyInjection;

/// <summary>
/// Helper class for property-related operations.
/// Consolidates property reflection logic in one place.
/// </summary>
internal static class PropertyHelper
{
    /// <summary>
    /// Gets PropertyInfo in an AOT-safe manner.
    /// Searches for both public and non-public properties to support internal properties.
    /// </summary>
    public static PropertyInfo GetPropertyInfo(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type containingType,
        string propertyName)
    {
        // Use binding flags to find both public and non-public properties
        // This is necessary to support internal properties on internal classes
        var property = containingType.GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        if (property == null)
        {
            throw new InvalidOperationException(
                $"Property '{propertyName}' not found on type '{containingType.Name}'");
        }

        return property;
    }
}
