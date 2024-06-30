using System.Reflection;
using System.Text.Json.Serialization;

namespace TUnit.Core;

public record TestInformation<TClassType> : TestInformation
{
    [JsonIgnore]
    public required ResettableLazy<TClassType?> LazyClassInstance { get; init; }

    public override object? ClassInstance => LazyClassInstance.Value;
} 

public abstract record TestInformation
{
    public required string TestId { get; init; }
    
    public required string TestName { get; init; }
    
    public required Type[] TestMethodParameterTypes { get; init; }
    public required object?[] TestMethodArguments { get; init; }
    
    public required Type[] TestClassParameterTypes { get; init; }
    public required object?[] TestClassArguments { get; init; }
    
    public required IReadOnlyList<string> Categories { get; init; }
    
    public required MethodInfo MethodInfo { get; init; }
    public required Type ClassType { get; init; }
    public abstract object? ClassInstance { get; }
    public required int RepeatIndex { get; init; }
    public required int RepeatCount { get; init; }
    public required int RetryCount { get; init; }
    public int CurrentExecutionCount { get; internal set; }
    
    public required int MethodRepeatCount { get; init; }
    public required int ClassRepeatCount { get; init; }

    public required TimeSpan? Timeout { get; init; }
    public required IReadOnlyList<string>? NotInParallelConstraintKeys { get; init; }
    public required IReadOnlyDictionary<string, string> CustomProperties { get; init; }

    [JsonIgnore]
    public required Attribute[] TestAndClassAttributes { get; init; }

    [JsonIgnore]
    internal RetryAttribute? RetryAttribute => TestAndClassAttributes.OfType<RetryAttribute>().FirstOrDefault();
    
    public required Type ReturnType { get; init; }
    
    public required int Order { get; init; }
    public required string TestFilePath { get; init; }
    public required int TestLineNumber { get; init; }
    public required string DisplayName { get; set; }

    internal bool IsSameTest(TestInformation testInformation) => TestName == testInformation.TestName &&
                                                                 ClassType == testInformation.ClassType &&
                                                                 TestMethodParameterTypes.SequenceEqual(testInformation.TestMethodParameterTypes);
}