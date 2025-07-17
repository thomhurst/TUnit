namespace TUnit.Core;

/// <summary>
/// Simplified test details for the new architecture
/// </summary>
public class TestDetails
{
    public required string TestId { get; init; }
    public required string TestName { get; init; }
    public required Type ClassType { get; init; }
    public required string MethodName { get; init; }
    public required object? ClassInstance { get; set; }
    public required object?[] TestMethodArguments { get; set; }
    public required object?[] TestClassArguments { get; set; }
    public TimeSpan? Timeout { get; set; }
    public int RetryLimit { get; set; }

    // Added for compatibility
    public required MethodMetadata MethodMetadata { get; set; }
    public required ClassMetadata ClassMetadata { get; set; }
    public string TestFilePath { get; set; } = "";
    public int TestLineNumber { get; set; }
    public required Type ReturnType { get; set; }
    public IDictionary<string, object?> TestClassInjectedPropertyArguments { get; init; } = new Dictionary<string, object?>();
    public Type[]? TestMethodParameterTypes { get; set; }
    public List<string> Categories { get; } =
    [
    ];
    public Dictionary<string, List<string>> CustomProperties { get; } = new();
    public Type[]? TestClassParameterTypes { get; set; }

    public required IReadOnlyList<Attribute> Attributes { get; init; }
    public object?[] ClassMetadataArguments => TestClassArguments;
    
    /// <summary>
    /// The data combination that generated this test instance.
    /// Contains resolved generic types for generic tests.
    /// </summary>
    public TestDataCombination? DataCombination { get; set; }
}

/// <summary>
/// Generic version of TestDetails for compatibility with tests
/// </summary>
public class TestDetails<T> : TestDetails where T : class;
