using TUnit.Core.Enums;

namespace TUnit.Core;

/// <summary>
/// Unified model representing a test variation that works across both source generation and reflection modes.
/// This model contains all information needed to execute a test in either mode.
/// </summary>
public sealed class TestVariation
{
    /// <summary>
    /// Unique identifier for this test variation.
    /// </summary>
    public required string TestId { get; init; }

    /// <summary>
    /// Display name for this test variation.
    /// </summary>
    public required string TestName { get; init; }

    /// <summary>
    /// Execution mode for this test variation.
    /// </summary>
    public required TestExecutionMode ExecutionMode { get; init; }

    /// <summary>
    /// Method metadata for the test method.
    /// </summary>
    public required MethodMetadata MethodMetadata { get; init; }

    /// <summary>
    /// Class metadata for the test class.
    /// </summary>
    public required ClassMetadata ClassMetadata { get; init; }

    /// <summary>
    /// Arguments for the test class constructor.
    /// </summary>
    public object?[]? ClassArguments { get; init; }

    /// <summary>
    /// Arguments for the test method.
    /// </summary>
    public object?[]? MethodArguments { get; init; }

    /// <summary>
    /// Property values for dependency injection.
    /// </summary>
    public IDictionary<string, object?>? PropertyValues { get; init; }

    /// <summary>
    /// Test file path for debugging and reporting.
    /// </summary>
    public string? TestFilePath { get; init; }

    /// <summary>
    /// Test line number for debugging and reporting.
    /// </summary>
    public int? TestLineNumber { get; init; }

    /// <summary>
    /// Timeout for this test variation.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Repeat count for this test variation.
    /// </summary>
    public int RepeatCount { get; init; } = 1;

    /// <summary>
    /// Index of this variation within repeats.
    /// </summary>
    public int RepeatIndex { get; init; }

    /// <summary>
    /// Index of class data variation.
    /// </summary>
    public int ClassDataIndex { get; init; }

    /// <summary>
    /// Index of method data variation.
    /// </summary>
    public int MethodDataIndex { get; init; }

    /// <summary>
    /// Category tags for this test.
    /// </summary>
    public IReadOnlyList<string> Categories { get; init; } = [];

    /// <summary>
    /// Attributes applied to this test method.
    /// </summary>
    public IReadOnlyList<Attribute> Attributes { get; init; } = [];

    /// <summary>
    /// Source generation specific data (only populated in source generation mode).
    /// </summary>
    public SourceGeneratedTestData? SourceGeneratedData { get; init; }

    /// <summary>
    /// Creates a copy of this test variation with updated properties.
    /// </summary>
    public TestVariation WithUpdates(
        string? testId = null,
        string? testName = null,
        object?[]? classArguments = null,
        object?[]? methodArguments = null,
        IDictionary<string, object?>? propertyValues = null,
        TimeSpan? timeout = null,
        int? repeatIndex = null,
        int? classDataIndex = null,
        int? methodDataIndex = null)
    {
        return new TestVariation
        {
            TestId = testId ?? TestId,
            TestName = testName ?? TestName,
            ExecutionMode = ExecutionMode,
            MethodMetadata = MethodMetadata,
            ClassMetadata = ClassMetadata,
            ClassArguments = classArguments ?? ClassArguments,
            MethodArguments = methodArguments ?? MethodArguments,
            PropertyValues = propertyValues ?? PropertyValues,
            TestFilePath = TestFilePath,
            TestLineNumber = TestLineNumber,
            Timeout = timeout ?? Timeout,
            RepeatCount = RepeatCount,
            RepeatIndex = repeatIndex ?? RepeatIndex,
            ClassDataIndex = classDataIndex ?? ClassDataIndex,
            MethodDataIndex = methodDataIndex ?? MethodDataIndex,
            Categories = Categories,
            Attributes = Attributes,
            SourceGeneratedData = SourceGeneratedData
        };
    }
}

/// <summary>
/// Source generation specific data for AOT-safe test execution.
/// </summary>
public sealed class SourceGeneratedTestData
{
    /// <summary>
    /// Factory delegate for creating test class instances (source generated).
    /// </summary>
    public Func<object>? ClassInstanceFactory { get; init; }

    /// <summary>
    /// Invoker delegate for calling test methods (source generated).
    /// </summary>
    public Func<object, object?[], Task<object?>>? MethodInvoker { get; init; }

    /// <summary>
    /// Property setters for dependency injection (source generated).
    /// </summary>
    public IDictionary<string, Action<object, object?>>? PropertySetters { get; init; }

    /// <summary>
    /// Compiled data sources resolved at compile-time.
    /// </summary>
    public IReadOnlyList<object?[]>? CompiledDataSources { get; init; }
}
