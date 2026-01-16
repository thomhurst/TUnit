using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core.PropertyInjection;

/// <summary>
/// Responsible for building property injection plans for types.
/// Follows Single Responsibility Principle by focusing only on plan creation.
/// </summary>
internal static class PropertyInjectionPlanBuilder
{
    /// <summary>
    /// Walks up the inheritance chain from the given type to typeof(object),
    /// invoking the action for each type in the hierarchy.
    /// </summary>
    /// <param name="type">The starting type.</param>
    /// <param name="action">The action to invoke for each type in the inheritance chain.</param>
    private static void WalkInheritanceChain(Type type, Action<Type> action)
    {
        var currentType = type;
        while (currentType != null && currentType != typeof(object))
        {
            action(currentType);
            currentType = currentType.BaseType;
        }
    }

    /// <summary>
    /// Creates an injection plan for source-generated mode.
    /// Walks the inheritance chain to include all injectable properties from base classes.
    /// </summary>
    public static PropertyInjectionPlan BuildSourceGeneratedPlan(Type type)
    {
        var allProperties = new List<PropertyInjectionMetadata>();
        var processedProperties = new HashSet<string>();

        // Walk up the inheritance chain to find all properties with data sources
        WalkInheritanceChain(type, currentType =>
        {
            var propertySource = PropertySourceRegistry.GetSource(currentType);

            if (propertySource?.ShouldInitialize == true)
            {
                foreach (var metadata in propertySource.GetPropertyMetadata())
                {
                    // Skip if we've already processed a property with this name (overridden in derived class)
                    if (processedProperties.Add(metadata.PropertyName))
                    {
                        allProperties.Add(metadata);
                    }
                }
            }
        });

        var sourceGenProps = allProperties.ToArray();

        return new PropertyInjectionPlan
        {
            Type = type,
            SourceGeneratedProperties = sourceGenProps,
            ReflectionProperties = [],
            HasProperties = sourceGenProps.Length > 0
        };
    }

    /// <summary>
    /// Creates an injection plan for reflection mode.
    /// </summary>
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Reflection mode requires runtime property discovery")]
    #endif
    public static PropertyInjectionPlan BuildReflectionPlan(Type type)
    {
        var propertyDataSourcePairs = new List<(PropertyInfo property, IDataSourceAttribute dataSource)>();
        var processedProperties = new HashSet<string>();

        // Walk up the inheritance chain to find all properties with data source attributes
        WalkInheritanceChain(type, currentType =>
        {
            var properties = currentType.GetProperties(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(p => p.CanWrite || p.SetMethod?.IsPublic == false);  // Include init-only properties

            foreach (var property in properties)
            {
                // Skip if we've already processed a property with this name (overridden in derived class)
                if (!processedProperties.Add(property.Name))
                {
                    continue;
                }

                // Check for data source attributes, including inherited attributes
                foreach (var attr in property.GetCustomAttributes(inherit: true))
                {
                    if (attr is IDataSourceAttribute dataSourceAttr)
                    {
                        propertyDataSourcePairs.Add((property, dataSourceAttr));
                        break; // Only one data source per property
                    }
                }
            }
        });

        return new PropertyInjectionPlan
        {
            Type = type,
            SourceGeneratedProperties = [],
            ReflectionProperties = propertyDataSourcePairs.ToArray(),
            HasProperties = propertyDataSourcePairs.Count > 0
        };
    }

    /// <summary>
    /// Builds an injection plan based on the current execution mode.
    /// Falls back to reflection when source-gen mode has no registered source for a type.
    /// This handles generic types like ErrFixture&lt;MyType&gt; where the source generator
    /// couldn't register a property source for the closed generic type.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Source gen mode has its own path")]
    public static PropertyInjectionPlan Build(Type type)
    {
        if (!SourceRegistrar.IsEnabled)
        {
            return BuildReflectionPlan(type);
        }

        // Try source-generated plan first
        var plan = BuildSourceGeneratedPlan(type);

        // If no properties found in source-gen mode, fall back to reflection
        // This handles generic types that couldn't be registered at compile time
        if (!plan.HasProperties)
        {
            var reflectionPlan = BuildReflectionPlan(type);
            if (reflectionPlan.HasProperties)
            {
                return reflectionPlan;
            }
        }

        return plan;
    }
}

/// <summary>
/// Represents a plan for injecting properties into an object.
/// Provides iterator methods to abstract source-gen vs reflection branching (DRY).
/// </summary>
internal sealed class PropertyInjectionPlan
{
    public required Type Type { get; init; }
    public required PropertyInjectionMetadata[] SourceGeneratedProperties { get; init; }
    public required (PropertyInfo Property, IDataSourceAttribute DataSource)[] ReflectionProperties { get; init; }
    public required bool HasProperties { get; init; }

    /// <summary>
    /// Iterates over all properties in the plan, abstracting source-gen vs reflection.
    /// Call the appropriate callback based on which mode has properties.
    /// </summary>
    /// <param name="onSourceGenerated">Action to invoke for each source-generated property.</param>
    /// <param name="onReflection">Action to invoke for each reflection property.</param>
    public void ForEachProperty(
        Action<PropertyInjectionMetadata> onSourceGenerated,
        Action<(PropertyInfo Property, IDataSourceAttribute DataSource)> onReflection)
    {
        if (SourceGeneratedProperties.Length > 0)
        {
            foreach (var metadata in SourceGeneratedProperties)
            {
                onSourceGenerated(metadata);
            }
        }
        else if (ReflectionProperties.Length > 0)
        {
            foreach (var prop in ReflectionProperties)
            {
                onReflection(prop);
            }
        }
    }

    /// <summary>
    /// Iterates over all properties in the plan asynchronously.
    /// </summary>
    public async Task ForEachPropertyAsync(
        Func<PropertyInjectionMetadata, Task> onSourceGenerated,
        Func<(PropertyInfo Property, IDataSourceAttribute DataSource), Task> onReflection)
    {
        if (SourceGeneratedProperties.Length > 0)
        {
            foreach (var metadata in SourceGeneratedProperties)
            {
                await onSourceGenerated(metadata);
            }
        }
        else if (ReflectionProperties.Length > 0)
        {
            foreach (var prop in ReflectionProperties)
            {
                await onReflection(prop);
            }
        }
    }

    /// <summary>
    /// Executes actions for all properties in parallel.
    /// </summary>
    public Task ForEachPropertyParallelAsync(
        Func<PropertyInjectionMetadata, Task> onSourceGenerated,
        Func<(PropertyInfo Property, IDataSourceAttribute DataSource), Task> onReflection)
    {
        if (SourceGeneratedProperties.Length > 0)
        {
            return Helpers.ParallelTaskHelper.ForEachAsync(SourceGeneratedProperties, onSourceGenerated);
        }
        else if (ReflectionProperties.Length > 0)
        {
            return Helpers.ParallelTaskHelper.ForEachAsync(ReflectionProperties, onReflection);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets property values from an instance, abstracting source-gen vs reflection.
    /// </summary>
    public IEnumerable<object?> GetPropertyValues(object instance)
    {
        if (SourceGeneratedProperties.Length > 0)
        {
            foreach (var metadata in SourceGeneratedProperties)
            {
                var property = metadata.ContainingType.GetProperty(metadata.PropertyName);
                if (property?.CanRead == true)
                {
                    yield return property.GetValue(instance);
                }
            }
        }
        else if (ReflectionProperties.Length > 0)
        {
            foreach (var (property, _) in ReflectionProperties)
            {
                if (property.CanRead)
                {
                    yield return property.GetValue(instance);
                }
            }
        }
    }
}
