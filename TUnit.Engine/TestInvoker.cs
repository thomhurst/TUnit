using TUnit.Core;

namespace TUnit.Engine;

internal class TestInvoker
{
    public TestInvoker()
    {
        
    }

    public async Task Invoke(UnInvokedTest unInvokedTest)
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

            await RunHelpers.RunSafelyAsync(() => Dispose(unInvokedTest.TestClass), teardownExceptions);
            
            await OneTimeCleanUpOrchestrator.NotifyCompletedTestAndRunOneTimeCleanUps(unInvokedTest.TestContext.TestInformation.ClassType, teardownExceptions);
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

    private ValueTask Dispose(object testClass)
    {
        if (testClass is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }

        if (testClass is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    private static IEnumerable<Task> GetThreadSafeOneTimeSetUps(UnInvokedTest unInvokedTest)
    {
        return OneTimeSetUpOrchestrator.Tasks.GetOrAdd(
            unInvokedTest.TestContext.TestInformation.ClassType, _ => unInvokedTest.OneTimeSetUps.Select(x => x.Invoke())
        );
    }
}