using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Helpers;

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
    /// TUnit's own row sources are read through <see cref="IDataSourceRowMetadata"/>, and custom data source
    /// attributes through the readers the source generator registers. Reflection is the fallback for reflection
    /// mode and for attribute types the generator did not see.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2075:Reflection on unknown types",
        Justification = "Fallback only: source-generated mode reads through generated readers, and a trimmed getter is skipped.")]
    public static TestDataRowMetadata? ExtractFromAttribute(IDataSourceAttribute? dataSource)
    {
        if (dataSource is null or NoDataSource)
        {
            return null;
        }

        // A generic class's class rows, partitioned by the class type arguments they infer, keep the settings of
        // the source they came from: reflection mode partitions them at discovery, source-generated mode at run time
        switch (dataSource)
        {
            case StaticDataSourceAttribute { PartitionOf: { } partitionedSource }:
                return ExtractFromAttribute(partitionedSource);
            case GenericClassRowPartitionDataSource partition:
                return ExtractFromAttribute(partition.InnerDataSource);
        }

        // TUnit's own row sources are sealed, so these reads have the same semantics as reflection.
        if (dataSource is IDataSourceRowMetadata rowSource)
        {
            return rowSource.DisplayName is null && rowSource.Skip is null && rowSource.Categories is null
                ? null
                : new TestDataRowMetadata(rowSource.DisplayName, null, rowSource.Skip, rowSource.Categories);
        }

        // Source-generated mode: the generator emitted typed reads for this attribute type, so nothing depends
        // on the trimmer keeping its property getters (#7782).
        if (DataSourceRowMetadataReaders.TryRead(dataSource, out var generatedMetadata))
        {
            return generatedMetadata;
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

    [UnconditionalSuppressMessage("Trimming", "IL2070:Reflection on unknown types",
        Justification = "Fallback only: source-generated mode reads through generated readers, and a trimmed getter is skipped.")]
    private static (PropertyInfo? DisplayName, PropertyInfo? Skip, PropertyInfo? Categories) LookupMetadataProperties(Type type)
    {
        return (Readable(type.GetProperty("DisplayName")), Readable(type.GetProperty("Skip")), Readable(type.GetProperty("Categories")));

        // The trimmer can keep a property for its setter alone; reading it would throw.
        static PropertyInfo? Readable(PropertyInfo? property) => property is { CanRead: true } ? property : null;
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
