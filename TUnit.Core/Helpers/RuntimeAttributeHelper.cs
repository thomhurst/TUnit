using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace TUnit.Core.Helpers;

/// <summary>
/// Runtime helper for creating attribute instances from metadata.
/// This is used in generated code to handle complex attribute instantiation.
/// </summary>
public static class RuntimeAttributeHelper
{
    /// <summary>
    /// Creates an attribute instance using reflection.
    /// </summary>
    public static Attribute CreateAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | 
                                    DynamicallyAccessedMemberTypes.PublicProperties)]
        Type attributeType, 
        object?[]? constructorArgs, 
        Dictionary<string, object?>? namedArgs)
    {
        try
        {
            // Create instance with constructor arguments
            var instance = Activator.CreateInstance(attributeType, constructorArgs ?? Array.Empty<object?>()) as Attribute
                ?? throw new InvalidOperationException($"Failed to create instance of {attributeType}");

            // Set named arguments (properties)
            if (namedArgs != null)
            {
                foreach (var kvp in namedArgs)
                {
                    var property = attributeType.GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(instance, kvp.Value);
                    }
                }
            }

            return instance;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create attribute {attributeType.FullName}: {ex.Message}", ex);
        }
    }
}