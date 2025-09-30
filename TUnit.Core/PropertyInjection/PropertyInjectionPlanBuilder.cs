using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    /// Creates an injection plan for source-generated mode.
    /// Walks the inheritance chain to include all injectable properties from base classes.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "BaseType reflection is required for inheritance support")]
    public static PropertyInjectionPlan BuildSourceGeneratedPlan(Type type)
    {
        var allProperties = new List<PropertyInjectionMetadata>();
        var processedProperties = new HashSet<string>();

        // Walk up the inheritance chain to find all properties with data sources
        var currentType = type;
        while (currentType != null && currentType != typeof(object))
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

            currentType = currentType.BaseType;
        }

        var sourceGenProps = allProperties.ToArray();

        return new PropertyInjectionPlan
        {
            Type = type,
            SourceGeneratedProperties = sourceGenProps,
            ReflectionProperties = Array.Empty<(PropertyInfo, IDataSourceAttribute)>(),
            HasProperties = sourceGenProps.Length > 0
        };
    }

    /// <summary>
    /// Creates an injection plan for reflection mode.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Reflection mode support")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "BaseType reflection is required for inheritance support")]
    public static PropertyInjectionPlan BuildReflectionPlan(Type type)
    {
        var propertyDataSourcePairs = new List<(PropertyInfo property, IDataSourceAttribute dataSource)>();
        var processedProperties = new HashSet<string>();
        
        // Walk up the inheritance chain to find all properties with data source attributes
        var currentType = type;
        while (currentType != null && currentType != typeof(object))
        {
            var properties = currentType.GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
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
            
            currentType = currentType.BaseType;
        }
        
        return new PropertyInjectionPlan
        {
            Type = type,
            SourceGeneratedProperties = Array.Empty<PropertyInjectionMetadata>(),
            ReflectionProperties = propertyDataSourcePairs.ToArray(),
            HasProperties = propertyDataSourcePairs.Count > 0
        };
    }

    /// <summary>
    /// Builds an injection plan based on the current execution mode.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Handles both AOT and non-AOT scenarios")]
    public static PropertyInjectionPlan Build(Type type)
    {
        return SourceRegistrar.IsEnabled 
            ? BuildSourceGeneratedPlan(type) 
            : BuildReflectionPlan(type);
    }
}

/// <summary>
/// Represents a plan for injecting properties into an object.
/// </summary>
internal sealed class PropertyInjectionPlan
{
    public required Type Type { get; init; }
    public required PropertyInjectionMetadata[] SourceGeneratedProperties { get; init; }
    public required (PropertyInfo Property, IDataSourceAttribute DataSource)[] ReflectionProperties { get; init; }
    public required bool HasProperties { get; init; }
}