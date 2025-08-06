using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core;

/// <summary>
/// Unified registry for property injection sources - supports both source generation and legacy array-based APIs
/// </summary>
public static class PropertySourceRegistry
{
    private static readonly ConcurrentDictionary<Type, IPropertySource> _sources = new();

    /// <summary>
    /// Registers a property source for a type. Called by generated code.
    /// </summary>
    public static void Register(Type type, IPropertySource source)
    {
        _sources[type] = source;
    }

    /// <summary>
    /// Gets a property source for the given type
    /// </summary>
    public static IPropertySource? GetSource(Type type)
    {
        return _sources.TryGetValue(type, out var source) ? source : null;
    }

    /// <summary>
    /// Gets all registered sources (for debugging/testing)
    /// </summary>
    public static IEnumerable<(Type Type, IPropertySource Source)> GetAllSources()
    {
        return _sources.Select(kvp => (kvp.Key, kvp.Value));
    }

    /// <summary>
    /// Gets property injection data in the legacy format for backward compatibility
    /// </summary>
    public static PropertyInjectionData[]? GetPropertyInjectionData(Type type)
    {
        var source = GetSource(type);
        if (source?.ShouldInitialize != true)
        {
            return null;
        }

        var metadata = source.GetPropertyMetadata().ToArray();
        if (metadata.Length == 0)
        {
            return null;
        }

        return metadata.Select(ConvertToPropertyInjectionData).ToArray();
    }

    /// <summary>
    /// Gets property data sources in the legacy format for backward compatibility
    /// </summary>
    public static PropertyDataSource[]? GetPropertyDataSources(Type type)
    {
        var source = GetSource(type);
        if (source?.ShouldInitialize != true)
        {
            return null;
        }

        var metadata = source.GetPropertyMetadata().ToArray();
        if (metadata.Length == 0)
        {
            return null;
        }

        return metadata.Select(ConvertToPropertyDataSource).ToArray();
    }

    /// <summary>
    /// Discovers injectable properties using reflection (legacy compatibility)
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Legacy reflection fallback")]
    public static PropertyInjectionData[] DiscoverInjectableProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] Type type)
    {
        // First try source-generated data
        var sourceGenerated = GetPropertyInjectionData(type);
        if (sourceGenerated != null)
        {
            return sourceGenerated;
        }

        // Fall back to reflection discovery
        var injectableProperties = new List<PropertyInjectionData>();

        foreach (var property in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
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
                    throw new InvalidOperationException($"Cannot create property injection for '{property.Name}' on type '{type.Name}': {ex.Message}", ex);
                }
            }
        }

        return injectableProperties.ToArray();
    }

    /// <summary>
    /// Converts PropertyInjectionMetadata to PropertyInjectionData for backward compatibility
    /// </summary>
    private static PropertyInjectionData ConvertToPropertyInjectionData(PropertyInjectionMetadata metadata)
    {
        return new PropertyInjectionData
        {
            PropertyName = metadata.PropertyName,
            PropertyType = metadata.PropertyType,
            Setter = metadata.SetProperty,
            ValueFactory = () => throw new InvalidOperationException("Value factory should be provided by data source"),
            NestedPropertyInjections = [], // Will be populated by recursive calls
            NestedPropertyValueFactory = obj => new Dictionary<string, object?>()
        };
    }

    /// <summary>
    /// Converts PropertyInjectionMetadata to PropertyDataSource for backward compatibility
    /// </summary>
    private static PropertyDataSource ConvertToPropertyDataSource(PropertyInjectionMetadata metadata)
    {
        return new PropertyDataSource
        {
            PropertyName = metadata.PropertyName,
            PropertyType = metadata.PropertyType,
            DataSource = metadata.CreateDataSource()
        };
    }

    /// <summary>
    /// Creates PropertyInjectionData from PropertyInfo (legacy compatibility)
    /// </summary>
    private static PropertyInjectionData CreatePropertyInjection(System.Reflection.PropertyInfo property)
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
    /// Creates property setter (legacy compatibility)
    /// </summary>
    private static Action<object, object?> CreatePropertySetter(System.Reflection.PropertyInfo property)
    {
        if (property.CanWrite && property.SetMethod != null)
        {
#if NETSTANDARD2_0
            return (instance, value) => property.SetValue(instance, value);
#else
            var setMethod = property.SetMethod;
            var isInitOnly = IsInitOnlyMethod(setMethod);

            if (!isInitOnly)
            {
                return (instance, value) => property.SetValue(instance, value);
            }
#endif
        }

        var backingField = GetBackingField(property);
        if (backingField != null)
        {
            return (instance, value) => backingField.SetValue(instance, value);
        }

        throw new InvalidOperationException(
            $"Property '{property.Name}' on type '{property.DeclaringType?.Name}' " +
            $"is not writable and no backing field was found.");
    }

    /// <summary>
    /// Gets backing field for property (legacy compatibility)
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Legacy reflection fallback")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Legacy reflection fallback")]
    private static System.Reflection.FieldInfo? GetBackingField(System.Reflection.PropertyInfo property)
    {
        var declaringType = property.DeclaringType;
        if (declaringType == null)
        {
            return null;
        }

        var backingFieldFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.FlattenHierarchy;

        var backingFieldName = $"<{property.Name}>k__BackingField";
        var field = GetFieldSafe(declaringType, backingFieldName, backingFieldFlags);

        if (field != null)
        {
            return field;
        }

        var underscoreName = "_" + char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
        field = GetFieldSafe(declaringType, underscoreName, backingFieldFlags);

        if (field != null && field.FieldType == property.PropertyType)
        {
            return field;
        }

        field = GetFieldSafe(declaringType, property.Name, backingFieldFlags);

        if (field != null && field.FieldType == property.PropertyType)
        {
            return field;
        }

        return null;
    }

    /// <summary>
    /// Helper method to get field with proper trimming suppression
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Legacy reflection fallback")]
    private static System.Reflection.FieldInfo? GetFieldSafe([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] Type type, string name, System.Reflection.BindingFlags bindingFlags)
    {
        return type.GetField(name, bindingFlags);
    }

    /// <summary>
    /// Checks if method is init-only (legacy compatibility)
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Legacy reflection fallback")]
    private static bool IsInitOnlyMethod(System.Reflection.MethodInfo setMethod)
    {
        var methodType = setMethod.GetType();
        var isInitOnlyProperty = methodType.GetProperty("IsInitOnly");
        return isInitOnlyProperty != null && (bool)isInitOnlyProperty.GetValue(setMethod)!;
    }
}