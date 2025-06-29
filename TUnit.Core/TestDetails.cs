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
    public required object? ClassInstance { get; init; }
    public required object?[] TestMethodArguments { get; set; }
    public required object?[] TestClassArguments { get; set; }
    public TimeSpan? Timeout { get; set; }
    public int RetryLimit { get; set; }
    public string? DisplayName { get; set; }

    // Added for compatibility
    public required MethodMetadata MethodMetadata { get; set; }
    public required ClassMetadata ClassMetadata { get; set; }
    public string TestFilePath { get; set; } = "";
    public int TestLineNumber { get; set; }
    public required Type ReturnType { get; set; }
    public IDictionary<string, object?> TestClassInjectedPropertyArguments { get; init; } = new Dictionary<string, object?>();
    public Type[]? TestMethodParameterTypes { get; set; }
    public List<string> Categories { get; } = new List<string>();
    public Dictionary<string, List<string>> CustomProperties { get; } = new Dictionary<string, List<string>>();
    public Type[]? TestClassParameterTypes { get; set; }

    // Missing properties for compatibility
    public IReadOnlyList<Attribute> Attributes { get; set; } = new List<Attribute>();
    public object?[] ClassMetadataArguments => TestClassArguments;
    public IReadOnlyList<IDataAttribute> DataAttributes { get; set; } = new List<IDataAttribute>();
}

/// <summary>
/// Generic version of TestDetails for compatibility with tests
/// </summary>
public class TestDetails<T> : TestDetails where T : class
{
}
