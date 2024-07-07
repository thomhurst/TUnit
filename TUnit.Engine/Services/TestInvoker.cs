using TUnit.Core;
using TUnit.Engine.Hooks;

namespace TUnit.Engine.Services;

internal class TestInvoker
{
    public async Task Invoke(UnInvokedTest unInvokedTest, List<Exception> cleanUpExceptions)
    {
        try
        {
            TestContext.TestContexts.Value = unInvokedTest.TestContext;

            await GlobalTestHookOrchestrator.ExecuteSetups(unInvokedTest.TestContext, EngineCancellationToken.CancellationTokenSource.Token);

            await unInvokedTest.RunBeforeEachTestSetUps(EngineCancellationToken.CancellationTokenSource.Token);

            await unInvokedTest.ExecuteTest(unInvokedTest.TestContext.CancellationToken);
        }
        finally
        {
            await unInvokedTest.RunAfterEachTestCleanUps(cleanUpExceptions, EngineCancellationToken.CancellationTokenSource.Token);
            
            await GlobalTestHookOrchestrator.ExecuteCleanUps(unInvokedTest.TestContext, cleanUpExceptions, EngineCancellationToken.CancellationTokenSource.Token);
        }
    }
}