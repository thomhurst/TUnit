using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using TUnit.Core.Enums;
using TUnit.Core.ReferenceTracking;
using TUnit.Core.Tracking;

namespace TUnit.Core;

public static class PropertyInjector
{
    private static readonly BindingFlags BackingFieldFlags =
        BindingFlags.Instance | BindingFlags.NonPublic;

    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.")]
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

        var propertyValues = new Dictionary<string, object?>();

        foreach (var propertyDataSource in propertyDataSources)
        {
            try
            {
                await InjectDataSourcePropertiesAsync(testContext, propertyDataSource.DataSource,
                    testInformation, testSessionId);

                await ObjectInitializer.InitializeAsync(propertyDataSource.DataSource);

                // Create property-specific metadata with the current property's information
                var propertyInjection = injectionData.FirstOrDefault(p => p.PropertyName == propertyDataSource.PropertyName);
                
                // Use metadata-provided types when available (source generation mode), 
                // fallback to runtime discovery (reflection mode)
                var containingType = testInformation.Type; // Use compile-time known type from metadata
                var propertyType = propertyInjection?.PropertyType ?? typeof(object);
                
                var dataGeneratorMetadata = new DataGeneratorMetadata
                {
                    TestBuilderContext = new TestBuilderContextAccessor(TestBuilderContext.Current ?? TestBuilderContext.FromTestContext(testContext, propertyDataSource.DataSource)),
                    MembersToGenerate = propertyInjection != null ? [
                        new PropertyMetadata
                        {
                            IsStatic = false,
                            Name = propertyDataSource.PropertyName,
                            ClassMetadata = GetClassMetadataForType(containingType),
                            Type = propertyType,
                            ReflectionInfo = GetPropertyInfo(containingType, propertyDataSource.PropertyName),
                            Getter = parent => GetPropertyInfo(containingType, propertyDataSource.PropertyName).GetValue(parent!)!,
                        }
                    ] : [],
                    TestInformation = testInformation,
                    Type = DataGeneratorType.Property,
                    TestSessionId = testSessionId,
                    TestClassInstance = instance,
                    ClassInstanceArguments = testContext.TestDetails.TestClassArguments
                };

                var dataRows = propertyDataSource.DataSource.GetDataRowsAsync(dataGeneratorMetadata);

                await foreach (var factory in dataRows)
                {
                    var args = await factory();
                    
                    // For tuple properties, we need to reconstruct the tuple from the unpacked elements
                    var currentPropertyInjection = injectionData.FirstOrDefault(p => p.PropertyName == propertyDataSource.PropertyName);
                    object? value;
                    
                    if (currentPropertyInjection != null && IsTupleType(currentPropertyInjection.PropertyType) && args != null && args.Length > 1)
                    {
                        // The data source has unpacked the tuple, we need to reconstruct it
                        #pragma warning disable IL2072 // Target parameter argument does not satisfy requirements
                        value = CreateTupleFromElements(currentPropertyInjection.PropertyType, args);
                        #pragma warning restore IL2072
                    }
                    else
                    {
                        value = args?.FirstOrDefault();
                    }

                    // Resolve Func<T> values to their actual values
                    value = await ResolveTestDataValueAsync(value);

                    if (value != null && value.GetType().IsClass && value.GetType() != typeof(string))
                    {
                        var nestedInjection = injectionData.FirstOrDefault(p => p.PropertyName == propertyDataSource.PropertyName);
                        if (nestedInjection?.NestedPropertyInjections?.Length > 0 && nestedInjection.NestedPropertyValueFactory != null)
                        {
                            await InjectPropertiesWithValuesAsync(testContext, value,
                                nestedInjection.NestedPropertyValueFactory(value),
                                nestedInjection.NestedPropertyInjections, 5, 0);
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

                UnifiedObjectTracker.TrackObject(testContext.Events, value);

                injection.Setter(instance, value);

                if (value != null &&
                    injection.NestedPropertyInjections.Length > 0 &&
                    injection.NestedPropertyValueFactory != null)
                {
                    try
                    {
                        var nestedPropertyValues = injection.NestedPropertyValueFactory(value);

                        await InjectPropertiesWithValuesAsync(
                            testContext,
                            value,
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
                var propertyValue = kvp.Value;
                UnifiedObjectTracker.TrackObject(testContext.Events, propertyValue);

                var setter = CreatePropertySetter(property);
                setter(instance, propertyValue);

                if (propertyValue != null && ShouldRecurse(propertyValue))
                {
                    var nestedInjectionData = DiscoverInjectableProperties(propertyValue.GetType());
                    if (nestedInjectionData.Length > 0)
                    {
                        var nestedPropertyValues = new Dictionary<string, object?>();

                        await InjectPropertiesWithValuesAsync(
                            testContext,
                            propertyValue,
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

        // In AOT mode, we must rely entirely on source-generated injection data
        // If not available, the properties cannot be injected
        if (injectionData == null || propertyDataSources == null)
        {
            // For AOT compatibility, we cannot fall back to reflection-based discovery
            // All property injection data must be provided by source generators
            return;
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
                        NestedPropertyInjections = [
                        ],
                        NestedPropertyValueFactory = obj => new Dictionary<string, object?>()
                    });
                }
            }
        }

        return (properties.ToArray(), injectionData.ToArray());
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Func resolution requires reflection")]
    private static async Task<object?> ResolveTestDataValueAsync(object? value)
    {
        if (value == null)
        {
            return null;
        }

        var type = value.GetType();

        // Check if it's a Func<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Func<>))
        {
            var returnType = type.GetGenericArguments()[0];

            // Invoke the Func to get the result
            var invokeMethod = type.GetMethod("Invoke");
            var result = invokeMethod!.Invoke(value, null);

            // If the result is a Task, await it
            if (result is Task task)
            {
                await task.ConfigureAwait(false);

                // Get the Result property for Task<T>
                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var resultProperty = task.GetType().GetProperty("Result");
                    return resultProperty?.GetValue(task);
                }

                // For non-generic Task
                return null;
            }

            return result;
        }

        // Check if it's already a Task<T>
        if (value is Task task2)
        {
            await task2.ConfigureAwait(false);

            var taskType = task2.GetType();
            if (taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultProperty = taskType.GetProperty("Result");
                return resultProperty?.GetValue(task2);
            }

            return null;
        }

        // Handle other delegate types
        if (typeof(Delegate).IsAssignableFrom(type))
        {
            var invokeMethod = type.GetMethod("Invoke");
            if (invokeMethod != null && invokeMethod.GetParameters().Length == 0)
            {
                // It's a parameterless delegate, invoke it
                var result = invokeMethod.Invoke(value, null);

                // Recursively resolve in case it returns a Task
                return await ResolveTestDataValueAsync(result).ConfigureAwait(false);
            }
        }

        return value;
    }

    private static bool IsTupleType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition().FullName?.StartsWith("System.ValueTuple") == true;
    }

    [UnconditionalSuppressMessage("AOT", "IL2067:UnrecognizedReflectionPattern", 
        Justification = "Tuple types have public constructors and are safe for AOT")]
    private static object? CreateTupleFromElements(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type tupleType, 
        object?[] elements)
    {
        if (!tupleType.IsGenericType)
        {
            return elements.FirstOrDefault();
        }

        var genericArgs = tupleType.GetGenericArguments();
        if (genericArgs.Length != elements.Length)
        {
            // If lengths don't match, just return the first element
            return elements.FirstOrDefault();
        }

        // Create the tuple using Activator.CreateInstance
        try
        {
            return Activator.CreateInstance(tupleType, elements);
        }
        catch
        {
            // If creation fails, return the first element
            return elements.FirstOrDefault();
        }
    }

    /// <summary>
    /// Gets PropertyInfo in an AOT-safe manner.
    /// </summary>
    private static PropertyInfo GetPropertyInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type containingType, string propertyName)
    {
        return containingType.GetProperty(propertyName)!;
    }

    /// <summary>
    /// Gets or creates ClassMetadata for the specified type.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.")]
    private static ClassMetadata GetClassMetadataForType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        return ClassMetadata.GetOrAdd(type.FullName ?? type.Name, () => new ClassMetadata
        {
            Type = type,
            TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName ?? type.FullName ?? type.Name),
            Name = type.Name,
            Namespace = type.Namespace ?? string.Empty,
            Assembly = AssemblyMetadata.GetOrAdd(type.Assembly.GetName().Name ?? type.Assembly.FullName ?? "Unknown", () => new AssemblyMetadata 
            { 
                Name = type.Assembly.GetName().Name ?? type.Assembly.FullName ?? "Unknown" 
            }),
            Properties = [],
            Parameters = [],
            Parent = type.DeclaringType != null ? GetClassMetadataForType(type.DeclaringType) : null
        });
    }

}
