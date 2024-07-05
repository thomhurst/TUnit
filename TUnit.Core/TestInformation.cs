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
    public required int CurrentRepeatAttempt { get; init; }
    public required int RepeatLimit { get; init; }
    public required int RetryLimit { get; init; }
    public int CurrentRetryAttempt { get; internal set; }

    public required TimeSpan? Timeout { get; init; }
    
    public required IReadOnlyList<string>? NotInParallelConstraintKeys { get; init; }
    public required IReadOnlyDictionary<string, string> CustomProperties { get; init; }

    [JsonIgnore]
    public required Attribute[] AssemblyAttributes { get; init; }
    
    [JsonIgnore]
    public required Attribute[] ClassAttributes { get; init; }
    
    [JsonIgnore]
    public required Attribute[] TestAttributes { get; init; }
    
    [JsonIgnore]
    public required Attribute[] Attributes { get; init; }

    [JsonIgnore]
    internal RetryAttribute? RetryAttribute => Attributes.OfType<RetryAttribute>().FirstOrDefault();
    
    public required Type ReturnType { get; init; }
    
    public required int Order { get; init; }
    public required string TestFilePath { get; init; }
    public required int TestLineNumber { get; init; }
    public required string DisplayName { get; set; }
    
    public required TestData[] InternalTestClassArguments { internal get; init; }

    public required TestData[] InternalTestMethodArguments { internal get; init; }


    internal bool IsSameTest(TestInformation testInformation) => TestName == testInformation.TestName &&
                                                                 ClassType == testInformation.ClassType &&
                                                                 TestMethodParameterTypes.SequenceEqual(testInformation.TestMethodParameterTypes);
}