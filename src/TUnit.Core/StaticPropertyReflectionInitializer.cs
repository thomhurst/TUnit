using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core.Enums;

namespace TUnit.Core;

/// <summary>
/// Handles initialization of static properties with data sources in reflection mode
/// </summary>
#if NET8_0_OR_GREATER
[RequiresUnreferencedCode("Reflection mode requires dynamic access for static property initialization")]
#endif
public static class StaticPropertyReflectionInitializer
{
    // A static property is shared by every session in the process. Keep one value until
    // all sessions that discovered it have released their claim.
    private static readonly ConditionalWeakTable<TestBuilderContext, ConcurrentDictionary<Type, bool>> _initializedTypes = new();

    private static readonly ConditionalWeakTable<Type, SharedInitialization> _sharedInitializations = new();

    private sealed class SharedInitialization
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public int Owners;
        public TestBuilderContext? Context;
        public Exception? Error;
    }

    /// <summary>
    /// Initializes static properties with data sources for all loaded types
    /// </summary>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Data source initialization may require dynamic code generation")]
#endif
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
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Data source initialization may require dynamic code generation")]
#endif
    public static async Task InitializeStaticPropertiesForType(Type type)
    {
        var context = TestSessionContext.GlobalStaticPropertyContext;
        var initializedTypes = _initializedTypes.GetValue(context, static _ => new());

        var staticProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Static);
        if (staticProperties.Length == 0)
        {
            initializedTypes.TryAdd(type, true);
            return;
        }

        var shared = _sharedInitializations.GetValue(type, static _ => new());
        await shared.Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!initializedTypes.TryAdd(type, true))
            {
                return;
            }

            var initialize = shared.Owners++ == 0;
            if (initialize)
            {
                shared.Context = new TestBuilderContext { TestMetadata = context.TestMetadata };
                shared.Error = null;
            }

            // Register before initialization so partial initialization also gets cleaned up.
            // Different types can be initialized concurrently against the same session.
            lock (context)
            {
                context.Events.OnDispose += (_, _) => ReleaseAsync(shared);
            }

            if (initialize)
            {
                try
                {
                    await InitializeTypeAsync(type, staticProperties, shared.Context!).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    shared.Error = ex;
                    throw;
                }
            }
            else if (shared.Error is { } error)
            {
                throw new InvalidOperationException($"Static property initialization failed for {type.FullName}", error);
            }
        }
        finally
        {
            shared.Gate.Release();
        }
    }

    private static async ValueTask ReleaseAsync(SharedInitialization shared)
    {
        await shared.Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (--shared.Owners != 0)
            {
                return;
            }

            var context = shared.Context!;
            var onDispose = context.EventsIfCreated?.OnDispose;
            shared.Context = null;
            shared.Error = null;
            if (onDispose is null)
            {
                return;
            }

            context.Events.OnDispose = null;
            var exceptions = new List<Exception>();
            foreach (var invocation in onDispose.InvocationList)
            {
                try
                {
                    await invocation.InvokeAsync(context, null!).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException("Errors occurred while disposing reflection static properties", exceptions);
            }
        }
        finally
        {
            shared.Gate.Release();
        }
    }

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Data source initialization may require dynamic code generation")]
#endif
    private static async Task InitializeTypeAsync(Type type, PropertyInfo[] staticProperties, TestBuilderContext context)
    {
        foreach (var property in staticProperties)
        {
            if (!property.CanWrite)
            {
                continue;
            }

            // Single-pass lookup avoids materialising the attribute array twice
            // (once to filter, once to fetch) and skips properties with no data source.
            var dataSourceAttr = GetFirstDataSourceAttribute(property);
            if (dataSourceAttr is null)
            {
                continue;
            }

            try
            {
                await InitializeStaticProperty(property, dataSourceAttr, context);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to initialize static property {type.FullName}.{property.Name}: {ex.Message}", ex);
            }
        }
    }

    private static IDataSourceAttribute? GetFirstDataSourceAttribute(PropertyInfo property)
    {
        foreach (var attribute in property.GetCustomAttributes())
        {
            if (attribute is IDataSourceAttribute dataSource)
            {
                return dataSource;
            }
        }

        return null;
    }

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Data source initialization may require dynamic code generation")]
#endif
    private static async Task InitializeStaticProperty(PropertyInfo property, IDataSourceAttribute dataSourceAttr, TestBuilderContext context)
    {
        // Create metadata for the data source
        var metadata = new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(context),
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
