using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Creates test class instances with property injection for reflection mode discovery.
/// This is a simplified version that handles basic property injection scenarios
/// where instance data sources depend on property-injected values.
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode isn't used in AOT scenarios")]
[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Reflection mode isn't used in AOT scenarios")]
[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Reflection mode isn't used in AOT scenarios")]
[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Reflection mode isn't used in AOT scenarios")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection mode isn't used in AOT scenarios")]
[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Reflection mode isn't used in AOT scenarios")]
internal static class ReflectionInstanceFactory
{
    private static readonly ConcurrentDictionary<Type, object?> _instanceCache = new();

    /// <summary>
    /// Creates an instance of the specified type with property injection performed.
    /// </summary>
    public static async Task<object?> CreateInstanceWithPropertyInjectionAsync(Type type)
    {
        // Check cache first to avoid creating multiple instances for the same type
        if (_instanceCache.TryGetValue(type, out var cachedInstance))
        {
            return cachedInstance;
        }

        try
        {
            // Create the instance
            var instance = Activator.CreateInstance(type);
            if (instance == null)
            {
                return null;
            }

            // Perform basic property injection
            await InjectPropertiesAsync(instance, type);

            // Cache the instance
            _instanceCache.TryAdd(type, instance);

            return instance;
        }
        catch
        {
            // If we fail to create an instance, return null and let the caller handle it
            return null;
        }
    }

    private static async Task InjectPropertiesAsync(object instance, Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (!property.CanWrite)
            {
                continue;
            }

            // Look for data source attributes on the property
            var dataSourceAttrs = property.GetCustomAttributes()
                .OfType<IDataSourceAttribute>()
                .ToArray();

            if (dataSourceAttrs.Length == 0)
            {
                continue;
            }

            // Try to get data from the first data source
            var dataSource = dataSourceAttrs[0];

            try
            {
                var metadata = CreatePropertyMetadata(property, type, dataSource);

                await foreach (var factory in dataSource.GetDataRowsAsync(metadata))
                {
                    var dataRow = await factory();
                    if (dataRow is { Length: > 0 })
                    {
                        var value = dataRow[0];

                        // Unwrap Func<T> if needed
                        if (value != null && IsFunc(value.GetType()))
                        {
                            value = InvokeFunc(value);
                        }

                        // Initialize if it implements IAsyncInitializer
                        if (value is IAsyncInitializer initializer)
                        {
                            await initializer.InitializeAsync();
                        }

                        // Recursively inject properties into the value
                        if (value != null)
                        {
                            await InjectPropertiesAsync(value, value.GetType());
                        }

                        property.SetValue(instance, value);
                        break; // Only use the first value
                    }
                }
            }
            catch
            {
                // Ignore errors during property injection - the test will fail with a better error message
            }
        }
    }

    private static DataGeneratorMetadata CreatePropertyMetadata(PropertyInfo property, Type containingType, IDataSourceAttribute dataSource)
    {
        var propertyMetadata = new PropertyMetadata
        {
            IsStatic = false,
            Name = property.Name,
            ClassMetadata = CreateClassMetadata(containingType),
            Type = property.PropertyType,
            ReflectionInfo = property,
            Getter = parent => property.GetValue(parent)!,
            ContainingTypeMetadata = CreateClassMetadata(containingType)
        };

        return new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext
            {
                TestMetadata = null!,
                DataSourceAttribute = dataSource,
                Events = new TestContextEvents(),
                StateBag = new ConcurrentDictionary<string, object?>()
            }),
            MembersToGenerate = [propertyMetadata],
            TestInformation = null,
            Type = Core.Enums.DataGeneratorType.Property,
            TestSessionId = "reflection-discovery",
            TestClassInstance = null,
            ClassInstanceArguments = null,
            InstanceFactory = null // Don't recurse into InstanceFactory here
        };
    }

    private static ClassMetadata CreateClassMetadata(Type type)
    {
        return ClassMetadata.GetOrAdd(type.FullName ?? type.Name, () => new ClassMetadata
        {
            Type = type,
            TypeInfo = new ConcreteType(type),
            Name = type.Name,
            Namespace = type.Namespace ?? string.Empty,
            Assembly = AssemblyMetadata.GetOrAdd(type.Assembly.GetName().Name ?? "Unknown", () => new AssemblyMetadata
            {
                Name = type.Assembly.GetName().Name ?? "Unknown"
            }),
            Properties = [],
            Parameters = [],
            Parent = null
        });
    }

    private static bool IsFunc(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        var genericDef = type.GetGenericTypeDefinition();
        return genericDef == typeof(Func<>);
    }

    private static object? InvokeFunc(object func)
    {
        var invokeMethod = func.GetType().GetMethod("Invoke");
        return invokeMethod?.Invoke(func, null);
    }

    /// <summary>
    /// Clears the instance cache. Should be called at the end of test discovery.
    /// </summary>
    public static void ClearCache()
    {
        _instanceCache.Clear();
    }
}
