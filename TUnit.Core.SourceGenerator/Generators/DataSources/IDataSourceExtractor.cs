using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Generators.DataSources;

/// <summary>
/// Interface for extracting data sources from symbols at different levels.
/// Implementations of this interface are responsible for identifying and extracting
/// data source attributes from class, method, and property symbols.
/// </summary>
public interface IDataSourceExtractor
{
    /// <summary>
    /// Extracts data sources from a symbol (class, method, or property)
    /// </summary>
    /// <param name="symbol">The symbol to extract data sources from</param>
    /// <param name="level">The level at which the data source is defined</param>
    /// <param name="testContext">Context information about the test</param>
    /// <returns>Collection of extracted data sources</returns>
    IEnumerable<ExtractedDataSource> ExtractDataSources(ISymbol symbol, DataSourceLevel level, TestMethodMetadata testContext);
}

/// <summary>
/// Represents the level at which a data source is defined
/// </summary>
public enum DataSourceLevel
{
    Class,
    Method,
    Property
}

/// <summary>
/// Represents an extracted data source with all necessary metadata
/// </summary>
public sealed class ExtractedDataSource
{
    /// <summary>
    /// The attribute that defines this data source
    /// </summary>
    public required AttributeData Attribute { get; init; }

    /// <summary>
    /// Type of data source (Arguments, MethodDataSource, or AsyncDataSourceGenerator)
    /// </summary>
    public required DataSourceType Type { get; init; }

    /// <summary>
    /// Whether this data source is async
    /// </summary>
    public required bool IsAsync { get; init; }

    /// <summary>
    /// Unique key for caching/identification
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// For method data sources - the method symbol
    /// </summary>
    public IMethodSymbol? MethodSymbol { get; init; }

    /// <summary>
    /// For property data sources - the property symbol
    /// </summary>
    public IPropertySymbol? PropertySymbol { get; init; }

    /// <summary>
    /// The type containing the data source
    /// </summary>
    public required ITypeSymbol SourceType { get; init; }

    /// <summary>
    /// The level at which this data source was defined
    /// </summary>
    public required DataSourceLevel Level { get; init; }
}

/// <summary>
/// Type of data source
/// </summary>
public enum DataSourceType
{
    Arguments,
    MethodDataSource,
    AsyncDataSourceGenerator
}
