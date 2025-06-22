using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

/// <summary>
/// Represents dynamic test metadata that requires runtime type resolution and reflection.
/// Used for tests discovered at runtime or those requiring dynamic type construction.
/// </summary>
[RequiresDynamicCode("DynamicTestMetadata uses runtime type resolution and reflection")]
[RequiresUnreferencedCode("DynamicTestMetadata may require types that aren't statically referenced")]
public record DynamicTestMetadata : ITestDescriptor
{
    // ITestDescriptor implementation
    public string TestId => TestIdTemplate; // For dynamic tests, this will be resolved at runtime
    public string DisplayName => DisplayNameTemplate;
    public required string TestFilePath { get; init; }
    public required int TestLineNumber { get; init; }
    public required bool IsAsync { get; init; }
    public required bool IsSkipped { get; init; }
    public string? SkipReason { get; init; }
    public TimeSpan? Timeout { get; init; }
    public required int RepeatCount { get; init; }

    /// <summary>
    /// Unique identifier template for the test. Can contain placeholders for data-driven tests.
    /// </summary>
    public required string TestIdTemplate { get; init; }

    /// <summary>
    /// The type reference of the test class.
    /// Can represent generic types with unresolved type parameters.
    /// </summary>
    public required TypeReference TestClassTypeReference { get; init; }

    /// <summary>
    /// The concrete type of the test class (only available for non-generic types).
    /// For generic types, this will be null and TypeReference must be resolved at runtime.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type? TestClassType { get; init; }

    /// <summary>
    /// Metadata about the test method.
    /// </summary>
    public required MethodMetadata MethodMetadata { get; init; }


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
