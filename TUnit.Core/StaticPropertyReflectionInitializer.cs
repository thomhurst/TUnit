using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Enums;

namespace TUnit.Core;

/// <summary>
/// Handles initialization of static properties with data sources in reflection mode
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access",
    Justification = "Reflection mode requires dynamic access")]
[UnconditionalSuppressMessage("Trimming", "IL2067:Target parameter argument does not satisfy annotation requirements",
    Justification = "Reflection mode requires dynamic access")]
[UnconditionalSuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicProperties'",
    Justification = "Reflection mode requires dynamic access")]
[UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicProperties'",
    Justification = "Reflection mode requires dynamic access")]

public static class StaticPropertyReflectionInitializer
{
    private static readonly ConcurrentDictionary<Type, bool> _initializedTypes = new();

    /// <summary>
    /// Initializes static properties with data sources for all loaded types
    /// </summary>
    public static async Task InitializeAllStaticPropertiesAsync()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && ShouldScanAssembly(a));

        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetExportedTypes()
                    .Where(t => t is { IsClass: true, IsAbstract: false });

                foreach (var type in types)
                {
                    await InitializeStaticPropertiesForType(type);
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail - some assemblies might not be accessible
                Console.WriteLine($"Warning: Failed to scan assembly {assembly.FullName} for static properties: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Initializes static properties with data sources for a specific type
    /// </summary>
    public static async Task InitializeStaticPropertiesForType(Type type)
    {
        // Skip if already initialized
        if (!_initializedTypes.TryAdd(type, true))
        {
            return;
        }

        // Get all static properties with data source attributes
        var staticProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.CanWrite && HasDataSourceAttribute(p));

        foreach (var property in staticProperties)
        {
            try
            {
                await InitializeStaticProperty(type, property);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to initialize static property {type.FullName}.{property.Name}: {ex.Message}", ex);
            }
        }
    }

    private static bool HasDataSourceAttribute(PropertyInfo property)
    {
        return property.GetCustomAttributes()
            .Any(attr => attr is IDataSourceAttribute);
    }

    private static async Task InitializeStaticProperty(Type type, PropertyInfo property)
    {
        if (property.GetCustomAttributes()
                .FirstOrDefault(attr => attr is IDataSourceAttribute) is not IDataSourceAttribute dataSourceAttr)
        {
            return;
        }

        // Create metadata for the data source
        var metadata = new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext()
            {
                DataSourceAttribute = dataSourceAttr,
                TestMetadata = null!,
                Events = new TestContextEvents(),
                ObjectBag = new Dictionary<string, object?>()
            }),
            MembersToGenerate = [],
            TestInformation = null!,
            Type = DataGeneratorType.Property,
            TestSessionId = string.Empty,
            TestClassInstance = null,
            ClassInstanceArguments = null
        };

        // Get the first value from the data source
        await foreach (var dataFactory in dataSourceAttr.GetDataRowsAsync(metadata))
        {
            var dataArray = await dataFactory();
            if (dataArray?.Length > 0)
            {
                var value = dataArray[0];

                // Resolve any async values
                value = await ResolveValue(value);

                // Set the property value
                property.SetValue(null, value);

                // Initialize the value if it's an object
                if (value != null)
                {
                    await ObjectInitializer.InitializeAsync(value);
                }

                // Only use the first value for static properties
                break;
            }
        }
    }

    private static async Task<object?> ResolveValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        // If it's a Task, await it
        if (value is Task task)
        {
            await task.ConfigureAwait(false);

            var taskType = task.GetType();
            if (taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultProperty = taskType.GetProperty("Result");
                return resultProperty?.GetValue(task);
            }

            return null;
        }

        // If it's a Func<T>, invoke it
        var type = value.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Func<>))
        {
            var result = ((Delegate)value).DynamicInvoke();

            // Recursively resolve in case it returns a Task
            return await ResolveValue(result).ConfigureAwait(false);
        }

        return value;
    }

    private static bool ShouldScanAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        if (name == null)
        {
            return false;
        }

        // Skip system and framework assemblies
        if (name.StartsWith("System") || name.StartsWith("Microsoft") || name == "mscorlib" || name == "netstandard")
        {
            return false;
        }

        // Skip TUnit engine assemblies
        if (name == "TUnit.Engine" || name == "TUnit.Core")
        {
            return false;
        }

        // Only scan assemblies that reference TUnit
        return assembly.GetReferencedAssemblies().Any(a =>
            a.Name != null && (a.Name.StartsWith("TUnit") || a.Name == "TUnit"));
    }
}
