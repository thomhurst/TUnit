using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Simplified test details for the new architecture
/// </summary>
public class TestDetails
{
    public required string TestId { get; init; }
    public required string TestName { get; init; }
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    public required Type ClassType { get; init; }
    public required string MethodName { get; init; }
    public required object ClassInstance { get; set; }
    public required object?[] TestMethodArguments { get; set; }
    public required object?[] TestClassArguments { get; set; }
    public TimeSpan? Timeout { get; set; }
    public int RetryLimit { get; set; }

    public required MethodMetadata MethodMetadata { get; set; }
    public string TestFilePath { get; set; } = "";
    public int TestLineNumber { get; set; }
    public required Type ReturnType { get; set; }
    public IDictionary<string, object?> TestClassInjectedPropertyArguments { get; init; } = new Dictionary<string, object?>();
    public List<string> Categories { get; } =
    [
    ];
    public Dictionary<string, List<string>> CustomProperties { get; } = new();
    public Type[]? TestClassParameterTypes { get; set; }

    public required IReadOnlyList<Attribute> Attributes { get; init; }
    public object?[] ClassMetadataArguments => TestClassArguments;
    
    /// <summary>
    /// Resolved generic type arguments for the test method.
    /// Will be Type.EmptyTypes if the method is not generic.
    /// </summary>
    public Type[] MethodGenericArguments { get; set; } = Type.EmptyTypes;
    
    /// <summary>
    /// Resolved generic type arguments for the test class.
    /// Will be Type.EmptyTypes if the class is not generic.
    /// </summary>
    public Type[] ClassGenericArguments { get; set; } = Type.EmptyTypes;
}

/// <summary>
/// Generic version of TestDetails for compatibility with tests
/// </summary>
public class TestDetails<T> : TestDetails where T : class;
