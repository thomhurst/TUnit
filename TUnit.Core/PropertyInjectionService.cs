using TUnit.Core;
using TUnit.Core.Tracking;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Data;
using TUnit.Core.Interfaces.SourceGenerator;
using TUnit.Core.Enums;
using TUnit.Core.Services;
using TUnit.Core.Helpers;
using System.Reflection;

namespace TUnit.Core;

public sealed class PropertyInjectionService
{
    private static readonly GetOnlyDictionary<object, Task> _injectionTasks = new();

    /// <summary>
    /// Injects properties with data sources into argument objects just before test execution.
    /// This ensures properties are only initialized when the test is about to run.
    /// Arguments are processed in parallel for better performance.
    /// </summary>
    public static async Task InjectPropertiesIntoArgumentsAsync(object?[] arguments, Dictionary<string, object?> objectBag, MethodMetadata methodMetadata, TestContextEvents events)
    {
        if (arguments.Length == 0)
        {
            return;
        }

        // Process arguments in parallel
        var argumentTasks = arguments
            .Where(argument => argument != null && ShouldInjectProperties(argument))
            .Select(argument => InjectPropertiesIntoObjectAsync(argument!, objectBag, methodMetadata, events))
            .ToArray();

        await Task.WhenAll(argumentTasks);
    }

    /// <summary>
    /// Determines if an object should have properties injected based on its type and whether it has nested data sources.
    /// </summary>
    private static bool ShouldInjectProperties(object? obj)
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

