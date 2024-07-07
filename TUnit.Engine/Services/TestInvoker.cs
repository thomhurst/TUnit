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
    
    public async Task Invoke(UnInvokedTest unInvokedTest, List<Exception> cleanUpExceptions)
    {
        try
        {
            TestContext.TestContexts.Value = unInvokedTest.TestContext;

            await GlobalTestHookOrchestrator.ExecuteSetups(unInvokedTest.TestContext, EngineCancellationToken.Token);

            foreach (var setUp in unInvokedTest.GetSetUps(EngineCancellationToken.Token))
            {
                await RunHelpers.RunAsync(setUp);
            }
            
            await unInvokedTest.ExecuteTest(unInvokedTest.TestContext.CancellationToken);
        }
        finally
        {
            foreach (var cleanUp in unInvokedTest.GetCleanUps(EngineCancellationToken.Token))
            {
                await RunHelpers.RunSafelyAsync(cleanUp, cleanUpExceptions);
            }
            
            await RunHelpers.RunSafelyAsync(() => _disposer.DisposeAsync(unInvokedTest.TestContext.TestInformation.ClassInstance), cleanUpExceptions);
            await GlobalTestHookOrchestrator.ExecuteCleanUps(unInvokedTest.TestContext, cleanUpExceptions, EngineCancellationToken.Token);
        }
    }
}