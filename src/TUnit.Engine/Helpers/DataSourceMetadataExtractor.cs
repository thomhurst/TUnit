using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Extracts metadata (DisplayName, Skip, Categories) from data source attributes.
/// </summary>
internal static class DataSourceMetadataExtractor
{
    private static readonly ConcurrentDictionary<Type, (PropertyInfo? DisplayName, PropertyInfo? Skip, PropertyInfo? Categories)> MetadataProperties = new();

    /// <summary>
    /// Extracts metadata from a data source attribute if it has the relevant properties.
    /// </summary>
    /// <remarks>
    /// Uses DynamicDependency to ensure the trimmer preserves public properties on known TUnit data source types.
    /// Custom data source attributes need to ensure their DisplayName/Skip/Categories properties are preserved
    /// if they want these features to work in trimmed/AOT scenarios.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ArgumentsAttribute))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, "TUnit.Core.ArgumentsAttribute`1", "TUnit.Core")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(MethodDataSourceAttribute))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ClassDataSourceAttribute<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(MatrixAttribute))]
    [UnconditionalSuppressMessage("Trimming", "IL2075:Reflection on unknown types",
        Justification = "Known TUnit data source types are preserved via DynamicDependency. Custom data sources must preserve their own properties.")]
    public static TestDataRowMetadata? ExtractFromAttribute(IDataSourceAttribute? dataSource)
    {
        if (dataSource is null or NoDataSource)
        {
            return null;
        }

        // ArgumentsAttribute is sealed, so these reads have the same semantics as reflection.
        if (dataSource is ArgumentsAttribute arguments)
        {
            return arguments.DisplayName is null && arguments.Skip is null && arguments.Categories is null
                ? null
                : new TestDataRowMetadata(arguments.DisplayName, null, arguments.Skip, arguments.Categories);
        }

        // Property lookups are cached per type: this runs for every generated test row.
        var (displayNameProp, skipProp, categoriesProp) = MetadataProperties.GetOrAdd(dataSource.GetType(), LookupMetadataProperties);

        // Try to get DisplayName property
        var displayName = displayNameProp?.GetValue(dataSource) as string;

        // Try to get Skip property
        var skip = skipProp?.GetValue(dataSource) as string;

        // Try to get Categories property
        var categories = categoriesProp?.GetValue(dataSource) as string[];

        if (displayName is null && skip is null && categories is null)
        {
            return null;
        }

        return new TestDataRowMetadata(displayName, null, skip, categories);
    }

    /// <summary>
    /// Merges metadata from TestDataRow wrapper with metadata from the data source attribute.
    /// TestDataRow metadata takes precedence over attribute metadata.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070:Reflection on unknown types",
        Justification = "Known TUnit data source types are preserved via DynamicDependency on ExtractFromAttribute. Custom data sources must preserve their own properties.")]
    private static (PropertyInfo? DisplayName, PropertyInfo? Skip, PropertyInfo? Categories) LookupMetadataProperties(Type type)
        => (type.GetProperty("DisplayName"), type.GetProperty("Skip"), type.GetProperty("Categories"));

    public static TestDataRowMetadata? Merge(TestDataRowMetadata? rowMetadata, TestDataRowMetadata? attributeMetadata)
    {
        if (rowMetadata is null && attributeMetadata is null)
        {
            return null;
        }

        if (rowMetadata is null)
        {
            return attributeMetadata;
        }

        if (attributeMetadata is null)
        {
            return rowMetadata;
        }

        // Row metadata takes precedence
        return rowMetadata.MergeWith(attributeMetadata);
    }
}
