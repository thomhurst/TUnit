using TUnit.Core;

namespace TUnit.Engine.Json;

internal record JsonOutput
{
    public required string TestId { get; init; }
    
    public required string TestName { get; init; }
    public required string DisplayName { get; set; }
    
    public required Type ClassType { get; init; }
    
    public required Type[]? TestMethodParameterTypes { get; init; }
    public required string?[]? TestMethodArguments { get; init; }
    
    public required Type[]? TestClassParameterTypes { get; init; }
    public required string?[]? TestClassArguments { get; init; }
    //
    public required IReadOnlyList<string> Categories { get; init; }
    
    public required int RetryLimit { get; init; }

    public required TimeSpan? Timeout { get; init; }
    
    public required IReadOnlyList<string>? NotInParallelConstraintKeys { get; init; }
    
    public required IReadOnlyDictionary<string, string> CustomProperties { get; init; }
    
    public required Type ReturnType { get; init; }
    
    public required int Order { get; init; }
    
    public required string TestFilePath { get; init; }
    public required int TestLineNumber { get; init; }
    
    public required Dictionary<string, object> ObjectBag { get; init; }
    
    public required TUnitTestResult? Result { get; set; }
}