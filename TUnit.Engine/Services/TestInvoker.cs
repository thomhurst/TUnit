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

            await GlobalTestHookOrchestrator.ExecuteSetups(unInvokedTest.TestContext);

            await unInvokedTest.RunBeforeEachTestSetUps();

            await unInvokedTest.ExecuteTest();
        }
        finally
        {
            await unInvokedTest.RunAfterEachTestCleanUps(cleanUpExceptions);
            
            await GlobalTestHookOrchestrator.ExecuteCleanUps(unInvokedTest.TestContext, cleanUpExceptions);
        }
    }
}