using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using TUnit.Core.Enums;
using TUnit.Core.ReferenceTracking;

namespace TUnit.Core;

public static class PropertyInjector
{
    private static readonly BindingFlags BackingFieldFlags =
        BindingFlags.Instance | BindingFlags.NonPublic;

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

        var dataGeneratorMetadata = new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(TestBuilderContext.Current ?? new TestBuilderContext()),
            MembersToGenerate = Array.Empty<MemberMetadata>(),
            TestInformation = testInformation,
            Type = DataGeneratorType.Property,
            TestSessionId = testSessionId,
            TestClassInstance = instance,
            ClassInstanceArguments = testContext.TestDetails.TestClassArguments
        };

        var propertyValues = new Dictionary<string, object?>();

        foreach (var propertyDataSource in propertyDataSources)
        {
            try
            {
                await InjectDataSourcePropertiesAsync(testContext, propertyDataSource.DataSource,
                    testInformation, testSessionId);

                await ObjectInitializer.InitializeAsync(propertyDataSource.DataSource);

                var dataRows = propertyDataSource.DataSource.GetDataRowsAsync(dataGeneratorMetadata);

                await foreach (var factory in dataRows)
                {
                    var args = await factory();
                    var value = args?.FirstOrDefault();

                    if (value != null && value.GetType().IsClass && value.GetType() != typeof(string))
                    {
                        var propertyInjection = injectionData.FirstOrDefault(p => p.PropertyName == propertyDataSource.PropertyName);
                        if (propertyInjection?.NestedPropertyInjections?.Length > 0 && propertyInjection.NestedPropertyValueFactory != null)
                        {
                            await InjectPropertiesWithValuesAsync(testContext, value,
                                propertyInjection.NestedPropertyValueFactory(value),
                                propertyInjection.NestedPropertyInjections, 5, 0);
                        }

                        await ObjectInitializer.InitializeAsync(value);
                    }

                    propertyValues[propertyDataSource.PropertyName] = value;
                    break;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve data source for property '{propertyDataSource.PropertyName}': {ex.Message}", ex);
            }
        }

        await InjectPropertiesWithValuesAsync(testContext, instance, propertyValues, injectionData, 5, 0);
    }

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
            return;
        }

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

                var trackedValue = DataSourceReferenceTrackerProvider.TrackDataSourceObject(value);

                injection.Setter(instance, trackedValue);

                if (trackedValue != null &&
                    injection.NestedPropertyInjections.Length > 0 &&
                    injection.NestedPropertyValueFactory != null)
                {
                    try
                    {
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
            await InjectPropertiesViaReflectionAsync(testContext, instance, propertyValues, maxRecursionDepth, currentDepth);
        }
    }

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

    public static Action<object, object?> CreatePropertySetter(PropertyInfo property)
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

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Property backing field access requires reflection")]
    private static FieldInfo? GetBackingField(PropertyInfo property)
    {
        if (property.DeclaringType == null)
        {
            return null;
        }

        var backingFieldName = $"<{property.Name}>k__BackingField";
        var field = GetField(property.DeclaringType, backingFieldName, BackingFieldFlags);

        if (field != null)
        {
            return field;
        }

        var underscoreName = "_" + char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
        field = GetField(property.DeclaringType, underscoreName, BackingFieldFlags);

        if (field != null && field.FieldType == property.PropertyType)
        {
            return field;
        }

        field = GetField(property.DeclaringType, property.Name, BackingFieldFlags);

        if (field != null && field.FieldType == property.PropertyType)
        {
            return field;
        }

        return null;
    }

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
            return;
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
                var trackedValue = DataSourceReferenceTrackerProvider.TrackDataSourceObject(kvp.Value);

                var setter = CreatePropertySetter(property);
                setter(instance, trackedValue);

                if (trackedValue != null && ShouldRecurse(trackedValue))
                {
                    var nestedInjectionData = DiscoverInjectableProperties(trackedValue.GetType());
                    if (nestedInjectionData.Length > 0)
                    {
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
                throw new InvalidOperationException($"Failed to inject property '{kvp.Key}' on type '{type.Name}': {ex.Message}", ex);
            }
        }
    }

    public static PropertyInjectionData[] DiscoverInjectableProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        var injectableProperties = new List<PropertyInjectionData>();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
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

    private static bool ShouldRecurse(object obj)
    {
        if (obj == null)
        {
            return false;
        }

        var type = obj.GetType();

        if (type.IsPrimitive || type == typeof(string) || type.IsEnum || type.IsValueType)
        {
            return false;
        }

        if (type.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

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

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Property injection with fallback to reflection")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Property injection with fallback to reflection")]
    private static async Task InjectDataSourcePropertiesAsync(
        TestContext testContext,
        object dataSourceInstance,
        MethodMetadata testInformation,
        string testSessionId)
    {
        var type = dataSourceInstance.GetType();

        var injectionData = DataSourcePropertyInjectionRegistry.GetInjectionData(type);
        var propertyDataSources = DataSourcePropertyInjectionRegistry.GetPropertyDataSources(type);

        if ((injectionData == null || propertyDataSources == null) && !IsAotMode())
        {
            var discovered = DiscoverDataSourcePropertiesViaReflection(type);
            if (discovered.properties.Length > 0)
            {
                propertyDataSources = discovered.properties;
                injectionData = discovered.injectionData;
            }
        }

        if (propertyDataSources is { Length: > 0 } &&
            injectionData is { Length: > 0 })
        {
            await InjectPropertiesAsync(testContext, dataSourceInstance,
                propertyDataSources, injectionData, testInformation, testSessionId);
        }
    }

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

    private static bool IsAotMode()
    {
        return DataSourcePropertyInjectionRegistry.GetInjectionData(typeof(ArgumentsAttribute)) != null;
    }
}
