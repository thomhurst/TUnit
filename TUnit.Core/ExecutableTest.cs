namespace TUnit.Core;

public abstract class ExecutableTest
{
    public required string TestId { get; init; }

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

    public TestResult? Result
    {
        get => Context.Result;
        set => Context.Result = value;
    }

    public DateTimeOffset? StartTime
    {
        get => Context.TestStart;
        set => Context.TestStart = value ?? DateTimeOffset.UtcNow;
    }

    public DateTimeOffset? EndTime { get => Context.TestEnd; set => Context.TestEnd = value; }

    public TimeSpan? Duration => StartTime.HasValue && EndTime.HasValue
        ? EndTime.Value - StartTime.Value
        : null;
}
