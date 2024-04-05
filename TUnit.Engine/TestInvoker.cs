using TUnit.Core;

namespace TUnit.Engine;

public class TestInvoker
{
    public async Task Invoke(UnInvokedTest unInvokedTest)
    {
        await Task.Run(async () =>
        {
            var teardownExceptions = new List<Exception>();
            try
            {
                TestDictionary.TestContexts.Value = unInvokedTest.TestContext;

                foreach (var oneTimeSetUp in unInvokedTest.OneTimeSetUps)
                {
                    await oneTimeSetUp();
                }
                
                foreach (var setUp in unInvokedTest.BeforeEachTestSetUps)
                {
                    await setUp();
                }

                await unInvokedTest.TestBody();
            }
            finally
            {
                foreach (var cleanUp in unInvokedTest.AfterEachTestCleanUps)
                {
                    await RunHelpers.RunSafelyAsync(cleanUp, teardownExceptions);
                }
                
                var remainingTests = OneTimeCleanUpOrchestrator.NotifyCompletedTestAndGetRemainingTestsForType(unInvokedTest.TestContext.TestInformation.ClassType);
               
                if (remainingTests == 0)
                {
                    foreach (var oneTimeCleanUp in unInvokedTest.OneTimeCleanUps)
                    {
                        await RunHelpers.RunSafelyAsync(oneTimeCleanUp, teardownExceptions);
                    }
                }
            }
           
            if (teardownExceptions.Any())
            {
                if (teardownExceptions.Count == 1)
                {
                    throw teardownExceptions.First();
                }
                
                throw new AggregateException(teardownExceptions);
            }
        });
    }
}