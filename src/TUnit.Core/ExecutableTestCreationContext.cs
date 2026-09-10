namespace TUnit.Core;

/// <summary>
/// Context containing all parameters needed to create an ExecutableTest
/// </summary>
public sealed class ExecutableTestCreationContext
{
    public required string TestId { get; init; }
    public required string DisplayName { get; init; }
    public required object?[] Arguments { get; init; }
    public required object?[] ClassArguments { get; init; }
    public required TestContext Context { get; init; }
    
    /// <summary>
    /// Factory function to create the test class instance lazily during execution.
    /// </summary>
    public Func<Task<object>>? TestClassInstanceFactory { get; init; }
    
    /// <summary>
    /// Resolved generic type arguments for the test method.
    /// Will be Type.EmptyTypes if the method is not generic.
    /// </summary>
    public Type[] ResolvedMethodGenericArguments { get; init; } = Type.EmptyTypes;
    
    /// <summary>
    /// Resolved generic type arguments for the test class.
    /// Will be Type.EmptyTypes if the class is not generic.
    /// </summary>
    public Type[] ResolvedClassGenericArguments { get; init; } = Type.EmptyTypes;
}
