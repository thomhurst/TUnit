using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Engine.Helpers;
using TUnit.Engine.Hooks;

namespace TUnit.Engine.Services;

internal class TestInvoker(TestHookOrchestrator testHookOrchestrator, Disposer disposer)
{
    private readonly SemaphoreSlim _consoleStandardOutLock = new(1, 1);

    public async Task Invoke(DiscoveredTest discoveredTest, CancellationToken cancellationToken, List<Exception> cleanupExceptions)
    {
        try
        {
            if (discoveredTest.TestDetails.ClassInstance is IAsyncInitializer asyncInitializer)
            {
                await asyncInitializer.InitializeAsync();
            }

            await testHookOrchestrator.ExecuteBeforeHooks(discoveredTest.TestContext.TestDetails.ClassInstance!,
                discoveredTest);

            await Timings.Record("Main Test Body", discoveredTest.TestContext,
                () => discoveredTest.ExecuteTest(cancellationToken));
        }
        finally
        {
            await DisposeTest(discoveredTest.TestContext, cleanupExceptions);
        }
    }
    
    private async ValueTask DisposeTest(TestContext testContext, List<Exception> cleanUpExceptions)
    {
        await testHookOrchestrator.ExecuteAfterHooks(testContext.TestDetails.ClassInstance!, testContext.InternalDiscoveredTest, cleanUpExceptions);
            
        await RunHelpers.RunValueTaskSafelyAsync(() => disposer.DisposeAsync(testContext.TestDetails.ClassInstance), cleanUpExceptions);
        
        await _consoleStandardOutLock.WaitAsync();

        try
        {
            await disposer.DisposeAsync(testContext);
        }
        finally
        {
            _consoleStandardOutLock.Release();
        }
    }
}