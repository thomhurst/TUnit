using System.Reflection;

namespace TUnit.Core;

/// <summary>
/// Represents an expanded test with all data resolved from data sources.
/// This is an intermediate representation between TestDefinition and DiscoveredTest.
/// </summary>
public class ExpandedTest
{
    /// <summary>
    /// Unique identifier for this test instance.
    /// </summary>
    public required string TestId { get; init; }

    /// <summary>
    /// Display name for this test instance.
    /// </summary>
    public required string TestName { get; init; }

    /// <summary>
    /// The test class instance.
    /// </summary>
    public required object TestInstance { get; init; }

    /// <summary>
    /// Arguments used to construct the test class.
    /// </summary>
    public object?[]? ClassArguments { get; init; }

    /// <summary>
    /// Arguments to pass to the test method.
    /// </summary>
    public object?[]? MethodArguments { get; init; }

    /// <summary>
    /// Property values to inject into the test class.
    /// </summary>
    public IDictionary<string, object?>? PropertyValues { get; init; }

    /// <summary>
    /// Metadata about the test method.
    /// </summary>
    public required MethodMetadata MethodMetadata { get; init; }

    /// <summary>
    /// The test method to invoke.
    /// </summary>
    public required MethodInfo TestMethod { get; init; }

    /// <summary>
    /// Source file path where the test is defined.
    /// </summary>
    public required string TestFilePath { get; init; }

    /// <summary>
    /// Line number in the source file where the test is defined.
    /// </summary>
    public required int TestLineNumber { get; init; }

    /// <summary>
    /// Timeout for the test execution.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Whether the test is skipped.
    /// </summary>
    public bool IsSkipped { get; init; }

    /// <summary>
    /// Reason for skipping the test.
    /// </summary>
    public string? SkipReason { get; init; }
}
