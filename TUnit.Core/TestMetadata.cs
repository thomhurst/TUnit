using System.Reflection;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Represents compile-time metadata for a test that can be expanded into multiple TestDefinition instances at runtime.
/// This is the data structure emitted by the source generator containing all information needed to build tests.
/// </summary>
public record TestMetadata
{
    /// <summary>
    /// Unique identifier template for the test. Can contain placeholders for data-driven tests.
    /// </summary>
    public required string TestIdTemplate { get; init; }
    
    /// <summary>
    /// The type of the test class.
    /// </summary>
    public required Type TestClassType { get; init; }
    
    /// <summary>
    /// Method information for the test method.
    /// </summary>
    public required MethodInfo TestMethod { get; init; }
    
    /// <summary>
    /// Metadata about the test method.
    /// </summary>
    public required MethodMetadata MethodMetadata { get; init; }
    
    /// <summary>
    /// Source file path where the test is defined.
    /// </summary>
    public required string TestFilePath { get; init; }
    
    /// <summary>
    /// Line number in the source file where the test is defined.
    /// </summary>
    public required int TestLineNumber { get; init; }
    
    /// <summary>
    /// Factory function to create test class instances.
    /// Returns null if class cannot be instantiated.
    /// </summary>
    public required Func<object?[]?, object?> TestClassFactory { get; init; }
    
    /// <summary>
    /// Data sources for class constructor arguments.
    /// Each data source provides an enumerable of argument arrays.
    /// </summary>
    public required IReadOnlyList<IDataSourceProvider> ClassDataSources { get; init; }
    
    /// <summary>
    /// Data sources for test method arguments.
    /// Each data source provides an enumerable of argument arrays.
    /// </summary>
    public required IReadOnlyList<IDataSourceProvider> MethodDataSources { get; init; }
    
    /// <summary>
    /// Properties to be set on the test instance with their data sources.
    /// </summary>
    public required IReadOnlyDictionary<PropertyInfo, IDataSourceProvider> PropertyDataSources { get; init; }
    
    /// <summary>
    /// Display name template for the test. Can contain placeholders like {0}, {1} for arguments.
    /// </summary>
    public required string DisplayNameTemplate { get; init; }
    
    /// <summary>
    /// Number of times to repeat the test.
    /// </summary>
    public required int RepeatCount { get; init; }
    
    /// <summary>
    /// Whether the test is async (returns Task or ValueTask).
    /// </summary>
    public required bool IsAsync { get; init; }
    
    /// <summary>
    /// Whether the test should be skipped.
    /// </summary>
    public required bool IsSkipped { get; init; }
    
    /// <summary>
    /// Skip reason if the test is skipped.
    /// </summary>
    public string? SkipReason { get; init; }
    
    /// <summary>
    /// Test attributes for filtering and metadata.
    /// </summary>
    public required IReadOnlyList<Attribute> Attributes { get; init; }
    
    /// <summary>
    /// Timeout for the test execution.
    /// </summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>
/// Provides data for test arguments from various sources (method, property, inline data, etc.).
/// </summary>
public interface IDataSourceProvider
{
    /// <summary>
    /// Gets the data synchronously. Returns an enumerable of argument arrays.
    /// </summary>
    IEnumerable<object?[]> GetData();
    
    /// <summary>
    /// Gets the data asynchronously. Returns an async enumerable of argument arrays.
    /// </summary>
    IAsyncEnumerable<object?[]> GetDataAsync();
    
    /// <summary>
    /// Whether this data source is async.
    /// </summary>
    bool IsAsync { get; }
    
    /// <summary>
    /// Whether the data should be shared across test instances.
    /// </summary>
    bool IsShared { get; }
}