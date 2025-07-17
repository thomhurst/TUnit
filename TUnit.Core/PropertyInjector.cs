using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace TUnit.Core;

/// <summary>
/// Unified property injector that handles property injection consistently
/// for both AOT and reflection modes.
/// </summary>
public static class PropertyInjector
{
    private static readonly BindingFlags BackingFieldFlags = 
        BindingFlags.Instance | BindingFlags.NonPublic;

    /// <summary>
    /// Injects property values into a test instance using PropertyInjectionData.
    /// Works for both regular and init-only properties.
    /// </summary>
    public static async Task InjectPropertiesAsync(
        object instance,
        Dictionary<string, object?> propertyValues,
        PropertyInjectionData[] injectionData)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        // Use PropertyInjectionData if available (preferred path)
        if (injectionData != null && injectionData.Length > 0)
        {
            foreach (var injection in injectionData)
            {
                if (propertyValues.TryGetValue(injection.PropertyName, out var value))
                {
                    injection.Setter(instance, value);
                }
            }
        }
        else
        {
            // Fallback to reflection if no injection data (for compatibility)
            InjectPropertiesViaReflection(instance, propertyValues);
        }

        // Initialize any data source properties
        await ObjectInitializer.InitializeAsync(instance);
    }

    /// <summary>
    /// Creates PropertyInjectionData for a property, handling both regular and init-only properties.
    /// Used by reflection mode to generate the same metadata as AOT mode.
    /// </summary>
    public static PropertyInjectionData CreatePropertyInjection(PropertyInfo property)
    {
        var setter = CreatePropertySetter(property);
        
        return new PropertyInjectionData
        {
            PropertyName = property.Name,
            PropertyType = property.PropertyType,
            Setter = setter,
            ValueFactory = () => throw new InvalidOperationException(
                $"Property value factory should be provided by TestDataCombination for {property.Name}")
        };
    }

    /// <summary>
    /// Creates a setter delegate for a property, handling init-only properties via backing fields.
    /// </summary>
    public static Action<object, object?> CreatePropertySetter(PropertyInfo property)
    {
        // Check if property has a regular setter
        if (property.CanWrite && property.SetMethod != null)
        {
#if NETSTANDARD2_0
            // In netstandard2.0, all writable properties can be set normally
            return (instance, value) => property.SetValue(instance, value);
#else
            // In .NET 6 and later, check for init-only properties
            // IsInitOnly is only available in .NET 6+
            var setMethod = property.SetMethod;
            var isInitOnly = IsInitOnlyMethod(setMethod);
            
            if (!isInitOnly)
            {
                // Regular property - use normal setter
                return (instance, value) => property.SetValue(instance, value);
            }
#endif
        }

        // Init-only or readonly property - use backing field
        var backingField = GetBackingField(property);
        if (backingField != null)
        {
            return (instance, value) => backingField.SetValue(instance, value);
        }

        // No setter available
        throw new InvalidOperationException(
            $"Property '{property.Name}' on type '{property.DeclaringType?.Name}' " +
            $"is not writable and no backing field was found.");
    }

    /// <summary>
    /// Gets the backing field for a property, typically for init-only properties.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Property backing field access requires reflection")]
    private static FieldInfo? GetBackingField(PropertyInfo property)
    {
        if (property.DeclaringType == null)
            return null;

        // Try compiler-generated backing field pattern
        var backingFieldName = $"<{property.Name}>k__BackingField";
        var field = GetField(property.DeclaringType, backingFieldName, BackingFieldFlags);
        
        if (field != null)
            return field;

        // Try underscore prefix pattern
        var underscoreName = "_" + char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
        field = GetField(property.DeclaringType, underscoreName, BackingFieldFlags);
        
        if (field != null && field.FieldType == property.PropertyType)
            return field;

        // Try exact name match
        field = GetField(property.DeclaringType, property.Name, BackingFieldFlags);
        
        if (field != null && field.FieldType == property.PropertyType)
            return field;

        return null;
    }

    /// <summary>
    /// Fallback method to inject properties via reflection when no PropertyInjectionData is available.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Property injection requires reflection access to object type")]
    private static void InjectPropertiesViaReflection(object instance, Dictionary<string, object?> propertyValues)
    {
        var type = instance.GetType();
        
        foreach (var kvp in propertyValues)
        {
            var property = GetProperty(type, kvp.Key);
            if (property == null)
                continue;

            try
            {
                var setter = CreatePropertySetter(property);
                setter(instance, kvp.Value);
            }
            catch (Exception ex)
            {
                // Property injection failure is a serious configuration error - rethrow with context
                throw new InvalidOperationException($"Failed to inject property '{kvp.Key}' on type '{type.Name}': {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Discovers properties with data source attributes on a type.
    /// Used by reflection mode to match AOT behavior.
    /// </summary>
    public static PropertyInjectionData[] DiscoverInjectableProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        var injectableProperties = new List<PropertyInjectionData>();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Check if property has any data source attributes
            var attributes = property.GetCustomAttributes(true);
            var hasDataSource = attributes.Any(attr => 
                attr.GetType().Name.Contains("DataSource") || 
                attr.GetType().Name == "ArgumentsAttribute");

            if (hasDataSource)
            {
                try
                {
                    var injection = CreatePropertyInjection(property);
                    injectableProperties.Add(injection);
                }
                catch (Exception ex)
                {
                    // Property injection creation failure indicates a serious configuration issue - rethrow with context
                    throw new InvalidOperationException($"Cannot create property injection for '{property.Name}' on type '{type.Name}': {ex.Message}", ex);
                }
            }
        }

        return injectableProperties.ToArray();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Property injection requires reflection access")]
    private static FieldInfo? GetField([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] Type type, string name, BindingFlags bindingFlags)
    {
        return type.GetField(name, bindingFlags);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Property injection requires reflection access")]
    private static PropertyInfo? GetProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, string name)
    {
        return type.GetProperty(name);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Checking IsInitOnly property requires reflection")]
    private static bool IsInitOnlyMethod(MethodInfo setMethod)
    {
        var methodType = setMethod.GetType();
        var isInitOnlyProperty = methodType.GetProperty("IsInitOnly");
        return isInitOnlyProperty != null && (bool)isInitOnlyProperty.GetValue(setMethod)!;
    }
}