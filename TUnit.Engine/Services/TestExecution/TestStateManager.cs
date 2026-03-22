using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Manages test state transitions and result creation.
/// Single Responsibility: Test state management.
/// </summary>
internal sealed class TestStateManager
{
    public void MarkRunning(AbstractExecutableTest test)
    {
        test.State = TestState.Running;
        test.StartTime = DateTimeOffset.UtcNow;
    }

    public void MarkCompleted(AbstractExecutableTest test)
    {
        var now = DateTimeOffset.UtcNow;

        test.Result ??= new TestResult
        {
            State = TestState.Passed,
            Start = test.StartTime,
            End = now,
            Duration = now - test.StartTime.GetValueOrDefault(),
            Exception = null,
            ComputerName = EnvironmentHelper.MachineName
        };

        test.State = test.Result.State;
        test.EndTime ??= now;
    }

    public void MarkFailed(AbstractExecutableTest test, Exception exception)
    {
        // Check if result has been overridden - if so, respect the override
        if (test.Context.Execution.Result?.IsOverridden == true)
        {
            test.State = test.Context.Execution.Result.State;
            test.EndTime = test.Context.Execution.Result.End ?? DateTimeOffset.UtcNow;
        }
        else
        {
            test.State = TestState.Failed;
            test.EndTime ??= DateTimeOffset.UtcNow;
            test.Result = new TestResult
            {
                State = TestState.Failed,
                Exception = exception,
                Start = test.StartTime,
                End = test.EndTime,
                Duration = test.EndTime - test.StartTime.GetValueOrDefault(),
                ComputerName = EnvironmentHelper.MachineName
            };
        }
    }

    public void MarkSkipped(AbstractExecutableTest test, string reason)
    {
        test.State = TestState.Skipped;
        var now = DateTimeOffset.UtcNow;

        // Ensure StartTime is set if it wasn't already
        if (!test.StartTime.HasValue)
        {
            test.StartTime = now;
        }

        test.EndTime = now;
        test.Result = new TestResult
        {
            State = TestState.Skipped,
            Exception = new SkipTestException(reason),
            Start = test.StartTime.Value,
            End = test.EndTime,
            Duration = test.EndTime - test.StartTime.GetValueOrDefault(),
            ComputerName = EnvironmentHelper.MachineName
        };
    }

    public void MarkCircularDependencyFailed(AbstractExecutableTest test, Exception exception)
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
            ComputerName = EnvironmentHelper.MachineName
        };
    }

    public void MarkDependencyResolutionFailed(AbstractExecutableTest test, Exception exception)
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
            ComputerName = EnvironmentHelper.MachineName
        };
    }
}
