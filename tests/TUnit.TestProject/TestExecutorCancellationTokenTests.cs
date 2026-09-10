using TUnit.Core.Executors;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class TestExecutorCancellationTokenTests
{
    [Test]
    [TestExecutor<CancellingTestExecutor>]
    public async Task LinkedCancellationToken_IsPassedToTest(CancellationToken cancellationToken)
    {
        await Assert.That(cancellationToken.IsCancellationRequested).IsTrue();
    }
}

public class CancellingTestExecutor : ITestExecutor
{
    public async ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        context.Execution.AddLinkedCancellationToken(cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();

        await action();
    }
}
