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
    public Func<Task<object?>>[] ClassDataFactories { get; init; } = [];

    /// <summary>
    /// Factory functions that create arguments for the test method invocation.
    /// Each function is invoked to get a fresh instance for test isolation.
    /// </summary>
    public Func<Task<object?>>[] MethodDataFactories { get; init; } = [];

    public int MethodDataSourceIndex { get; init; } = -1;

    public int ClassDataSourceIndex { get; init; } = -1;

    public int MethodLoopIndex { get; init; } = 0;

    public int ClassLoopIndex { get; init; } = 0;


    /// <summary>
    /// Exception that occurred during data generation, if any.
    /// When set, this combination represents a failed data generation attempt.
    /// </summary>
    public Exception? DataGenerationException { get; init; }

    public string? DisplayName { get; init; }

    public int RepeatIndex { get; init; } = 0;

    public Dictionary<string, Type>? ResolvedGenericTypes { get; init; }
}
