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

            await GlobalTestHookOrchestrator.ExecuteSetups(discoveredTest.TestContext, EngineCancellationToken.Token);

            foreach (var setUp in discoveredTest.GetSetUps())
            {
                await setUp();
            }
            
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
            
            foreach (var cleanUp in discoveredTest.GetCleanUps())
            {
                await RunHelpers.RunSafelyAsync(cleanUp, cleanUpExceptions);
            }
            
            await RunHelpers.RunSafelyAsync(() => _disposer.DisposeAsync(discoveredTest.TestContext.TestDetails.ClassInstance), cleanUpExceptions);
            await GlobalTestHookOrchestrator.ExecuteCleanUps(discoveredTest.TestContext, cleanUpExceptions, EngineCancellationToken.Token);
        }
    }
}