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
    /// The index of the data source attribute applied to the method.
    /// -1 if no method data source is present.
    /// </summary>
    public int MethodDataSourceIndex { get; init; } = -1;

    /// <summary>
    /// The index of the data source attribute applied to the class.
    /// -1 if no class data source is present.
    /// </summary>
    public int ClassDataSourceIndex { get; init; } = -1;

    /// <summary>
    /// The loop index within the method data source if it returns multiple rows.
    /// 0 for the first row, 1 for the second, etc.
    /// </summary>
    public int MethodLoopIndex { get; init; } = 0;

    /// <summary>
    /// The loop index within the class data source if it returns multiple rows.
    /// 0 for the first row, 1 for the second, etc.
    /// </summary>
    public int ClassLoopIndex { get; init; } = 0;

    /// <summary>
    /// Property values to be injected into the test class instance.
    /// Key: property name, Value: property value
    /// </summary>
    public Dictionary<string, object?> PropertyValues { get; init; } = new();

    /// <summary>
    /// Indicates if this combination requires runtime data generation
    /// from data source generator attributes like MatrixDataSourceAttribute
    /// </summary>
    public bool IsRuntimeGenerated { get; init; }
}