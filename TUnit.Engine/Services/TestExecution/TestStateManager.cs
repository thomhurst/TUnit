using TUnit.Core;
using TUnit.Core.Exceptions;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Manages test state transitions and result creation.
/// Single Responsibility: Test state management.
/// </summary>
internal sealed class TestStateManager
{
    private readonly SessionResultTracker _sessionResultTracker;

    public TestStateManager(SessionResultTracker sessionResultTracker)
    {
        _sessionResultTracker = sessionResultTracker;
    }
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
        
        // Record the result in session tracker
        _sessionResultTracker.RecordTestResult(test.State);
        
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(AbstractExecutableTest test, Exception exception)
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

        // Record the result in session tracker
        _sessionResultTracker.RecordTestResult(test.State);

        return Task.CompletedTask;
    }

    public Task MarkSkippedAsync(AbstractExecutableTest test, string reason)
    {
        test.State = TestState.Skipped;
        test.EndTime = DateTimeOffset.UtcNow;
        test.Result = new TestResult
        {
            State = TestState.Skipped,
            Exception = new SkipTestException(reason),
            Start = test.StartTime ?? DateTimeOffset.UtcNow,
            End = test.EndTime,
            Duration = test.EndTime - test.StartTime.GetValueOrDefault(),
            ComputerName = Environment.MachineName
        };
        
        // Record the result in session tracker
        _sessionResultTracker.RecordTestResult(test.State);
        
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
        
        // Record the result in session tracker
        _sessionResultTracker.RecordTestResult(test.State);
        
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
        
        // Record the result in session tracker
        _sessionResultTracker.RecordTestResult(test.State);
        
        return Task.CompletedTask;
    }
}
