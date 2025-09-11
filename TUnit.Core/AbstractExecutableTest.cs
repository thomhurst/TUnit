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

    private readonly Lock _executionLock = new();

    internal Func<AbstractExecutableTest, CancellationToken, Task>? ExecutorDelegate { get; set; }

    internal CancellationToken ExecutionCancellationToken { get; set; }

    /// <summary>
    /// Gets the task representing this test's execution.
    /// The task is started lazily on first access in a thread-safe manner.
    /// </summary>
    [field: AllowNull, MaybeNull]
    public Task ExecutionTask
    {
        get
        {
            lock (_executionLock)
            {
                if (field == null)
                {
                    if (ExecutorDelegate == null)
                    {
                        field = Task.FromException(new InvalidOperationException(
                            $"Test {TestId} execution was accessed before executor was set"));
                    }
                    else
                    {
                        field = Task.Run(async () =>
                        {
                            try
                            {
                                await ExecutorDelegate(this, ExecutionCancellationToken).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Test {TestId} execution failed: {ex}");
                            }
                        });
                    }
                }
                return field;
            }
        }
    }

    /// <summary>
    /// Allows the scheduler to trigger execution if not already started
    /// </summary>
    internal void EnsureStarted()
    {
        _ = ExecutionTask;
    }

    public Task CompletionTask => ExecutionTask;

    public DateTimeOffset? EndTime { get => Context.TestEnd; set => Context.TestEnd = value; }

    public TimeSpan? Duration => StartTime.HasValue && EndTime.HasValue
        ? EndTime.Value - StartTime.Value
        : null;
}
