using TUnit.Core;

namespace TUnit.Engine;

public class TestInvoker
{
    public async Task Invoke(UninvokedTest uninvokedTest)
    {
        await Task.Run(async () =>
        {
            var teardownExceptions = new List<Exception>();
            try
            {
                TestDictionary.TestContexts.Value = uninvokedTest.TestContext;

                foreach (var oneTimeSetUp in uninvokedTest.OneTimeSetUps)
                {
                    await oneTimeSetUp();
                }
                
                foreach (var setUp in uninvokedTest.SetUps)
                {
                    await setUp();
                }

                await uninvokedTest.TestBody();
            }
            finally
            {
                foreach (var cleanUp in uninvokedTest.CleanUps)
                {
                    await RunHelpers.RunSafelyAsync(cleanUp, teardownExceptions);
                }
                
                var remainingTests = OneTimeCleanUpOrchestrator.NotifyCompletedTestAndGetRemainingTestsForType(uninvokedTest.TestContext.TestInformation.ClassType);
               
                if (remainingTests == 0)
                {
                    foreach (var oneTimeCleanUp in uninvokedTest.OneTimeCleanUps)
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