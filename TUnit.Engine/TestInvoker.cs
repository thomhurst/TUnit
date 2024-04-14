using TUnit.Core;

namespace TUnit.Engine;

internal class TestInvoker
{
    public async Task Invoke(UnInvokedTest unInvokedTest)
    {
        var teardownExceptions = new List<Exception>();
        try
        {
            TestDictionary.TestContexts.Value = unInvokedTest.TestContext;

            await ClassHookOrchestrator.ExecuteSetups(unInvokedTest.TestContext.TestInformation.ClassType);

            await unInvokedTest.RunBeforeEachTestSetUps();

            await unInvokedTest.ExecuteTest();
        }
        finally
        {
            await unInvokedTest.RunAfterEachTestCleanUps(teardownExceptions);
            
            await ClassHookOrchestrator.ExecuteCleanUpsIfLastInstance(unInvokedTest.TestContext.TestInformation.ClassType, teardownExceptions);
        }

        if (teardownExceptions.Any())
        {
            if (teardownExceptions.Count == 1)
            {
                throw teardownExceptions.First();
            }

            throw new AggregateException(teardownExceptions);
        }
    }
}