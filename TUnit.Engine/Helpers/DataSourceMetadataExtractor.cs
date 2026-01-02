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
    [UnconditionalSuppressMessage("Trimming", "IL2075:Reflection on unknown types",
        Justification = "Data source attributes are preserved by the test framework and their properties are accessed at runtime.")]
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
