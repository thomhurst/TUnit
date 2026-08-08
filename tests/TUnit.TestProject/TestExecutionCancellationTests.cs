using TUnit.Core.Executors;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Failure)]
[TestExecutor<CancellationObservingTestExecutor>]
public class TestExecutionCancellationTests
{
    [Test]
    [Timeout(5_000)]
    public async Task CancelStopsCooperativeTestBody(CancellationToken cancellationToken)
    {
        var execution = TestContext.Current!.Execution;

        ThreadPool.QueueUserWorkItem(static state => ((ITestExecution)state!).Cancel(), execution);

        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    [Test]
    public void CancelMarksTestAsCancelledWhenBodyReturnsNormally()
    {
        TestContext.Current!.Execution.Cancel();
    }
}

public sealed class CancellationObservingTestExecutor : ITestExecutor
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
            // Custom executors may consume cooperative cancellation. The execution result
            // must still remain cancelled when the executor returns successfully.
        }
    }
}

[EngineTest(ExpectedResult.Pass)]
public class LateTestExecutionCancellationTests
{
    private static ITestExecution? _completedExecution;

    [Test]
    public void CaptureExecution()
    {
        _completedExecution = TestContext.Current!.Execution;
    }

    [Test]
    [DependsOn(nameof(CaptureExecution))]
    public void CancelAfterPreviousTestIsDisposed()
    {
        _completedExecution!.Cancel();
    }
}
