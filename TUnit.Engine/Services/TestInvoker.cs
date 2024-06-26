using TUnit.Core;
using TUnit.Engine.Hooks;

namespace TUnit.Engine.Services;

internal class TestInvoker
{
    public async Task Invoke(UnInvokedTest unInvokedTest)
    {
        var cleanUpExceptions = new List<Exception>();
        
        try
        {
            TestDictionary.TestContexts.Value = unInvokedTest.TestContext;

            await GlobalTestHookOrchestrator.ExecuteSetups(unInvokedTest.TestContext);

            await unInvokedTest.RunBeforeEachTestSetUps();

            await unInvokedTest.ExecuteTest();
        }
        finally
        {
            await unInvokedTest.RunAfterEachTestCleanUps(cleanUpExceptions);
            
            await GlobalTestHookOrchestrator.ExecuteCleanUps(unInvokedTest.TestContext, cleanUpExceptions);
        }

        if (cleanUpExceptions.Any())
        {
            if (cleanUpExceptions.Count == 1)
            {
                throw cleanUpExceptions.First();
            }

            throw new AggregateException(cleanUpExceptions);
        }
    }
}