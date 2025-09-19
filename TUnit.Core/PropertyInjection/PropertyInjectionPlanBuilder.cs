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
    /// </summary>
    public static PropertyInjectionPlan BuildSourceGeneratedPlan(Type type)
    {
        var propertySource = PropertySourceRegistry.GetSource(type);
        var sourceGenProps = propertySource?.ShouldInitialize == true 
            ? propertySource.GetPropertyMetadata().ToArray() 
            : Array.Empty<PropertyInjectionMetadata>();
            
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
    public static PropertyInjectionPlan BuildReflectionPlan(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(p => p.CanWrite || p.SetMethod?.IsPublic == false);  // Include init-only properties

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