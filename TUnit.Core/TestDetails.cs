using System.Reflection;
using System.Text.Json.Serialization;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public record TestDetails<
[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] 
    TClassType
>() : TestDetails(typeof(TClassType)) where TClassType : class
{
    [JsonIgnore]
    public required ResettableLazy<TClassType> LazyClassInstance { get; init; }

    public override object ClassInstance => LazyClassInstance.Value;
} 

public abstract record TestDetails(Type ClassType)
{
    public required string TestId { get; init; }
    
    public required string TestName { get; init; }
    
    public required Type[] TestMethodParameterTypes { get; init; }
    public required object?[] TestMethodArguments { get; init; }
    
    public required Type[] TestClassParameterTypes { get; init; }
    public required object?[] TestClassArguments { get; init; }
    public required object?[] TestClassProperties { get; init; }
    
    public required IReadOnlyList<string> Categories { get; init; }
    
    public required MethodInfo MethodInfo { get; init; }
    public abstract object? ClassInstance { get; }
    public required int CurrentRepeatAttempt { get; init; }
    public required int RepeatLimit { get; init; }
    public required int RetryLimit { get; init; }

    public required TimeSpan? Timeout { get; init; }
    
    public required IReadOnlyList<string>? NotInParallelConstraintKeys { get; init; }
    public IReadOnlyDictionary<string, string> CustomProperties => InternalCustomProperties;
    internal Dictionary<string, string> InternalCustomProperties { get; } = [];

    [JsonIgnore]
    public required Attribute[] AssemblyAttributes { get; init; }
    
    [JsonIgnore]
    public required Attribute[] ClassAttributes { get; init; }
    
    [JsonIgnore]
    public required Attribute[] TestAttributes { get; init; }
    
    [JsonIgnore]
    public required Attribute[] DataAttributes { get; init; }
    
    [JsonIgnore]
    public required Attribute[] Attributes { get; init; }

    [JsonIgnore]
    internal RetryAttribute? RetryAttribute => Attributes.OfType<RetryAttribute>().FirstOrDefault();
    
    public required Type ReturnType { get; init; }
    
    public required int Order { get; init; }
    public required string TestFilePath { get; init; }
    public required int TestLineNumber { get; init; }
    public string? DisplayName { get; internal set; }
    
    public IParallelLimit? ParallelLimit { get; internal set; }


    internal bool IsSameTest(TestDetails testDetails) => TestName == testDetails.TestName &&
                                                                 ClassType == testDetails.ClassType &&
                                                                 TestMethodParameterTypes.SequenceEqual(testDetails.TestMethodParameterTypes);
}