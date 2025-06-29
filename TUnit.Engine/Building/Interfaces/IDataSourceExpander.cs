using TUnit.Core;

namespace TUnit.Engine.Building.Interfaces;

/// <summary>
/// Interface for expanding data sources into test variations
/// </summary>
public interface IDataSourceExpander
{
    /// <summary>
    /// Expands all data sources for a test into individual test variations
    /// </summary>
    /// <param name="metadata">Test metadata containing data sources</param>
    /// <returns>Collection of expanded test data for each variation</returns>
    Task<IEnumerable<ExpandedTestData>> ExpandDataSourcesAsync(TestMetadata metadata);
}

/// <summary>
/// Represents a single test variation with all necessary data
/// </summary>
public sealed class ExpandedTestData
{
    /// <summary>
    /// The original test metadata
    /// </summary>
    public required TestMetadata Metadata { get; init; }
    
    /// <summary>
    /// Factory functions for class constructor arguments
    /// </summary>
    public required Func<object?[]> ClassArgumentsFactory { get; init; }
    
    /// <summary>
    /// Factory functions for test method arguments
    /// </summary>
    public required Func<object?[]> MethodArgumentsFactory { get; init; }
    
    /// <summary>
    /// Factory functions for property values (key: property name, value: factory for value)
    /// </summary>
    public required Dictionary<string, Func<object?>> PropertyFactories { get; init; }
    
    /// <summary>
    /// Display text for arguments (used in test name)
    /// </summary>
    public required string ArgumentsDisplayText { get; init; }
    
    /// <summary>
    /// Data source indices for unique test identification
    /// </summary>
    public required int[] DataSourceIndices { get; init; }
}