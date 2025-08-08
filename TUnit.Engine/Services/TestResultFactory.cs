using TUnit.Core;

namespace TUnit.Engine.Services;

internal sealed class TestResultFactory : ITestResultFactory
{
    public TestResult CreatePassedResult(DateTimeOffset startTime)
    {
        var endTime = DateTimeOffset.Now;
        return new TestResult
        {
            State = TestState.Passed,
            Start = startTime,
            End = endTime,
            Duration = endTime - startTime,
            Exception = null,
            ComputerName = Environment.MachineName
        };
    }

    public TestResult CreateFailedResult(DateTimeOffset startTime, Exception exception)
    {
        var endTime = DateTimeOffset.Now;
        return new TestResult
        {
            State = TestState.Failed,
            Start = startTime,
            End = endTime,
            Duration = endTime - startTime,
            Exception = exception,
            ComputerName = Environment.MachineName
        };
    }

    public TestResult CreateSkippedResult(DateTimeOffset startTime, string reason)
    {
        var endTime = DateTimeOffset.Now;
        return new TestResult
        {
            State = TestState.Skipped,
            Start = startTime,
            End = endTime,
            Duration = endTime - startTime,
            Exception = null,
            ComputerName = Environment.MachineName,
            OverrideReason = reason
        };
    }

    public TestResult CreateTimeoutResult(DateTimeOffset startTime, int timeoutMs)
    {
        var endTime = DateTimeOffset.Now;
        return new TestResult
        {
            State = TestState.Timeout,
            Start = startTime,
            End = endTime,
            Duration = endTime - startTime,
            Exception = new TimeoutException($"Test exceeded timeout of {timeoutMs}ms"),
            ComputerName = Environment.MachineName,
            OverrideReason = $"Test exceeded timeout of {timeoutMs}ms"
        };
    }

    public TestResult? CreateCancelledResult(DateTimeOffset startTime)
    {
        var endTime = DateTimeOffset.Now;

        return new TestResult
        {
            State = TestState.Cancelled,
            Start = startTime,
            End = endTime,
            Duration = endTime - startTime,
            Exception = null,
            ComputerName = Environment.MachineName
        };
    }
}
