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

            await GlobalStaticTestHookOrchestrator.ExecuteSetups(discoveredTest.TestContext);

            await TestHookOrchestrator.ExecuteSetups(discoveredTest.TestContext.TestDetails.ClassInstance!, discoveredTest.TestContext);
            
            await Timings.Record("Main Test Body", discoveredTest.TestContext, () => discoveredTest.ExecuteTest(cancellationToken));
        }
        finally
        {
            await TestHookOrchestrator.ExecuteCleanUps(discoveredTest.TestContext.TestDetails.ClassInstance!, discoveredTest.TestContext, cleanUpExceptions);
            
            await GlobalStaticTestHookOrchestrator.ExecuteCleanUps(discoveredTest.TestContext, cleanUpExceptions);
            
            await RunHelpers.RunSafelyAsync(() => _disposer.DisposeAsync(discoveredTest.TestContext.TestDetails.ClassInstance), cleanUpExceptions);
        }
    }
}