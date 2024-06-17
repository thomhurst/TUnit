using TUnit.Core;

namespace TUnit.Engine;

internal class TestInvoker
{
    public async Task Invoke(UnInvokedTest unInvokedTest)
    {
        var cleanUpExceptions = new List<Exception>();
        
        try
        {
            TestDictionary.TestContexts.Value = unInvokedTest.TestContext;

            await GlobalTestHookOrchestrator.ExecuteSetups(unInvokedTest.TestContext);
            await ClassHookOrchestrator.ExecuteSetups(unInvokedTest.TestContext.TestInformation.ClassType);

            await unInvokedTest.RunBeforeEachTestSetUps();

            await unInvokedTest.ExecuteTest();
        }
        finally
        {
            await unInvokedTest.RunAfterEachTestCleanUps(cleanUpExceptions);
            
            await ClassHookOrchestrator.ExecuteCleanUpsIfLastInstance(unInvokedTest.TestContext.TestInformation.ClassInstance, unInvokedTest.TestContext.TestInformation.ClassType, cleanUpExceptions);
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