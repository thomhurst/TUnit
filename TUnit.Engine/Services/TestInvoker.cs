using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;
using TUnit.Engine.Exceptions;
using TUnit.Engine.Extensions;
using TUnit.Engine.Helpers;
using TUnit.Engine.Hooks;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

internal class TestInvoker(TestHookOrchestrator testHookOrchestrator, TUnitFrameworkLogger logger, Disposer disposer)
{
    private readonly SemaphoreSlim _consoleStandardOutLock = new(1, 1);

    public async Task Invoke(DiscoveredTest discoveredTest, CancellationToken cancellationToken, List<Exception> cleanupExceptions)
    {
        try
        {
            foreach (var onInitializeObject in discoveredTest.TestContext.GetOnInitializeObjects())
            {
                await logger.LogDebugAsync($"Initializing IAsyncInitializer: {onInitializeObject.GetType().Name}...");

                await onInitializeObject.InitializeAsync();
            }

            ExecutionContextHelper.RestoreContext(await testHookOrchestrator.ExecuteBeforeHooks(discoveredTest, cancellationToken));
            
            foreach (var testStartEventsObject in discoveredTest.TestContext.GetTestStartEventObjects())
            {
                await logger.LogDebugAsync($"Executing ITestStartEventReceiver: {testStartEventsObject.GetType().Name}");

                await testStartEventsObject.OnTestStart(new BeforeTestContext(discoveredTest));
            }

            await logger.LogDebugAsync("Executing test body");

            await Timings.Record("Test Body", discoveredTest.TestContext,
                () => discoveredTest.ExecuteTest(cancellationToken));
            
            discoveredTest.TestContext.SetResult(null);
        }
        catch (Exception ex)
        {
            discoveredTest.TestContext.SetResult(ex);
            throw;
        }
        finally
        {
            await DisposeTest(discoveredTest.TestContext, cleanupExceptions);
        }
    }
    
    private async ValueTask DisposeTest(TestContext testContext, List<Exception> cleanUpExceptions)
    {
        var afterHooks = testHookOrchestrator.CollectAfterHooks(testContext.TestDetails.ClassInstance, testContext.InternalDiscoveredTest, cleanUpExceptions);
            
        foreach (var executableHook in afterHooks)
        {
            {
                await logger.LogDebugAsync($"Executing [After(Test)] hook: {executableHook.MethodInfo.Class.Name}.{executableHook.Name}");

                await Timings.Record($"After(Test): {executableHook.Name}", testContext, () =>
                {
                    try
                    {
                        return executableHook.ExecuteAsync(testContext, CancellationToken.None);
                    }
                    catch (Exception e)
                    {
                        throw new HookFailedException($"Error executing [After(Test)] hook: {executableHook.MethodInfo.Class.Name}.{executableHook.Name}", e);
                    }
                });
            }
        }
        
        foreach (var testEndEventsObject in testContext.GetTestEndEventObjects())
        {
            await logger.LogDebugAsync($"Executing ITestEndEventReceiver: {testEndEventsObject.GetType().Name}");

            await RunHelpers.RunValueTaskSafelyAsync(() => testEndEventsObject.OnTestEnd(testContext),
                cleanUpExceptions);
        }
        
        foreach (var disposableObject in testContext.GetOnDisposeObjects())
        {
            await logger.LogDebugAsync($"Disposing: {disposableObject.GetType().Name}");

            await RunHelpers.RunValueTaskSafelyAsync(() => disposer.DisposeAsync(disposableObject),
                cleanUpExceptions);
        }
        
        await _consoleStandardOutLock.WaitAsync();

        try
        {
            await logger.LogDebugAsync("Disposing test context");
            await disposer.DisposeAsync(testContext);
        }
        finally
        {
            _consoleStandardOutLock.Release();
        }
    }
}