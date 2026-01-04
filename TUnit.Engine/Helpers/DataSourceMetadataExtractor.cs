using System.Diagnostics.CodeAnalysis;
using TUnit.Core;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Extracts metadata (DisplayName, Skip, Categories) from data source attributes.
/// </summary>
internal static class DataSourceMetadataExtractor
{
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
        if (dataSource is null)
        {
            return null;
        }

        var type = dataSource.GetType();

        // Try to get DisplayName property
        var displayNameProp = type.GetProperty("DisplayName");
        var displayName = displayNameProp?.GetValue(dataSource) as string;

        // Try to get Skip property
        var skipProp = type.GetProperty("Skip");
        var skip = skipProp?.GetValue(dataSource) as string;

        // Try to get Categories property
        var categoriesProp = type.GetProperty("Categories");
        var categories = categoriesProp?.GetValue(dataSource) as string[];

        if (displayName is null && skip is null && categories is null)
        {
            return null;
        }

        return new TestDataRowMetadata(displayName, skip, categories);
    }

    /// <summary>
    /// Merges metadata from TestDataRow wrapper with metadata from the data source attribute.
    /// TestDataRow metadata takes precedence over attribute metadata.
    /// </summary>
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
