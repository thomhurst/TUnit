using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public record TestMetadata<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TClassType>
{
    public required string TestId { get; init; }
    public required string DisplayName { get; init; }
    public required MethodInfo MethodInfo { get; init; }
    
    public required int RepeatLimit { get; init; }
    public required int CurrentRepeatAttempt { get; init; }
    
    public required string TestFilePath { get; init; }
    public required int TestLineNumber { get; init; }


    public required ResettableLazy<TClassType> ResettableClassFactory { get; init; }
    public required Func<TClassType, CancellationToken, Task> TestMethodFactory { get; init; }
    
    public required object?[] TestClassArguments { get; init; }
    public required object?[] TestMethodArguments { get; init; }
    public required object?[] TestClassProperties { get; init; }
    
    public required ITestExecutor TestExecutor { get; init; }

    public required IClassConstructor? ClassConstructor { get; init; }
    
    public required IParallelLimit? ParallelLimit { get; init; }
    
    // Need to be referenced statically for AOT
    public required Type[] AttributeTypes { get; init; }
    
    public required Attribute[] DataAttributes { get; init; }
    
    public required Dictionary<string, object?> ObjectBag { get; init; }
}