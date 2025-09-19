using TUnit.Core;
using TUnit.Core.Exceptions;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Manages test state transitions and result creation.
/// Single Responsibility: Test state management.
/// </summary>
internal sealed class TestStateManager
{
    public Task MarkRunningAsync(AbstractExecutableTest test)
    {
        test.State = TestState.Running;
        test.StartTime = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    public Task MarkCompletedAsync(AbstractExecutableTest test)
    {
        test.Result ??= new TestResult
        {
            State = TestState.Passed,
            Start = test.StartTime,
            End = DateTimeOffset.UtcNow,
            Duration = DateTimeOffset.UtcNow - test.StartTime.GetValueOrDefault(),
            Exception = null,
            ComputerName = Environment.MachineName
        };

        test.State = test.Result.State;
        test.EndTime = DateTimeOffset.UtcNow;

        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(AbstractExecutableTest test, Exception exception)
    {
        // Check if result has been overridden - if so, respect the override
        if (test.Context.Result?.IsOverridden == true)
        {
            test.State = test.Context.Result.State;
            test.EndTime = test.Context.Result.End ?? DateTimeOffset.UtcNow;
        }
        else
        {
            test.State = TestState.Failed;
            test.EndTime = DateTimeOffset.UtcNow;
            test.Result = new TestResult
            {
                State = TestState.Failed,
                Exception = exception,
                Start = test.StartTime,
                End = test.EndTime,
                Duration = test.EndTime - test.StartTime.GetValueOrDefault(),
                ComputerName = Environment.MachineName
            };
        }

        return Task.CompletedTask;
    }

    public Task MarkSkippedAsync(AbstractExecutableTest test, string reason)
    {
        test.State = TestState.Skipped;

        // Ensure StartTime is set if it wasn't already
        if (!test.StartTime.HasValue)
        {
            test.StartTime = DateTimeOffset.UtcNow;
        }

        test.EndTime = DateTimeOffset.UtcNow;
        test.Result = new TestResult
        {
            State = TestState.Skipped,
            Exception = new SkipTestException(reason),
            Start = test.StartTime.Value,
            End = test.EndTime,
            Duration = test.EndTime - test.StartTime.GetValueOrDefault(),
            ComputerName = Environment.MachineName
        };

        return Task.CompletedTask;
    }

    public Task MarkCircularDependencyFailedAsync(AbstractExecutableTest test, Exception exception)
    {
        test.State = TestState.Failed;
        var now = DateTimeOffset.UtcNow;
        test.Result = new TestResult
        {
            State = TestState.Failed,
            Exception = exception,
            Start = now,
            End = now,
            Duration = TimeSpan.Zero,
            ComputerName = Environment.MachineName
        };

        return Task.CompletedTask;
    }

    public Task MarkDependencyResolutionFailedAsync(AbstractExecutableTest test, Exception exception)
    {
        test.State = TestState.Failed;

        var now = DateTimeOffset.UtcNow;

        test.Result = new TestResult
        {
            State = TestState.Failed,
            Exception = exception,
            Start = now,
            End = now,
            Duration = TimeSpan.Zero,
            ComputerName = Environment.MachineName
        };

        return Task.CompletedTask;
    }
}
