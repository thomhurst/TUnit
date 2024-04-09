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

            await OneTimeHookOrchestrator.ExecuteSetups(unInvokedTest.TestContext.TestInformation.ClassType);
            
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
            
            await OneTimeHookOrchestrator.ExecuteCleanUpsIfLastInstance(unInvokedTest.TestContext.TestInformation.ClassType, teardownExceptions);
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
}