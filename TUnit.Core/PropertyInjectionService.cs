using TUnit.Core;
using TUnit.Core.Tracking;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Data;
using TUnit.Core.Interfaces.SourceGenerator;
using TUnit.Core.Enums;
using TUnit.Core.Services;
using System.Reflection;

namespace TUnit.Core;

public sealed class PropertyInjectionService
{
    private static readonly GetOnlyDictionary<object, Task> _injectionTasks = new();

    /// <summary>
    /// Injects properties with data sources into argument objects just before test execution.
    /// This ensures properties are only initialized when the test is about to run.
    /// </summary>
    public static async Task InjectPropertiesIntoArgumentsAsync(object?[] arguments, Dictionary<string, object?> objectBag, MethodMetadata methodMetadata, TestContextEvents events)
    {
        if (arguments.Length == 0)
        {
            return;
        }

        foreach (var argument in arguments)
        {
            if (argument != null && ShouldInjectProperties(argument))
            {
                await InjectPropertiesIntoObjectAsync(argument, objectBag, methodMetadata, events);
            }
        }
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
    public static async Task InjectPropertiesIntoObjectAsync(object instance, Dictionary<string, object?> objectBag, MethodMetadata methodMetadata, TestContextEvents events)
    {
        try
        {
            await _injectionTasks.GetOrAdd(instance, async _ =>
            {
                var executionMode = ModeDetector.Mode;

                switch (executionMode)
                {
                    case TestExecutionMode.SourceGeneration:
                        await InjectPropertiesUsingSourceGenerationAsync(instance, objectBag, methodMetadata, events);
                        break;
                    case TestExecutionMode.Reflection:
                        await InjectPropertiesUsingReflectionAsync(instance, objectBag, methodMetadata, events);
                        break;
                    default:
                        throw new NotSupportedException($"Test execution mode '{executionMode}' is not supported for property injection");
                }
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to inject properties for type '{instance?.GetType().Name}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Injects properties using source-generated metadata (AOT-safe mode).
    /// </summary>
    private static async Task InjectPropertiesUsingSourceGenerationAsync(object instance, Dictionary<string, object?> objectBag, MethodMetadata methodMetadata, TestContextEvents events)
    {
        var type = instance.GetType();
        var propertySource = PropertySourceRegistry.GetSource(type);

        if (propertySource?.ShouldInitialize == true)
        {
            var propertyMetadata = propertySource.GetPropertyMetadata();

            foreach (var metadata in propertyMetadata)
            {
                await ProcessPropertyMetadata(instance, metadata, objectBag, methodMetadata, events);
            }
        }
    }

    /// <summary>
    /// Injects properties using runtime reflection (full feature mode).
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
    private static async Task InjectPropertiesUsingReflectionAsync(object instance, Dictionary<string, object?> objectBag, MethodMetadata methodMetadata, TestContextEvents events)
    {
        var type = instance.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(p => p.CanWrite);

        foreach (var property in properties)
        {
            foreach (var attr in property.GetCustomAttributes())
            {
                if (attr is IDataSourceAttribute dataSourceAttr)
                {
                    await ProcessReflectionPropertyDataSource(instance, property, dataSourceAttr, objectBag, methodMetadata, events);
                }
            }
        }
    }

    /// <summary>
    /// Processes property injection using metadata: creates data source, gets values, and injects them.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.")]
    private static async Task ProcessPropertyMetadata(object instance, PropertyInjectionMetadata metadata, Dictionary<string, object?> objectBag, MethodMetadata methodMetadata,
        TestContextEvents events)
    {
        // Create the data source using the generated factory
        var dataSource = metadata.CreateDataSource();

        // Create metadata for data generation
        var dataGeneratorMetadata = new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext
            {
                Events = events,
                TestMetadata = methodMetadata,
                DataSourceAttribute = dataSource,
                ObjectBag = objectBag,
            }),
            MembersToGenerate =
            [
                new PropertyMetadata
                {
                    IsStatic = false,
                    Name = metadata.PropertyName,
                    ClassMetadata = GetClassMetadataForType(metadata.ContainingType),
                    Type = metadata.PropertyType,
                    ReflectionInfo = GetPropertyInfo(metadata.ContainingType, metadata.PropertyName),
                    Getter = parent => GetPropertyInfo(metadata.ContainingType, metadata.PropertyName).GetValue(parent!)!,
                }
            ],
            TestInformation = methodMetadata,
            Type = DataGeneratorType.Property,
            TestSessionId = TestSessionContext.Current!.Id,
            TestClassInstance = TestContext.Current?.TestDetails.ClassInstance,
            ClassInstanceArguments = TestContext.Current?.TestDetails.TestClassArguments
        };

        // Get data from the source
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
    private static async Task ProcessReflectionPropertyDataSource(object instance, PropertyInfo property, IDataSourceAttribute dataSource, Dictionary<string, object?> objectBag, MethodMetadata methodMetadata, TestContextEvents events)
    {
        // Create metadata for data generation
        var dataGeneratorMetadata = new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext
            {
                Events = events,
                TestMetadata = methodMetadata,
                DataSourceAttribute = dataSource,
                ObjectBag = objectBag,
            }),
            MembersToGenerate =
            [
                new PropertyMetadata
                {
                    IsStatic = property.GetMethod?.IsStatic ?? false,
                    Name = property.Name,
                    ClassMetadata = GetClassMetadataForType(property.DeclaringType!),
                    Type = property.PropertyType,
                    ReflectionInfo = property,
                    Getter = parent => property.GetValue(parent),
                }
            ],
            TestInformation = methodMetadata,
            Type = DataGeneratorType.Property,
            TestSessionId = TestSessionContext.Current!.Id,
            TestClassInstance = instance,
            ClassInstanceArguments = TestContext.Current?.TestDetails.TestClassArguments ?? []
        };

        // Get data from the source
        var dataRows = dataSource.GetDataRowsAsync(dataGeneratorMetadata);

        await foreach (var factory in dataRows)
        {
            var args = await factory();
            var value = args?.FirstOrDefault();

            // Resolve any Func<T> wrappers
            value = await ResolveTestDataValueAsync(property.PropertyType, value);

            if (value != null)
            {
                await ProcessInjectedPropertyValue(instance, value, (inst, val) => property.SetValue(inst, val), objectBag, methodMetadata, events);
                break; // Only use first value
            }
        }
    }

    /// <summary>
    /// Processes a single injected property value: tracks it, initializes it, sets it on the instance, and handles cleanup.
    /// </summary>
    private static async Task ProcessInjectedPropertyValue(object instance, object? propertyValue, Action<object, object?> setProperty, Dictionary<string, object?> objectBag, MethodMetadata methodMetadata, TestContextEvents events)
    {
        if (propertyValue == null)
        {
            return;
        }

        UnifiedObjectTracker.TrackObject(events, propertyValue);

        if (ShouldInjectProperties(propertyValue))
        {
            await InjectPropertiesIntoObjectAsync(propertyValue, objectBag, methodMetadata, events);
        }

        await ObjectInitializer.InitializeAsync(propertyValue);

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

        // Check if value is a delegate (includes all Func<T> types)
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
            // Get constructor parameters for the class
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
                Assembly = AssemblyMetadata.GetOrAdd(type.Assembly.GetName().Name ?? type.Assembly.FullName ?? "Unknown", () => new AssemblyMetadata 
                { 
                    Name = type.Assembly.GetName().Name ?? type.Assembly.FullName ?? "Unknown" 
                }),
                Properties = [],
                Parameters = constructorParameters,
                Parent = type.DeclaringType != null ? GetClassMetadataForType(type.DeclaringType) : null
            };
        });
    }
}
