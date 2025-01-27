﻿using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;
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
            if (discoveredTest.TestDetails.ClassInstance is IAsyncInitializer asyncInitializer)
            {
                await logger.LogDebugAsync("Initializing IAsyncInitializer test class...");

                await asyncInitializer.InitializeAsync();
            }

            // In order to set async-local values properly in a before hook, the method doing so needs to have the parents execution context.
            // This is achievable by:
            // - Calling it here (and not in a child/sibling method to the actual test body) as this method calls the test body so is considered a parent
            // - Running synchronous hooks synchronously, so they don't generate a new child execution context which happens in async methods
            var beforeHooks = testHookOrchestrator.CollectBeforeHooks(
                discoveredTest.TestContext.TestDetails.ClassInstance!,
                discoveredTest);

            foreach (var executableHook in beforeHooks)
            {
                if (executableHook.IsSynchronous)
                {
                    await logger.LogDebugAsync("Executing synchronous [Before(Test)] hook");
                    
                    Timings.Record($"Before(Test): {executableHook.Name}", discoveredTest.TestContext, () =>
                        executableHook.Execute(discoveredTest.TestContext, cancellationToken)
                    );
                }
                else
                {
                    await logger.LogDebugAsync("Executing asynchronous [Before(Test)] hook");

                    await Timings.Record($"Before(Test): {executableHook.Name}", discoveredTest.TestContext, () =>
                        executableHook.ExecuteAsync(discoveredTest.TestContext, cancellationToken)
                    );
                }
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
        var afterHooks = testHookOrchestrator.CollectAfterHooks(testContext.TestDetails.ClassInstance!, testContext.InternalDiscoveredTest, cleanUpExceptions);
            
        foreach (var executableHook in afterHooks)
        {
            if (executableHook.IsSynchronous)
            {
                await logger.LogDebugAsync("Executing synchronous [After(Test)] hook");

                Timings.Record($"After(Test): {executableHook.Name}", testContext, () =>
                    executableHook.Execute(testContext, CancellationToken.None)
                );
            }
            else
            {
                await logger.LogDebugAsync("Executing asynchronous [After(Test)] hook");

                await Timings.Record($"After(Test): {executableHook.Name}", testContext, () =>
                    executableHook.ExecuteAsync(testContext, CancellationToken.None)
                );
            }
        }
        
        await logger.LogDebugAsync("Disposing test class");
        await RunHelpers.RunValueTaskSafelyAsync(() => disposer.DisposeAsync(testContext.TestDetails.ClassInstance), cleanUpExceptions);
        
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