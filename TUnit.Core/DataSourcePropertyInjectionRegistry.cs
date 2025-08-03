using System.Collections.Concurrent;

namespace TUnit.Core;

/// <summary>
/// Registry for storing pre-generated property injection metadata for data source attributes.
/// Used by source generation to enable AOT-compatible property injection on data sources.
/// </summary>
public static class DataSourcePropertyInjectionRegistry
{
    private static readonly ConcurrentDictionary<Type, PropertyInjectionData[]> InjectionDataCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyDataSource[]> PropertyDataSourceCache = new();

    /// <summary>
    /// Registers property injection data for a data source attribute type.
    /// Called by generated code at startup.
    /// </summary>
    public static void Register(Type dataSourceType, PropertyInjectionData[] injectionData, PropertyDataSource[] propertyDataSources)
    {
        InjectionDataCache[dataSourceType] = injectionData;
        PropertyDataSourceCache[dataSourceType] = propertyDataSources;
    }

    /// <summary>
    /// Gets property injection data for a data source attribute type.
    /// </summary>
    public static PropertyInjectionData[]? GetInjectionData(Type dataSourceType)
    {
        return InjectionDataCache.TryGetValue(dataSourceType, out var data) ? data : null;
    }

    /// <summary>
    /// Gets property data sources for a data source attribute type.
    /// </summary>
    public static PropertyDataSource[]? GetPropertyDataSources(Type dataSourceType)
    {
        return PropertyDataSourceCache.TryGetValue(dataSourceType, out var sources) ? sources : null;
    }
}