using System.Reflection;

namespace TUnit.Core;

public record TestInformation
{
    internal TestInformation()
    {
    }
    
    public required string TestName { get; init; }
    
    public required string[]? TestMethodArgumentTypes { get; init; }
    public required object?[]? TestMethodArguments { get; init; }
    
    public required string[]? TestClassArgumentTypes { get; init; }
    public required object?[]? TestClassArguments { get; init; }
    
    public required List<string> Categories { get; init; }
    
    public required MethodInfo MethodInfo { get; init; }
    public required Type ClassType { get; init; }
    public required object? ClassInstance { get; init; }
    
    public required int RepeatCount { get; init; }
    public required int RetryCount { get; init; }
    public int CurrentExecutionCount { get; internal set; }
    public required TimeSpan Timeout { get; set; }
}