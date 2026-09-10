using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6567;

[EngineTest(ExpectedResult.Pass)]
public sealed class LinkedCancellationTokenFromBeforeHookTests
{
    private CancellationTokenSource? _cancellationTokenSource;

    [Before(Test)]
    public void BeforeTest(TestContext context)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        context.Execution.AddLinkedCancellationToken(_cancellationTokenSource.Token);
    }

    [Test]
    public void LinkedCancellationReachesInjectedToken(CancellationToken cancellationToken)
    {
        _cancellationTokenSource!.Cancel();

        AssertCancellationObserved(cancellationToken);
    }

    [Test]
    [Timeout(5_000)]
    public void AdditionalLinkPreservesInjectedAndCurrentTokens(CancellationToken cancellationToken)
    {
        TestContext.Current!.Execution.AddLinkedCancellationToken(CancellationToken.None);
        _cancellationTokenSource!.Cancel();

        AssertCancellationObserved(cancellationToken);
        AssertCancellationObserved(TestContext.Current.Execution.CancellationToken);
    }

    [After(Test)]
    public void AfterTest()
    {
        _cancellationTokenSource?.Dispose();
    }

    private static void AssertCancellationObserved(CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("Token did not observe linked cancellation.");
        }
    }
}
