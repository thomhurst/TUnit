using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using TUnit.Core.Enums;
using TUnit.Core.Helpers;
using TUnit.Core.ReferenceTracking;
using TUnit.Core.Tracking;

namespace TUnit.Core;

public static class PropertyInjector
{
    private static readonly BindingFlags BackingFieldFlags =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

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

                var propertyInjection = injectionData.FirstOrDefault(p => p.PropertyName == propertyDataSource.PropertyName);

                var containingType = testInformation.Type;
                var propertyType = propertyInjection?.PropertyType ?? typeof(object);

                // Create property metadata
                PropertyMetadata? propertyMetadata = null;
                if (propertyInjection != null)
                {
                    propertyMetadata = new PropertyMetadata
                    {
                        IsStatic = false,
                        Name = propertyDataSource.PropertyName,
                        ClassMetadata = GetClassMetadataForType(containingType),
                        Type = propertyType,
                        ReflectionInfo = GetPropertyInfo(containingType, propertyDataSource.PropertyName),
                        Getter = parent => GetPropertyInfo(containingType, propertyDataSource.PropertyName).GetValue(parent!)!,
                        ContainingTypeMetadata = GetClassMetadataForType(containingType)
                    };
                }

                var dataGeneratorMetadata = propertyMetadata != null
                    ? DataGeneratorMetadataCreator.CreateForPropertyInjection(
                        propertyMetadata,
                        testInformation,
                        propertyDataSource.DataSource,
                        testContext,
                        instance)
                    : new DataGeneratorMetadata
                    {
                        TestBuilderContext = new TestBuilderContextAccessor(TestBuilderContext.Current ?? TestBuilderContext.FromTestContext(testContext, propertyDataSource.DataSource)),
                        MembersToGenerate = [],
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

                    var currentPropertyInjection = injectionData.FirstOrDefault(p => p.PropertyName == propertyDataSource.PropertyName);
                    object? value;

                    if (currentPropertyInjection != null && TupleFactory.IsTupleType(currentPropertyInjection.PropertyType))
                    {
                        if (args is { Length: > 1 })
                        {
                            // Multiple arguments - create tuple from them
                            value = TupleFactory.CreateTuple(currentPropertyInjection.PropertyType, args);
                        }
                        else if (args is
                                 [
                                     not null
                                 ] && TupleFactory.IsTupleType(args[0]!.GetType()))
                        {
                            // Single tuple argument - check if it needs type conversion
                            var tupleValue = args[0]!;
                            var tupleType = tupleValue!.GetType();

                            if (tupleType != currentPropertyInjection.PropertyType)
                            {
                                // Tuple types don't match - unwrap and recreate with correct types
                                var elements = DataSourceHelpers.UnwrapTupleAot(tupleValue);
                                value = TupleFactory.CreateTuple(currentPropertyInjection.PropertyType, elements);
                            }
                            else
                            {
                                // Types match - use directly
                                value = tupleValue;
                            }
                        }
                        else
                        {
                            // Single non-tuple argument or null
                            value = args?.FirstOrDefault();
                        }
                    }
                    else
                    {
                        value = args?.FirstOrDefault();
                    }

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

        // First inject all properties
        var allInjectedObjects = new Dictionary<object, int>(); // object -> depth
        await InjectPropertiesWithValuesAsync(testContext, instance, propertyValues, injectionData, 5, 0, allInjectedObjects);

        // Then schedule initialization of all injected objects in the correct order (deepest first)
        if (allInjectedObjects.Count > 0)
        {
            var onTestStart = testContext.Events.OnTestStart ??= new AsyncEvent<TestContext>();
            var objectsByDepth = allInjectedObjects
                .OrderByDescending(kvp => kvp.Value) // Sort by depth (deepest first)
                .Select(kvp => kvp.Key)
                .Where(obj => obj is Interfaces.IAsyncInitializer)
                .ToList();

            if (objectsByDepth.Count > 0)
            {
                onTestStart.InsertAtFront(async (o, context) =>
                {
                    foreach (var obj in objectsByDepth)
                    {
                        await ObjectInitializer.InitializeAsync(obj);
                    }
                });
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Reflection mode requires dynamic property access")]
    private static async Task InjectPropertiesWithValuesAsync(
        TestContext testContext,
        object? instance,
        Dictionary<string, object?> propertyValues,
        PropertyInjectionData[] injectionData,
        int maxRecursionDepth = 5,
        int currentDepth = 0,
        Dictionary<object, int>? allInjectedObjects = null)
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

                ObjectTracker.TrackObject(testContext.Events, value);

                injection.Setter(instance, value);

                // Track this object for initialization ordering
                if (allInjectedObjects != null && value != null)
                {
                    allInjectedObjects.TryAdd(value, currentDepth);
                }

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
                            currentDepth + 1,
                            allInjectedObjects);
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
            await InjectPropertiesViaReflectionAsync(testContext, instance, propertyValues, maxRecursionDepth, currentDepth, allInjectedObjects);
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

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Reflection mode only - backing field access requires reflection")]
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

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Reflection mode only - property injection requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection mode only - property injection requires dynamic access")]
    private static async Task InjectPropertiesViaReflectionAsync(
        TestContext testContext,
        object instance,
        Dictionary<string, object?> propertyValues,
        int maxRecursionDepth = 5,
        int currentDepth = 0,
        Dictionary<object, int>? allInjectedObjects = null)
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
                ObjectTracker.TrackObject(testContext.Events, propertyValue);

                var setter = CreatePropertySetter(property);
                setter(instance, propertyValue);

                // Track this object for initialization ordering
                if (allInjectedObjects != null && propertyValue != null)
                {
                    allInjectedObjects.TryAdd(propertyValue, currentDepth);
                }

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
                            currentDepth + 1,
                            allInjectedObjects);
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

    private static FieldInfo? GetField([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] Type type, string name, BindingFlags bindingFlags)
    {
        return type.GetField(name, bindingFlags);
    }

    private static PropertyInfo? GetProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, string name)
    {
        return type.GetProperty(name);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection mode only - IsInitOnly check requires reflection")]
    private static bool IsInitOnlyMethod(MethodInfo setMethod)
    {
        var methodType = setMethod.GetType();
        var isInitOnlyProperty = methodType.GetProperty("IsInitOnly");
        return isInitOnlyProperty != null && (bool)isInitOnlyProperty.GetValue(setMethod)!;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Source generation mode uses pre-generated injection data")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Source generation mode uses pre-generated injection data")]
    private static async Task InjectDataSourcePropertiesAsync(
        TestContext testContext,
        object dataSourceInstance,
        MethodMetadata testInformation,
        string testSessionId)
    {
        var type = dataSourceInstance.GetType();

        var injectionData = DataSourcePropertyInjectionRegistry.GetInjectionData(type);
        var propertyDataSources = DataSourcePropertyInjectionRegistry.GetPropertyDataSources(type);

        if (injectionData == null || propertyDataSources == null)
        {
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
                if (property.GetCustomAttributes()
                        .FirstOrDefault(attr => attr is IDataSourceAttribute) is IDataSourceAttribute dataSourceAttr)
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

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Func<>))
        {
            var returnType = type.GetGenericArguments()[0];

            var invokeMethod = type.GetMethod("Invoke");
            var result = invokeMethod!.Invoke(value, null);

            if (result is Task task)
            {
                await task.ConfigureAwait(false);

                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var resultProperty = task.GetType().GetProperty("Result");
                    return resultProperty?.GetValue(task);
                }

                return null;
            }

            return result;
        }

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
        return ClassMetadata.GetOrAdd(type.FullName ?? type.Name, () =>
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            var constructor = constructors.FirstOrDefault();

            var constructorParameters = constructor?.GetParameters().Select((p, i) => new ParameterMetadata(p.ParameterType)
            {
                Name = p.Name ?? $"param{i}",
                TypeReference = new TypeReference { AssemblyQualifiedName = p.ParameterType.AssemblyQualifiedName },
                ReflectionInfo = p
            }).ToArray() ?? Array.Empty<ParameterMetadata>();

            return new ClassMetadata
            {
                Type = type,
                TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName ?? type.FullName ?? type.Name),
                Name = type.Name,
                Namespace = type.Namespace ?? string.Empty,
                Assembly = AssemblyMetadata.GetOrAdd(type.Assembly.GetName().Name ?? type.Assembly.GetName().FullName ?? "Unknown", () => new AssemblyMetadata
                {
                    Name = type.Assembly.GetName().Name ?? type.Assembly.GetName().FullName ?? "Unknown"
                }),
                Properties = [],
                Parameters = constructorParameters,
                Parent = type.DeclaringType != null ? GetClassMetadataForType(type.DeclaringType) : null
            };
        });
    }


}
