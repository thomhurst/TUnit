using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Models;

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

    /// <summary>
    /// Execution context information for this test, used to coordinate class hooks properly
    /// </summary>
    public TestExecutionContext? ExecutionContext { get; set; }

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

    /// <summary>
    /// Gets the task representing this test's execution, set directly by the scheduler.
    /// </summary>
    public Task? ExecutionTask { get; internal set; }

    public Task CompletionTask => ExecutionTask ?? Task.CompletedTask;

    public DateTimeOffset? EndTime { get => Context.TestEnd; set => Context.TestEnd = value; }

    public TimeSpan? Duration => StartTime.HasValue && EndTime.HasValue
        ? EndTime.Value - StartTime.Value
        : null;
}
