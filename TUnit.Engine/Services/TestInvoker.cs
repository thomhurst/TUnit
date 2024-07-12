using TUnit.Core;
using TUnit.Engine.Helpers;
using TUnit.Engine.Hooks;

namespace TUnit.Engine.Services;

internal class TestInvoker
{
    private readonly Disposer _disposer;

    public TestInvoker(Disposer disposer)
    {
        _disposer = disposer;
    }
    
    public async Task Invoke(DiscoveredTest discoveredTest, List<Exception> cleanUpExceptions, CancellationToken cancellationToken)
    {
        try
        {
            TestContext.TestContexts.Value = discoveredTest.TestContext;

            await GlobalStaticTestHookOrchestrator.ExecuteSetups(discoveredTest.TestContext, EngineCancellationToken.Token);

            await TestHookOrchestrator.ExecuteSetups(discoveredTest.TestContext.TestDetails.ClassInstance!, discoveredTest.TestContext);
            
            discoveredTest.TestContext.SetUpEnd = DateTimeOffset.Now;
            
            discoveredTest.TestContext.TestStart = DateTimeOffset.Now;
            
            await discoveredTest.ExecuteTest(cancellationToken);
            
            discoveredTest.TestContext.TestEnd = DateTimeOffset.Now;
        }
        finally
        {
            discoveredTest.TestContext.SetUpEnd ??= DateTimeOffset.Now;
            
            discoveredTest.TestContext.TestStart ??= DateTimeOffset.Now;
            discoveredTest.TestContext.TestEnd ??= DateTimeOffset.Now;
            
            discoveredTest.TestContext.CleanUpStart = DateTimeOffset.Now;
            
            await TestHookOrchestrator.ExecuteCleanUps(discoveredTest.TestContext.TestDetails.ClassInstance!, discoveredTest.TestContext, cleanUpExceptions);
            
            await GlobalStaticTestHookOrchestrator.ExecuteCleanUps(discoveredTest.TestContext, cleanUpExceptions, EngineCancellationToken.Token);
            
            await RunHelpers.RunSafelyAsync(() => _disposer.DisposeAsync(discoveredTest.TestContext.TestDetails.ClassInstance), cleanUpExceptions);
        }
    }
}