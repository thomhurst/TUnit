using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Extracts metadata (DisplayName, Skip, Categories) from data source attributes.
/// </summary>
internal static class DataSourceMetadataExtractor
{
    private const BindingFlags DeclaredPublicProperties = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>
    /// Extracts metadata from a data source attribute if it has the relevant properties.
    /// </summary>
    /// <remarks>
    /// Every attribute type has one cached reader: the typed one the source generator registered, or one built
    /// here for reflection mode and for attribute types the generator did not see. Both resolve a setting to the
    /// most derived public property of that name.
    /// </remarks>
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

        // The most common row source. It is sealed, so this check is cheap and the reads match reflection.
        if (dataSource is ArgumentsAttribute arguments)
        {
            return TestDataRowMetadata.FromDataSource(arguments.DisplayName, arguments.Skip, arguments.Categories);
        }

        // One lookup per row: this runs for every generated test row.
        return DataSourceRowMetadataReaders.GetOrAdd(dataSource.GetType(), CreateReader)(dataSource);
    }

    private static Func<IDataSourceAttribute, TestDataRowMetadata?> CreateReader(Type type)
    {
        if (typeof(IDataSourceRowMetadata).IsAssignableFrom(type))
        {
            return static dataSource =>
            {
                var rowSource = (IDataSourceRowMetadata) dataSource;

                return TestDataRowMetadata.FromDataSource(rowSource.DisplayName, rowSource.Skip, rowSource.Categories);
            };
        }

        var displayName = FindReadableProperty(type, "DisplayName");
        var skip = FindReadableProperty(type, "Skip");
        var categories = FindReadableProperty(type, "Categories");

        if (displayName is null && skip is null && categories is null)
        {
            return static _ => null;
        }

        return dataSource => TestDataRowMetadata.FromDataSource(
            displayName?.GetValue(dataSource) as string,
            skip?.GetValue(dataSource) as string,
            categories?.GetValue(dataSource) as string[]);
    }

    // The most derived public property of that name, as the source generator resolves it, or null when it has no
    // getter. Type.GetProperty(name) would throw when a derived property hides one of another type.
    [UnconditionalSuppressMessage("Trimming", "IL2070:Reflection on unknown types",
        Justification = "Fallback only: source-generated mode reads through generated readers, and a trimmed getter is reported.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075:Reflection on unknown types",
        Justification = "Fallback only: source-generated mode reads through generated readers, and a trimmed getter is reported.")]
    private static PropertyInfo? FindReadableProperty(Type type, string name)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            // Type.EmptyTypes leaves out indexers.
            if (current.GetProperty(name, DeclaredPublicProperties, binder: null, returnType: null, Type.EmptyTypes, modifiers: null) is not { } property)
            {
                continue;
            }

            if (property.CanRead)
            {
                return property;
            }

#if NET
            // A write-only property is ignored. Native AOT cannot tell one from a property whose getter the trimmer
            // removed, and ignoring that would silently run a row its data source skips, so it fails instead.
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                throw new InvalidOperationException(
                    $"The getter of '{current}.{name}' is not available, so the setting cannot be applied to the rows of '{type}'. "
                    + "Native AOT removes a getter that no code calls. Keep it by adding "
                    + $"[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof({current.Name}))] to the attribute's constructors.");
            }
#endif

            return null;
        }

        return null;
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
