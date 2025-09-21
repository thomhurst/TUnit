using System;
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
    /// </summary>
    public static PropertyInfo GetPropertyInfo(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        Type containingType,
        string propertyName)
    {
        var property = containingType.GetProperty(propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                $"Property '{propertyName}' not found on type '{containingType.Name}'");
        }

        return property;
    }
}
