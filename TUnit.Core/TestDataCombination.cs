namespace TUnit.Core;

/// <summary>
/// Represents a single combination of test data for a specific test method execution.
/// This replaces the more complex ExpandedTestData structure with a simpler data-only approach.
/// </summary>
public class TestDataCombination
{
    /// <summary>
    /// Constructor arguments for the test class instance
    /// </summary>
    public object?[] ClassData { get; init; } = Array.Empty<object?>();

    /// <summary>
    /// Arguments for the test method invocation
    /// </summary>
    public object?[] MethodData { get; init; } = Array.Empty<object?>();

    /// <summary>
    /// Data source indices for deterministic test ID generation.
    /// Used to create unique identifiers like TestId_ds{index1}.{index2}.{index3}
    /// </summary>
    public int[] DataSourceIndices { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Property values to be injected into the test class instance.
    /// Key: property name, Value: property value
    /// </summary>
    public Dictionary<string, object?> PropertyValues { get; init; } = new();
}