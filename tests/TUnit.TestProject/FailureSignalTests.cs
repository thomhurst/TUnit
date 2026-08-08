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
