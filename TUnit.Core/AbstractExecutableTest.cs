using System.Diagnostics;

namespace TUnit.Core;

[DebuggerDisplay("{Metadata.TestClassType.Name}.{Metadata.TestName}")]
public abstract class AbstractExecutableTest
{
    public required string TestId { get; init; }

    public virtual TestMetadata Metadata { get; init; } = null!;

    public required object?[] Arguments { get; init; }

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

    public ResolvedDependency[] Dependencies { get; set; } = [];

    public TestState State { get; set; } = TestState.NotStarted;

    public TestResult? Result
    {
        get => Context.Result;
        set
        {
            Context.Result = value;
            _taskCompletionSource.TrySetResult();
        }
    }

    public DateTimeOffset? StartTime
    {
        get => Context.TestStart;
        set => Context.TestStart = value ?? DateTimeOffset.UtcNow;
    }

    public Task CompletionTask => _taskCompletionSource.Task;

    private readonly TaskCompletionSource _taskCompletionSource = new();

    public DateTimeOffset? EndTime { get => Context.TestEnd; set => Context.TestEnd = value; }

    public TimeSpan? Duration => StartTime.HasValue && EndTime.HasValue
        ? EndTime.Value - StartTime.Value
        : null;
}
