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

                var threadSafeOneTimeSetUps = GetThreadSafeOneTimeSetUps(unInvokedTest);
                
                foreach (var oneTimeSetUp in threadSafeOneTimeSetUps)
                {
                    // We should get the same Task for each test within the class
                    // So we await the same Task to ensure it's finished first
                    // and also gives the benefit of rethrowing the same exception if it failed
                    await oneTimeSetUp;
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

    private static IEnumerable<Task> GetThreadSafeOneTimeSetUps(UnInvokedTest unInvokedTest)
    {
        return OneTimeSetUpOrchestrator.Tasks.GetOrAdd(
            unInvokedTest.TestContext.TestInformation.ClassType, _ => unInvokedTest.OneTimeSetUps.Select(x => x.Invoke())
        );
    }
}