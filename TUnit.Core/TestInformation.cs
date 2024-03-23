using System.Reflection;

namespace TUnit.Core;

public record TestInformation
{
    public TestInformation()
    {
        LazyTestAndClassAttributes = new(
            () => MethodInfo!.GetCustomAttributes()
                .Concat(ClassType!.GetCustomAttributes())
        );

        LazyRetryAttribute = new(
            () => LazyTestAndClassAttributes.Value.OfType<RetryAttribute>().FirstOrDefault()
        );
    }
    
    public required string TestName { get; init; }
    
    public required string[]? TestMethodParameterTypes { get; init; }
    public required object?[]? TestMethodArguments { get; init; }
    
    public required string[]? TestClassParameterTypes { get; init; }
    public required object?[]? TestClassArguments { get; init; }
    
    public required List<string> Categories { get; init; }
    
    public required MethodInfo MethodInfo { get; init; }
    public required Type ClassType { get; init; }
    public required object? ClassInstance { get; init; }
    
    public required int RepeatCount { get; init; }
    public required int RetryCount { get; init; }
    public int CurrentExecutionCount { get; internal set; }
    public required TimeSpan? Timeout { get; init; }
    public required string[]? NotInParallelConstraintKeys { get; init; }
    public required IReadOnlyDictionary<string, string> CustomProperties { get; init; }

    internal Lazy<IEnumerable<Attribute>> LazyTestAndClassAttributes { get; }
    
    internal Lazy<RetryAttribute?> LazyRetryAttribute { get; }
}