        if (type.Assembly == typeof(object).Assembly)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Recursively injects properties with data sources into a single object.
    /// Uses source generation mode when available, falls back to reflection mode.
    /// After injection, handles tracking, initialization, and recursive injection.
    /// </summary>
    public static async Task InjectPropertiesIntoObjectAsync(object instance, Dictionary<string, object?>? objectBag, MethodMetadata? methodMetadata, TestContextEvents? events)
    {
        if (instance == null)
        {
            return;
        }

        // If we don't have the required context, try to get it from the current test context
        objectBag ??= TestContext.Current?.ObjectBag ?? new Dictionary<string, object?>();
        methodMetadata ??= TestContext.Current?.TestDetails?.MethodMetadata;
        events ??= TestContext.Current?.Events;

        // If we still don't have events after trying to get from context, create a default instance
        events ??= new TestContextEvents();

        try
        {
            await _injectionTasks.GetOrAdd(instance, async _ =>
            {
                if (SourceRegistrar.IsEnabled)
                {
                    await InjectPropertiesUsingSourceGenerationAsync(instance, objectBag, methodMetadata, events);
                }
                else
                {
                    await InjectPropertiesUsingReflectionAsync(instance, objectBag, methodMetadata, events);
                }
                
                // Initialize the object AFTER all its properties have been injected and initialized
                await ObjectInitializer.InitializeAsync(instance);
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to inject properties for type '{instance.GetType().Name}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Injects properties using source-generated metadata (AOT-safe mode).
    /// Properties at the same level are initialized in parallel for better performance.
    /// </summary>
    private static async Task InjectPropertiesUsingSourceGenerationAsync(object instance, Dictionary<string, object?> objectBag, MethodMetadata? methodMetadata, TestContextEvents events)
    {
        var type = instance.GetType();
        var propertySource = PropertySourceRegistry.GetSource(type);

        if (propertySource?.ShouldInitialize == true)
        {
            var propertyMetadata = propertySource.GetPropertyMetadata();

            // Process all properties at the same level in parallel
            var propertyTasks = propertyMetadata.Select(metadata => 
                ProcessPropertyMetadata(instance, metadata, objectBag, methodMetadata, events, TestContext.Current)
            ).ToArray();

            await Task.WhenAll(propertyTasks);
        }
    }

    /// <summary>
    /// Injects properties using runtime reflection (full feature mode).
    /// Properties at the same level are initialized in parallel for better performance.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
    private static async Task InjectPropertiesUsingReflectionAsync(object instance, Dictionary<string, object?> objectBag, MethodMetadata? methodMetadata, TestContextEvents events)
    {
        var type = instance.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(p => p.CanWrite || p.SetMethod?.IsPublic == false);  // Include init-only properties

        // Collect all property-datasource pairs
        var propertyDataSourcePairs = new List<(PropertyInfo property, IDataSourceAttribute dataSource)>();
        
        foreach (var property in properties)
        {
            foreach (var attr in property.GetCustomAttributes())
            {
                if (attr is IDataSourceAttribute dataSourceAttr)
                {
                    propertyDataSourcePairs.Add((property, dataSourceAttr));
                }
            }
        }

        // Process all properties in parallel
        var propertyTasks = propertyDataSourcePairs.Select(pair => 
            ProcessReflectionPropertyDataSource(instance, pair.property, pair.dataSource, objectBag, methodMetadata, events, TestContext.Current)
        ).ToArray();

        await Task.WhenAll(propertyTasks);
    }

    /// <summary>
    /// Processes property injection using metadata: creates data source, gets values, and injects them.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.")]
    private static async Task ProcessPropertyMetadata(object instance, PropertyInjectionMetadata metadata, Dictionary<string, object?> objectBag, MethodMetadata? methodMetadata,
        TestContextEvents events, TestContext? testContext = null)
    {
        var dataSource = metadata.CreateDataSource();
        var propertyMetadata = new PropertyMetadata
        {
            IsStatic = false,
            Name = metadata.PropertyName,
            ClassMetadata = GetClassMetadataForType(metadata.ContainingType),
            Type = metadata.PropertyType,
            ReflectionInfo = GetPropertyInfo(metadata.ContainingType, metadata.PropertyName),
            Getter = parent => GetPropertyInfo(metadata.ContainingType, metadata.PropertyName).GetValue(parent!)!,
            ContainingTypeMetadata = GetClassMetadataForType(metadata.ContainingType)
        };

        // Use centralized factory
        var dataGeneratorMetadata = DataGeneratorMetadataCreator.CreateForPropertyInjection(
            propertyMetadata,
            methodMetadata,
            dataSource,
            testContext,
            testContext?.TestDetails.ClassInstance,
            events,
            objectBag);

        var dataRows = dataSource.GetDataRowsAsync(dataGeneratorMetadata);

        await foreach (var factory in dataRows)
        {
            var args = await factory();
            var value = args?.FirstOrDefault();

            // Resolve any Func<T> wrappers
            value = await ResolveTestDataValueAsync(metadata.PropertyType, value);

            if (value != null)
            {
                await ProcessInjectedPropertyValue(instance, value, metadata.SetProperty, objectBag, methodMetadata, events);
                break; // Only use first value
            }
        }
    }

    /// <summary>
    /// Processes a property data source using reflection mode.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
    private static async Task ProcessReflectionPropertyDataSource(object instance, PropertyInfo property, IDataSourceAttribute dataSource, Dictionary<string, object?> objectBag, MethodMetadata? methodMetadata, TestContextEvents events, TestContext? testContext = null)
    {
        // Use centralized factory for reflection mode
        var dataGeneratorMetadata = DataGeneratorMetadataCreator.CreateForPropertyInjection(
            property,
            property.DeclaringType!,
            methodMetadata,
            dataSource,
            testContext,
            instance,
            events,
            objectBag);

        var dataRows = dataSource.GetDataRowsAsync(dataGeneratorMetadata);

        await foreach (var factory in dataRows)
        {
            var args = await factory();
            var value = args?.FirstOrDefault();

            // Resolve any Func<T> wrappers
            value = await ResolveTestDataValueAsync(property.PropertyType, value);

            if (value != null)
            {
                var setter = CreatePropertySetter(property);
                await ProcessInjectedPropertyValue(instance, value, setter, objectBag, methodMetadata, events);
                break; // Only use first value
            }
        }
    }

    /// <summary>
    /// Processes a single injected property value: tracks it, initializes it, sets it on the instance, and handles cleanup.
    /// </summary>
    private static async Task ProcessInjectedPropertyValue(object instance, object? propertyValue, Action<object, object?> setProperty, Dictionary<string, object?> objectBag, MethodMetadata? methodMetadata, TestContextEvents events)
    {
        if (propertyValue == null)
        {
            return;
        }

        ObjectTracker.TrackObject(events, propertyValue);

        // First, recursively inject and initialize all descendants of this property value
        if (ShouldInjectProperties(propertyValue))
        {
            // This will recursively inject properties and initialize the object
            await InjectPropertiesIntoObjectAsync(propertyValue, objectBag, methodMetadata, events);
        }
        else
        {
            // For objects that don't need property injection, still initialize them
            await ObjectInitializer.InitializeAsync(propertyValue);
        }
        
        // Finally, set the fully initialized property on the parent
        setProperty(instance, propertyValue);
    }

    /// <summary>
    /// Resolves Func<T> values by invoking them without using reflection (AOT-safe).
    /// </summary>
    private static ValueTask<object?> ResolveTestDataValueAsync(Type type, object? value)
    {
        if (value == null)
        {
            return new ValueTask<object?>(result: null);
        }

        if (value is Delegate del)
        {
            // Use DynamicInvoke which is AOT-safe for parameterless delegates
            var result = del.DynamicInvoke();
            return new ValueTask<object?>(result);
        }

        return new ValueTask<object?>(value);
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

    // =====================================
    // LEGACY COMPATIBILITY API
    // =====================================
    // These methods provide compatibility with the old PropertyInjector API
    // while using the unified PropertySourceRegistry internally

    /// <summary>
    /// Legacy compatibility: Discovers injectable properties for a type
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Legacy compatibility method")]
    public static PropertyInjectionData[] DiscoverInjectableProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] Type type)
    {
        return PropertySourceRegistry.DiscoverInjectableProperties(type);
    }

    /// <summary>
    /// Legacy compatibility: Injects properties using array-based data structures
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Legacy compatibility method")]
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

        // Use the modern PropertyInjectionService for all injection work
        // This ensures consistent behavior and proper recursive injection
        var objectBag = new Dictionary<string, object?>();

        // Process each property data source
        foreach (var propertyDataSource in propertyDataSources)
        {
            try
            {
                // First inject properties into the data source itself (if it has any)
                if (ShouldInjectProperties(propertyDataSource.DataSource))
                {
                    await InjectPropertiesIntoObjectAsync(propertyDataSource.DataSource, objectBag, testInformation, testContext.Events);
                }

                // Initialize the data source
                await ObjectInitializer.InitializeAsync(propertyDataSource.DataSource);

                // Get the property injection info
                var propertyInjection = injectionData.FirstOrDefault(p => p.PropertyName == propertyDataSource.PropertyName);
                if (propertyInjection == null)
                {
                    continue;
                }

                // Create property metadata for the data generator
                var propertyMetadata = new PropertyMetadata
                {
                    IsStatic = false,
                    Name = propertyDataSource.PropertyName,
                    ClassMetadata = GetClassMetadataForType(testInformation.Type),
                    Type = propertyInjection.PropertyType,
                    ReflectionInfo = GetPropertyInfo(testInformation.Type, propertyDataSource.PropertyName),
                    Getter = parent => GetPropertyInfo(testInformation.Type, propertyDataSource.PropertyName).GetValue(parent!)!,
                    ContainingTypeMetadata = GetClassMetadataForType(testInformation.Type)
                };

                // Create data generator metadata
                var dataGeneratorMetadata = DataGeneratorMetadataCreator.CreateForPropertyInjection(
                    propertyMetadata,
                    testInformation,
                    propertyDataSource.DataSource,
                    testContext,
                    instance);

                // Get data rows and process the first one
                var dataRows = propertyDataSource.DataSource.GetDataRowsAsync(dataGeneratorMetadata);
                await foreach (var factory in dataRows)
                {
                    var args = await factory();
                    object? value;

                    // Handle tuple properties - need to create tuple from multiple arguments
                    if (TupleFactory.IsTupleType(propertyInjection.PropertyType))
                    {
                        if (args is { Length: > 1 })
                        {
                            // Multiple arguments - create tuple from them
                            value = TupleFactory.CreateTuple(propertyInjection.PropertyType, args);
                        }
                        else if (args is [not null] && TupleFactory.IsTupleType(args[0]!.GetType()))
                        {
                            // Single tuple argument - check if it needs type conversion
                            var tupleValue = args[0]!;
                            var tupleType = tupleValue!.GetType();

                            if (tupleType != propertyInjection.PropertyType)
                            {
                                // Tuple types don't match - unwrap and recreate with correct types
                                var elements = DataSourceHelpers.UnwrapTupleAot(tupleValue);
                                value = TupleFactory.CreateTuple(propertyInjection.PropertyType, elements);
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

                    // Resolve the value (handle Func<T>, Task<T>, etc.)
                    value = await ResolveTestDataValueAsync(propertyInjection.PropertyType, value);

                    if (value != null)
                    {
                        // Use the modern service for recursive injection and initialization
                        await ProcessInjectedPropertyValue(instance, value, propertyInjection.Setter, objectBag, testInformation, testContext.Events);
                        break; // Only use first value
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve data source for property '{propertyDataSource.PropertyName}': {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Legacy compatibility: Creates PropertyInjectionData from PropertyInfo
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
    /// Legacy compatibility: Creates property setter
    /// </summary>
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

    /// <summary>
    /// Legacy compatibility: Gets backing field for property
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Legacy compatibility method")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Legacy compatibility method")]
    private static FieldInfo? GetBackingField(PropertyInfo property)
    {
        var declaringType = property.DeclaringType;
        if (declaringType == null)
        {
            return null;
        }

        var backingFieldFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

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
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Legacy compatibility method")]
    private static FieldInfo? GetFieldSafe([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] Type type, string name, BindingFlags bindingFlags)
    {
        return type.GetField(name, bindingFlags);
    }

    /// <summary>
    /// Legacy compatibility: Checks if method is init-only
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Legacy compatibility method")]
    private static bool IsInitOnlyMethod(MethodInfo setMethod)
    {
        var methodType = setMethod.GetType();
        var isInitOnlyProperty = methodType.GetProperty("IsInitOnly");
        return isInitOnlyProperty != null && (bool)isInitOnlyProperty.GetValue(setMethod)!;
    }
}
