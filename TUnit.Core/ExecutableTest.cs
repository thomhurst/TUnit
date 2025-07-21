namespace TUnit.Core;

public abstract class ExecutableTest
{
    public required string TestId { get; init; }

    /// <summary>
    /// Includes parameter values for data-driven tests
    /// </summary>
    public required string DisplayName { get; init; }

    public virtual TestMetadata Metadata { get; init; } = null!;

    /// <summary>
    /// Empty for parameterless tests
    /// </summary>
    public required object?[] Arguments { get; init; }

    /// <summary>
    /// Empty for parameterless constructors
    /// </summary>
    public object?[] ClassArguments { get; init; } = [];

    public abstract Task<object> CreateInstanceAsync();

    public abstract Task InvokeTestAsync(object instance, CancellationToken cancellationToken);

    public Dictionary<string, object?> PropertyValues { get; init; } = new();

    public required Func<TestContext, CancellationToken, Task>[] BeforeTestHooks { get; init; }

    public required Func<TestContext, CancellationToken, Task>[] AfterTestHooks { get; init; }

    public required TestContext Context
    {
        get;
        init
        {
            field = value;
            value.InternalExecutableTest = this;
        }
    }

    public ExecutableTest[] Dependencies { get; set; } = [];

    public TestState State { get; set; } = TestState.NotStarted;

    public TestResult? Result { get; set; }

    public DateTimeOffset? StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public TimeSpan? Duration => StartTime.HasValue && EndTime.HasValue
        ? EndTime.Value - StartTime.Value
        : null;
}
