using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using TUnit.Core.Enums;
using TUnit.Core.ReferenceTracking;

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
    /// Injects property values into a test instance by resolving data sources.
    /// Works for both regular and init-only properties.
    /// </summary>
    public static async Task InjectPropertiesAsync(
        TestContext testContext,
        object instance,
        PropertyDataSource[] propertyDataSources,
        PropertyInjectionData[] injectionData,
        MethodMetadata testInformation,
        string testSessionId)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance), "Test instance cannot be null");
        }

        // Create DataGeneratorMetadata for property data source resolution
        var dataGeneratorMetadata = new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(TestBuilderContext.Current ?? new TestBuilderContext()),
            MembersToGenerate = Array.Empty<MemberMetadata>(), // Properties don't need member generation
            TestInformation = testInformation,
            Type = DataGeneratorType.Property,
            TestSessionId = testSessionId,
            TestClassInstance = instance,
            ClassInstanceArguments = testContext.TestDetails.TestClassArguments
        };

        // Create a dictionary to hold resolved property values
        var propertyValues = new Dictionary<string, object?>();

        // Resolve each property data source
        foreach (var propertyDataSource in propertyDataSources)
        {
            try
            {
                // For reflection mode, discover and inject properties dynamically
                await InjectDataSourcePropertiesAsync(testContext, propertyDataSource.DataSource,
                    testInformation, testSessionId);

                // Initialize the data source attribute after property injection
                await ObjectInitializer.InitializeAsync(propertyDataSource.DataSource);

                // Get data rows from the initialized data source attribute
                var dataRows = propertyDataSource.DataSource.GetDataRowsAsync(dataGeneratorMetadata);

                await foreach (var factory in dataRows)
                {
                    // Get the first value - properties only support single values
                    var args = await factory();
                    var value = args?.FirstOrDefault();

                    // If the value is an object that might have properties needing injection, handle that
                    if (value != null && value.GetType().IsClass && value.GetType() != typeof(string))
                    {
                        // Find the injection data for this property's type
                        var propertyInjection = injectionData.FirstOrDefault(p => p.PropertyName == propertyDataSource.PropertyName);
                        if (propertyInjection?.NestedPropertyInjections?.Length > 0 && propertyInjection.NestedPropertyValueFactory != null)
                        {
                            // Recursively inject properties into the nested object
                            await InjectPropertiesWithValuesAsync(testContext, value,
                                propertyInjection.NestedPropertyValueFactory(value),
                                propertyInjection.NestedPropertyInjections, 5, 0);
                        }

                        // Initialize the data object
                        await ObjectInitializer.InitializeAsync(value);
                    }

                    propertyValues[propertyDataSource.PropertyName] = value;
                    break; // Only take the first value for properties
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve data source for property '{propertyDataSource.PropertyName}': {ex.Message}", ex);
            }
        }

        // Now inject the resolved values
        await InjectPropertiesWithValuesAsync(testContext, instance, propertyValues, injectionData, 5, 0);
    }

    /// <summary>
    /// Internal method to inject already-resolved property values.
    /// Used after data sources have been invoked.
    /// </summary>
    private static async Task InjectPropertiesWithValuesAsync(
        TestContext testContext,
        object? instance,
        Dictionary<string, object?> propertyValues,
        PropertyInjectionData[] injectionData,
        int maxRecursionDepth = 5,
        int currentDepth = 0)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        if (currentDepth >= maxRecursionDepth)
        {
            return; // Prevent infinite recursion
        }

        // Use PropertyInjectionData if available (preferred path)
        if (injectionData is { Length: > 0 })
        {
            foreach (var injection in injectionData)
            {
                if (!propertyValues.TryGetValue(injection.PropertyName, out var value))
                {
                    continue;
                }

                var onTestStart = testContext.Events.OnTestStart ??= new AsyncEvent<TestContext>();
                onTestStart.InsertAtFront(async (o, context) =>
                {
                    await ObjectInitializer.InitializeAsync(value);
                });

                testContext.Events.OnDispose += async (o, context) =>
                {
                    await DataSourceReferenceTrackerProvider.ReleaseDataSourceObject(value);
                };

                // Track the injected value
                var trackedValue = DataSourceReferenceTrackerProvider.TrackDataSourceObject(value);

                injection.Setter(instance, trackedValue);

                // Recursively inject properties using pre-compiled nested metadata (AOT-compatible)
                if (trackedValue != null &&
                    injection.NestedPropertyInjections.Length > 0 &&
                    injection.NestedPropertyValueFactory != null)
                {
                    try
                    {
                        // Extract nested property values using pre-compiled factory
                        var nestedPropertyValues = injection.NestedPropertyValueFactory(trackedValue);

                        await InjectPropertiesWithValuesAsync(
                            testContext,
                            trackedValue,
                            nestedPropertyValues,
                            injection.NestedPropertyInjections,
                            maxRecursionDepth,
                            currentDepth + 1);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to recursively inject properties on '{injection.PropertyName}': {ex.Message}", ex);
                    }
                }
            }
        }
        else
        {
            // Fallback to reflection if no injection data (for compatibility)
            await InjectPropertiesViaReflectionAsync(testContext, instance, propertyValues, maxRecursionDepth, currentDepth);
        }
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
        {
            return null;
        }

        // Try compiler-generated backing field pattern
        var backingFieldName = $"<{property.Name}>k__BackingField";
        var field = GetField(property.DeclaringType, backingFieldName, BackingFieldFlags);

        if (field != null)
        {
            return field;
        }

        // Try underscore prefix pattern
        var underscoreName = "_" + char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
        field = GetField(property.DeclaringType, underscoreName, BackingFieldFlags);

        if (field != null && field.FieldType == property.PropertyType)
        {
            return field;
        }

        // Try exact name match
        field = GetField(property.DeclaringType, property.Name, BackingFieldFlags);

        if (field != null && field.FieldType == property.PropertyType)
        {
            return field;
        }

        return null;
    }

    /// <summary>
    /// Fallback method to inject properties via reflection when no PropertyInjectionData is available.
    /// Supports recursive injection and data source tracking.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Property injection requires reflection access to object type")]
    private static async Task InjectPropertiesViaReflectionAsync(
        TestContext testContext,
        object instance,
        Dictionary<string, object?> propertyValues,
        int maxRecursionDepth = 5,
        int currentDepth = 0)
    {
        if (currentDepth >= maxRecursionDepth)
        {
            return; // Prevent infinite recursion
        }

        var type = instance.GetType();

        foreach (var kvp in propertyValues)
        {
            var property = GetProperty(type, kvp.Key);
            if (property == null)
            {
                continue;
            }

            try
            {
                // Track the injected value
                var trackedValue = DataSourceReferenceTrackerProvider.TrackDataSourceObject(kvp.Value);

                var setter = CreatePropertySetter(property);
                setter(instance, trackedValue);

                // Recursively inject properties on the injected object
                if (trackedValue != null && ShouldRecurse(trackedValue))
                {
                    var nestedInjectionData = DiscoverInjectableProperties(trackedValue.GetType());
                    if (nestedInjectionData.Length > 0)
                    {
                        // For recursive injection, we need to extract property values from the object itself
                        var nestedPropertyValues = new Dictionary<string, object?>();

                        await InjectPropertiesWithValuesAsync(
                            testContext,
                            trackedValue,
                            nestedPropertyValues,
                            nestedInjectionData,
                            maxRecursionDepth,
                            currentDepth + 1);
                    }
                }
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

    /// <summary>
    /// Determines if an object should be recursively processed for property injection.
    /// Excludes primitive types, strings, and other basic types to prevent infinite recursion.
    /// </summary>
    private static bool ShouldRecurse(object obj)
    {
        if (obj == null)
        {
            return false;
        }

        var type = obj.GetType();

        // Don't recurse on primitive types, strings, enums, or value types
        if (type.IsPrimitive || type == typeof(string) || type.IsEnum || type.IsValueType)
        {
            return false;
        }

        // Don't recurse on collections to avoid complexity
        if (type.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

        // Don't recurse on common framework types
        if (type.Namespace?.StartsWith("System") == true && type.Assembly == typeof(object).Assembly)
        {
            return false;
        }

        return true;
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

    /// <summary>
    /// Injects properties into a data source attribute instance.
    /// For AOT mode, this uses pre-generated metadata from the registry.
    /// For reflection mode, this discovers properties dynamically.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Property injection with fallback to reflection")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Property injection with fallback to reflection")]
    private static async Task InjectDataSourcePropertiesAsync(
        TestContext testContext,
        object dataSourceInstance,
        MethodMetadata testInformation,
        string testSessionId)
    {
        var type = dataSourceInstance.GetType();

        // Try to get pre-generated metadata from registry (AOT mode)
        var injectionData = DataSourcePropertyInjectionRegistry.GetInjectionData(type);
        var propertyDataSources = DataSourcePropertyInjectionRegistry.GetPropertyDataSources(type);

        // If no pre-generated data, try reflection (non-AOT mode only)
        if ((injectionData == null || propertyDataSources == null) && !IsAotMode())
        {
            var discovered = DiscoverDataSourcePropertiesViaReflection(type);
            if (discovered.properties.Length > 0)
            {
                propertyDataSources = discovered.properties;
                injectionData = discovered.injectionData;
            }
        }

        // Inject properties if we have the necessary metadata
        if (propertyDataSources is { Length: > 0 } &&
            injectionData is { Length: > 0 })
        {
            await InjectPropertiesAsync(testContext, dataSourceInstance,
                propertyDataSources, injectionData, testInformation, testSessionId);
        }
    }

    /// <summary>
    /// Discovers data source properties via reflection (non-AOT mode only).
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection-only fallback")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Reflection-only fallback")]
    private static (PropertyDataSource[] properties, PropertyInjectionData[] injectionData)
        DiscoverDataSourcePropertiesViaReflection([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        var properties = new List<PropertyDataSource>();
        var injectionData = new List<PropertyInjectionData>();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.CanWrite || GetBackingField(property) != null)
            {
                var dataSourceAttr = property.GetCustomAttributes()
                    .FirstOrDefault(attr => attr is IDataSourceAttribute) as IDataSourceAttribute;

                if (dataSourceAttr != null)
                {
                    properties.Add(new PropertyDataSource
                    {
                        PropertyName = property.Name,
                        PropertyType = property.PropertyType,
                        DataSource = dataSourceAttr
                    });

                    injectionData.Add(new PropertyInjectionData
                    {
                        PropertyName = property.Name,
                        PropertyType = property.PropertyType,
                        Setter = CreatePropertySetter(property),
                        ValueFactory = () => throw new InvalidOperationException("Should not be called"),
                        NestedPropertyInjections = Array.Empty<PropertyInjectionData>(),
                        NestedPropertyValueFactory = obj => new Dictionary<string, object?>()
                    });
                }
            }
        }

        return (properties.ToArray(), injectionData.ToArray());
    }

    /// <summary>
    /// Checks if we're running in AOT mode where reflection is limited.
    /// </summary>
    private static bool IsAotMode()
    {
        // Check if any data source registrations exist - if they do, we're in source generation mode
        // In reflection mode, the registry will be empty
        return DataSourcePropertyInjectionRegistry.GetInjectionData(typeof(ArgumentsAttribute)) != null;
    }
}
