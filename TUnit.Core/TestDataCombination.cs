namespace TUnit.Core;

/// <summary>
/// Represents a single combination of test data for a specific test method execution.
/// This replaces the more complex ExpandedTestData structure with a simpler data-only approach.
/// </summary>
public class TestDataCombination
{
    /// <summary>
    /// Factory functions that create constructor arguments for the test class instance.
    /// Each function is invoked to get a fresh instance for test isolation.
    /// </summary>
    public Func<object?>[] ClassDataFactories { get; init; } = Array.Empty<Func<object?>>();

    /// <summary>
    /// Factory functions that create arguments for the test method invocation.
    /// Each function is invoked to get a fresh instance for test isolation.
    /// </summary>
    public Func<object?>[] MethodDataFactories { get; init; } = Array.Empty<Func<object?>>();

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
    /// Factory functions for property values to be injected into the test class instance.
    /// Key: property name, Value: factory function that creates the property value
    /// </summary>
    public Dictionary<string, Func<object?>> PropertyValueFactories { get; init; } = new();

    /// <summary>
    /// Exception that occurred during data generation, if any.
    /// When set, this combination represents a failed data generation attempt.
    /// </summary>
    public Exception? DataGenerationException { get; init; }

    /// <summary>
    /// Custom display name for this combination, typically used for error cases.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// The repeat index for tests using the RepeatAttribute.
    /// 0 for the first run, 1 for the second, etc.
    /// </summary>
    public int RepeatIndex { get; init; } = 0;
}