using TUnit.Core.Executors;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Failure)]
public class FailureSignalTests : ITestStartEventReceiver
{
    private ITestFailureSignal _failureSignal = null!;

    public ValueTask OnTestStart(TestContext context)
    {
        _failureSignal = context.Execution.CreateFailureSignal();
        return default;
    }

    [Test]
    public async Task ExceptionReportedFromCallbackFailsTest(CancellationToken cancellationToken)
    {
        ThreadPool.QueueUserWorkItem(
            static state => ((ITestFailureSignal)state!).Report(
                new InvalidOperationException("Failure reported from callback")),
            _failureSignal);

        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    [Test]
    public async Task ReasonReportedFromCallbackFailsTest(CancellationToken cancellationToken)
    {
        ThreadPool.QueueUserWorkItem(
            static state => ((ITestFailureSignal)state!).Report("Failure reason reported from callback"),
            _failureSignal);

        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    [Test]
    public async Task ReportedFailureDoesNotWaitForeverForNonCooperativeBody()
    {
        ThreadPool.QueueUserWorkItem(
            static state => ((ITestFailureSignal)state!).Report("Non-cooperative body failed"),
            _failureSignal);

        await Task.Delay(Timeout.InfiniteTimeSpan);
    }
}

[EngineTest(ExpectedResult.Pass)]
[Retry(1)]
public class FailureSignalRetryTests : ITestStartEventReceiver
{
    private const string PreviousSignalKey = nameof(FailureSignalRetryTests);
    private ITestFailureSignal _failureSignal = null!;
    private ITestFailureSignal? _previousSignal;

    public ValueTask OnTestStart(TestContext context)
    {
        context.StateBag.TryGetValue(PreviousSignalKey, out _previousSignal);
        _failureSignal = context.Execution.CreateFailureSignal();
        context.StateBag[PreviousSignalKey] = _failureSignal;
        return default;
    }

    [Test]
    public async Task SignalIsScopedToCurrentRetryAttempt(CancellationToken cancellationToken)
    {
        if (TestContext.Current!.Execution.CurrentRetryAttempt == 0)
        {
            ThreadPool.QueueUserWorkItem(
                static state => ((ITestFailureSignal)state!).Report("First attempt failed"),
                _failureSignal);

            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return;
        }

        await Assert.That(_previousSignal).IsNotNull();
        await Assert.That(_previousSignal!.TryReport("Late failure from previous attempt")).IsFalse();
    }
}

[EngineTest(ExpectedResult.Failure)]
[TestExecutor<FailureSignalTestExecutor>]
public class FailureSignalCustomExecutorTests : ITestStartEventReceiver, ITestEndEventReceiver
{
    internal const string ExecutorCompletedKey = nameof(ExecutorCompletedKey);
    internal const string ThrowFromExecutorKey = nameof(ThrowFromExecutorKey);
    private ITestFailureSignal _failureSignal = null!;

    public ValueTask OnTestStart(TestContext context)
    {
        _failureSignal = context.Execution.CreateFailureSignal();
        return default;
    }

    public ValueTask OnTestEnd(TestContext context)
    {
        if (!context.StateBag.TryGetValue<bool>(ExecutorCompletedKey, out var completed) || !completed)
        {
            throw new InvalidOperationException("The custom test executor did not complete after signal cancellation");
        }

        return default;
    }

    [Test]
    [Timeout(5_000)]
    public async Task SignalCancelsTheTokenUsedByACustomExecutor(CancellationToken cancellationToken)
    {
        ThreadPool.QueueUserWorkItem(
            static state => ((ITestFailureSignal)state!).Report("Failure reported through custom executor"),
            _failureSignal);

        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    [Test]
    public async Task SignalAndCustomExecutorFailureAreBothReported(CancellationToken cancellationToken)
    {
        TestContext.Current!.StateBag[ThrowFromExecutorKey] = true;

        ThreadPool.QueueUserWorkItem(
            static state => ((ITestFailureSignal)state!).Report("Failure reported before executor cleanup"),
            _failureSignal);

        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }
}

public sealed class FailureSignalTestExecutor : ITestExecutor
{
    public async ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
    {
        using var executorCancellationSource = new CancellationTokenSource();
        context.Execution.AddLinkedCancellationToken(executorCancellationSource.Token);

        try
        {
            await action();
        }
        catch (OperationCanceledException) when (context.Execution.CancellationToken.IsCancellationRequested)
        {
            // Custom executors may translate or consume cooperative cancellation. The reported
            // failure must still fail the test even when the executor returns successfully.
        }
        finally
        {
            context.StateBag[FailureSignalCustomExecutorTests.ExecutorCompletedKey] = true;
        }

        if (context.StateBag.TryGetValue<bool>(FailureSignalCustomExecutorTests.ThrowFromExecutorKey, out var shouldThrow)
            && shouldThrow)
        {
            throw new InvalidOperationException("Custom executor cleanup failed");
        }
    }
}

[EngineTest(ExpectedResult.Pass)]
[TestExecutor<LateReportingFailureSignalTestExecutor>]
public class FailureSignalCustomExecutorLateReportTests : ITestStartEventReceiver
{
    internal const string FailureSignalKey = nameof(FailureSignalKey);

    public ValueTask OnTestStart(TestContext context)
    {
        context.StateBag[FailureSignalKey] = context.Execution.CreateFailureSignal();
        return default;
    }

    [Test]
    public void ReportAfterTestBodyCompletesIsIgnored()
    {
    }
}

public sealed class LateReportingFailureSignalTestExecutor : ITestExecutor
{
    public async ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
    {
        await action();

        var signal = (ITestFailureSignal)context.StateBag[FailureSignalCustomExecutorLateReportTests.FailureSignalKey]!;

        if (signal.TryReport("Late report from custom executor"))
        {
            throw new InvalidOperationException("A failure signal accepted a report after the test body completed");
        }
    }
}
