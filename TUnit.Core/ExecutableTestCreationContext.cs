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
    public required Func<TestContext, CancellationToken, Task>[] BeforeTestHooks { get; init; }
    public required Func<TestContext, CancellationToken, Task>[] AfterTestHooks { get; init; }
    public required TestContext Context { get; init; }
}
