using TUnit.Core.Executors;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Failure)]
[TestExecutor<LinkedTimeoutExecutor>]
[Timeout(1000)]
public class LinkedTokenTimeoutTests
{
    [Test]
    public async Task CurrentLinkedToken(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    [Test]
    public async Task CapturedLinkedToken(CancellationToken cancellationToken)
    {
        TestContext.Current!.Execution.AddLinkedCancellationToken(CancellationToken.None);
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    [Test]
    public async Task CustomLinkedCancellationDiagnostic(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw new OperationCanceledException("Linked timeout diagnostic", cancellationToken);
        }
    }
}

internal sealed class LinkedTimeoutExecutor : ITestExecutor
{
    public ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
    {
        context.Execution.AddLinkedCancellationToken(CancellationToken.None);
        return action();
    }
}
