using TUnit.Core;
using TUnit.Core.Tracking;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Data;
using TUnit.Core.Interfaces.SourceGenerator;
using TUnit.Core.Enums;

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
    /// Recursively injects properties with data sources into a single object using the new static property source system.
    /// The PropertySource includes inherited properties, so we only need to check the concrete type.
    /// After injection, handles tracking, initialization, and recursive injection.
    /// </summary>
    public static async Task InjectPropertiesIntoObjectAsync(object instance, Dictionary<string, object?> objectBag, MethodMetadata methodMetadata, TestContextEvents events)
    {
        try
        {
            await _injectionTasks.GetOrAdd(instance, async _ =>
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
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to inject properties for type '{instance?.GetType().Name}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Processes property injection using metadata: creates data source, gets values, and injects them.
    /// </summary>
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
                ClassInformation = methodMetadata.Class,
                MethodInformation = methodMetadata,
                DataAttribute = dataSource,
                ObjectBag = objectBag,
            }),
            MembersToGenerate =
            [
                new PropertyMetadata
                {
                    IsStatic = false,
                    Name = metadata.PropertyName,
                    ClassMetadata = methodMetadata.Class,
                    Type = metadata.PropertyType,
                    ReflectionInfo = metadata.ContainingType.GetProperty(metadata.PropertyName)!,
                    Getter = parent => metadata.ContainingType.GetProperty(metadata.PropertyName)!.GetValue(parent!)!,
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
            value = await ResolveTestDataValueAsync(value);

            if (value != null)
            {
                await ProcessInjectedPropertyValue(instance, metadata.PropertyName, value, metadata.SetProperty, objectBag, methodMetadata, events);
                break; // Only use first value
            }
        }
    }

    /// <summary>
    /// Processes a single injected property value: tracks it, initializes it, sets it on the instance, and handles cleanup.
    /// </summary>
    private static async Task ProcessInjectedPropertyValue(object instance, string propertyName, object? propertyValue, Action<object, object?> setProperty, Dictionary<string, object?> objectBag, MethodMetadata methodMetadata, TestContextEvents events)
    {
        if (propertyValue == null)
        {
            return;
        }

        var trackedValue = ObjectTrackerProvider.Track(propertyValue);

        if (trackedValue != null && ShouldInjectProperties(trackedValue))
        {
            await InjectPropertiesIntoObjectAsync(trackedValue, objectBag, methodMetadata, events);
        }

        await ObjectInitializer.InitializeAsync(trackedValue);

        setProperty(instance, trackedValue);

        events.OnDispose += async (o, context) =>
        {
            await ObjectTrackerProvider.Untrack(trackedValue);
        };
    }

    /// <summary>
    /// Resolves Func<T> values by invoking them.
    /// </summary>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075", Justification = "Method invocation is expected for Func<T> resolution")]
    private static Task<object?> ResolveTestDataValueAsync(object? value)
    {
        if (value == null) return Task.FromResult<object?>(null);

        var type = value.GetType();

        // Check if it's a Func<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Func<>))
        {
            var invokeMethod = type.GetMethod("Invoke");
            var result = invokeMethod!.Invoke(value, null);
            return Task.FromResult<object?>(result);
        }

        return Task.FromResult<object?>(value);
    }

}
