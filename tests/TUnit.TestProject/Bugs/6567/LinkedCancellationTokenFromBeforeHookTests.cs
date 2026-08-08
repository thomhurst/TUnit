using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6567;

[EngineTest(ExpectedResult.Pass)]
public sealed class LinkedCancellationTokenFromBeforeHookTests
{
    private CancellationTokenSource? _cancellationTokenSource;

    [Before(Test)]
    public void BeforeTest(TestContext context)
    {
        _cancellationTokenSource = new CancellationTokenSource(100);
        context.Execution.AddLinkedCancellationToken(_cancellationTokenSource.Token);
    }

    [Test]
    public Task LinkedCancellationReachesInjectedToken(CancellationToken cancellationToken)
    {
        return ObserveCancellation(cancellationToken);
    }

    [Test]
    [Timeout(5_000)]
    public async Task AdditionalLinkPreservesInjectedAndCurrentTokens(CancellationToken cancellationToken)
    {
        TestContext.Current!.Execution.AddLinkedCancellationToken(CancellationToken.None);

        await Task.WhenAll(
            ObserveCancellation(cancellationToken),
            ObserveCancellation(TestContext.Current.Execution.CancellationToken));
    }

    [After(Test)]
    public void AfterTest()
    {
        _cancellationTokenSource?.Dispose();
    }

    private static async Task ObserveCancellation(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(1_000, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        throw new InvalidOperationException("Token did not observe linked cancellation.");
    }
}
