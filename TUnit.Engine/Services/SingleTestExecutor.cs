using System.Diagnostics;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Exceptions;
using TUnit.Core.Extensions;
using TUnit.Core.Logging;
using TUnit.Engine.Extensions;
using TUnit.Engine.Helpers;
using TUnit.Engine.Hooks;
using TUnit.Engine.Logging;

#pragma warning disable TPEXP

namespace TUnit.Engine.Services;

internal class SingleTestExecutor(
    IExtension extension,
    InstanceTracker instanceTracker,
    TestInvoker testInvoker,
    ParallelLimitLockProvider parallelLimitLockProvider,
    AssemblyHookOrchestrator assemblyHookOrchestrator,
    ClassHookOrchestrator classHookOrchestrator,
    ITUnitMessageBus messageBus,
    TUnitFrameworkLogger logger,
    EngineCancellationToken engineCancellationToken,
    TestRegistrar testRegistrar)
    : IDataProducer
{
    private static readonly Lock Lock = new();
    
    public Task ExecuteTestAsync(DiscoveredTest test, ITestExecutionFilter? filter, ExecuteRequestContext context,
        bool isStartedAsDependencyForAnotherTest)
    {
        lock (Lock)
        {
            return test.TestContext.TestTask ??= Task.Run(() => ExecuteTestInternalAsync(test, filter, context, isStartedAsDependencyForAnotherTest));
        }
    }

    private async Task ExecuteTestInternalAsync(DiscoveredTest test, ITestExecutionFilter? filter, ExecuteRequestContext context, bool isStartedAsDependencyForAnotherTest)
    {
        var semaphore = WaitForParallelLimiter(test, isStartedAsDependencyForAnotherTest);

        if (semaphore != null)
        {
            await semaphore.WaitAsync();
        }

        DateTimeOffset? start = null;
        try
        {
            var testContext = test.TestContext;

            if (engineCancellationToken.Token.IsCancellationRequested)
            {
                await messageBus.Cancelled(testContext);

                engineCancellationToken.Token.ThrowIfCancellationRequested();
            }

            await messageBus.InProgress(testContext);

            var cleanUpExceptions = new List<Exception>();

            start = DateTimeOffset.Now;

            try
            {
                if (testContext.Result?.Exception is {} exception)
                {
                    throw exception;
                }
                
                await WaitForDependencies(test, filter, context);

                start = DateTimeOffset.Now;

                await RegisterIfNotAlready(testContext);

                if (testContext.SkipReason != null)
                {
                    throw new SkipTestException(testContext.SkipReason);
                }

                if (engineCancellationToken.Token.IsCancellationRequested)
                {
                    throw new SkipTestException("The test session has been cancelled...");
                }
                
                // Ideally all these 'Set up' hooks would be refactored into inner/classes and/or methods,
                // But users may want to set AsyncLocal values, and so the method must be a parent/ancestor of the method that starts the test!
                // So actually refactoring these into other methods would mean they wouldn't be a parent/ancestor and would break async local!
                var assemblyHooksTaskCompletionSource = assemblyHookOrchestrator.PreviouslyRunBeforeHooks.GetOrAdd(testContext.TestDetails.TestClass.Type.Assembly,
                    _ => new TaskCompletionSource<bool>(), out var assemblyHooksTaskPreviouslyExisted);
                
                if (assemblyHooksTaskPreviouslyExisted)
                {
                    await assemblyHooksTaskCompletionSource.Task;
                }
                else
                {
                    try
                    {
                        var beforeAssemblyHooks =
                            assemblyHookOrchestrator.CollectBeforeHooks(test.TestContext.TestDetails.TestClass.Type.Assembly);
                        var assemblyHookContext =
                            assemblyHookOrchestrator.GetContext(test.TestContext.TestDetails.TestClass.Type.Assembly);

                        AssemblyHookContext.Current = assemblyHookContext;

                        foreach (var beforeHook in beforeAssemblyHooks)
                        {
                            if (beforeHook.IsSynchronous)
                            {
                                await logger.LogDebugAsync("Executing synchronous [Before(Assembly)] hook");

                                beforeHook.Execute(assemblyHookContext, CancellationToken.None);
                            }
                            else
                            {
                                await logger.LogDebugAsync("Executing asynchronous [Before(Assembly)] hook");

                                await beforeHook.ExecuteAsync(assemblyHookContext, CancellationToken.None);
                            }
                        }

                        AssemblyHookContext.Current = null;
                        assemblyHooksTaskCompletionSource.SetResult(false);
                    }
                    catch (Exception e)
                    {
                        assemblyHooksTaskCompletionSource.SetException(e);
                        throw;
                    }
                }

                var classHooksTaskCompletionSource = classHookOrchestrator.PreviouslyRunBeforeHooks.GetOrAdd(testContext.TestDetails.TestClass.Type,
                    _ => new TaskCompletionSource<bool>(), out var classHooksTaskPreviouslyExisted);
                
                if (classHooksTaskPreviouslyExisted)
                {
                    await classHooksTaskCompletionSource.Task;
                }
                else
                {
                    try
                    {
                        var beforeClassHooks =
                            classHookOrchestrator.CollectBeforeHooks(test.TestContext.TestDetails.TestClass.Type);
                        var classHookContext = classHookOrchestrator.GetContext(test.TestContext.TestDetails.TestClass.Type);

                        ClassHookContext.Current = classHookContext;

                        foreach (var beforeHook in beforeClassHooks)
                        {
                            if (beforeHook.IsSynchronous)
                            {
                                await logger.LogDebugAsync("Executing synchronous [Before(Class)] hook");

                                beforeHook.Execute(classHookContext, CancellationToken.None);
                            }
                            else
                            {
                                await logger.LogDebugAsync("Executing asynchronous [Before(Class)] hook");

                                await beforeHook.ExecuteAsync(classHookContext, CancellationToken.None);
                            }
                        }

                        ClassHookContext.Current = null;
                        classHooksTaskCompletionSource.SetResult(false);
                    }
                    catch (Exception e)
                    {
                        classHooksTaskCompletionSource.SetException(e);
                        throw;
                    }
                }

                TestContext.Current = testContext;
                
                await ExecuteWithRetries(test, cleanUpExceptions);

                ExceptionsHelper.ThrowIfAny(cleanUpExceptions);
            }
            catch (SkipTestException skipTestException)
            {
                await logger.LogInformationAsync($"Skipping {testContext.GetClassTypeName()}.{testContext.GetTestDisplayName()}...");
                
                testContext.SetResult(skipTestException);

                await messageBus.Skipped(testContext, skipTestException.Reason);
            }
            catch (Exception e)
            {
                await logger.LogDebugAsync($"Error in test: {e}");
                testContext.SetResult(e);
                throw;
            }
            finally
            {
                await RunCleanUps(test, testContext, cleanUpExceptions);
            }
        }
        finally
        {
            semaphore?.Release();

            var result = test.TestContext.Result!;
            
            var task = result.Status switch
            {
                Status.Passed => messageBus.Passed(test.TestContext, start.GetValueOrDefault()),
                Status.Failed => messageBus.Failed(test.TestContext, result.Exception!, start.GetValueOrDefault()),
                _ => default,
            };

            await task;
        }
    }

    private ValueTask RegisterIfNotAlready(TestContext testContext)
    {
        lock (Lock)
        {
            // Could not be registered if it's triggered from a [DependsOn]
            if (!testContext.IsRegistered)
            {
                return testRegistrar.RegisterInstance(testContext.InternalDiscoveredTest, _ => default);
            }
        }

        return default;
    }

    private async Task RunCleanUps(DiscoveredTest test, TestContext testContext,
        List<Exception> cleanUpExceptions)
    {
        try
        {
            if (testContext.Result?.Status == Status.Skipped)
            {
                foreach (var testSkippedEventReceiver in testContext.GetTestSkippedEventObjects())
                {
                    await logger.LogDebugAsync("Executing ITestSkippedEventReceivers");

                    await RunHelpers.RunValueTaskSafelyAsync(() => testSkippedEventReceiver.OnTestSkipped(testContext),
                        cleanUpExceptions);
                }
            }

            TestContext.Current = null;

            await ExecuteStaticAfterHooks(test, testContext, cleanUpExceptions);

            foreach (var artifact in testContext.Artifacts)
            {
                await messageBus.TestArtifact(testContext, artifact);
            }

            ExceptionsHelper.ThrowIfAny(cleanUpExceptions);
        }
        catch (Exception e)
        {
            testContext.SetResult(e);
            throw;
        }
    }

    private async Task ExecuteStaticAfterHooks(DiscoveredTest test, TestContext testContext,
        List<Exception> cleanUpExceptions)
    {
        var afterClassHooks = classHookOrchestrator.CollectAfterHooks(testContext, test.TestContext.TestDetails.TestClass.Type);
        var classHookContext = classHookOrchestrator.GetContext(test.TestContext.TestDetails.TestClass.Type);
                
        ClassHookContext.Current = classHookContext;
                
        foreach (var afterHook in afterClassHooks)
        {
            if (afterHook.IsSynchronous)
            {
                await logger.LogDebugAsync("Executing synchronous [After(Class)] hook");

                RunHelpers.RunSafely(() => afterHook.Execute(classHookContext, CancellationToken.None), cleanUpExceptions);
            }
            else
            {
                await logger.LogDebugAsync("Executing asynchronous [After(Class)] hook");

                await RunHelpers.RunSafelyAsync(() => afterHook.ExecuteAsync(classHookContext, CancellationToken.None), cleanUpExceptions);
            }
        }
                
        ClassHookContext.Current = null;
        
        var afterAssemblyHooks = assemblyHookOrchestrator.CollectAfterHooks(testContext, test.TestContext.TestDetails.TestClass.Type.Assembly);
        var assemblyHookContext = assemblyHookOrchestrator.GetContext(test.TestContext.TestDetails.TestClass.Type.Assembly);

        AssemblyHookContext.Current = assemblyHookContext;
                
        foreach (var afterHook in afterAssemblyHooks)
        {
            if (afterHook.IsSynchronous)
            {
                await logger.LogDebugAsync("Executing synchronous [After(Assembly)] hook");

                RunHelpers.RunSafely(() => afterHook.Execute(assemblyHookContext, CancellationToken.None), cleanUpExceptions);
            }
            else
            {
                await logger.LogDebugAsync("Executing asynchronous [After(Assembly)] hook");

                await RunHelpers.RunSafelyAsync(() => afterHook.ExecuteAsync(assemblyHookContext, CancellationToken.None), cleanUpExceptions);
            }
        }
                
        AssemblyHookContext.Current = null;
        
        if (instanceTracker.IsLastTest())
        {
            var testSessionContext = TestSessionContext.Current!;
            var allTests = testSessionContext.AllTests;
            foreach (var testEndEventsObject in allTests.SelectMany(tc => tc.GetLastTestInTestSessionEventObjects()))
            {
                await logger.LogDebugAsync("Executing ILastTestInTestSessionEventReceivers");

                await RunHelpers.RunValueTaskSafelyAsync(
                    () => testEndEventsObject.OnLastTestInTestSession(testSessionContext, testContext),
                    cleanUpExceptions);
            }
        }
    }

    private SemaphoreSlim? WaitForParallelLimiter(DiscoveredTest test, bool isStartedAsDependencyForAnotherTest)
    {
        if (test.TestDetails.ParallelLimit is { } parallelLimit && !isStartedAsDependencyForAnotherTest)
        {
            return parallelLimitLockProvider.GetLock(parallelLimit);
        }

        return null;
    }

    private async ValueTask ExecuteWithRetries(DiscoveredTest discoveredTest, List<Exception> cleanupExceptions)
    {
        var retryCount = discoveredTest.TestDetails.RetryLimit;

        discoveredTest.TestContext.TestStart = DateTimeOffset.Now;

        // +1 for the original non-retry
        for (var i = 0; i < retryCount + 1; i++)
        {
            try
            {
                await ExecuteWithCancellationTokens(discoveredTest, cleanupExceptions);
                break;
            }
            catch (Exception e)
            {
                if (i == retryCount
                    || Debugger.IsAttached
                    || !await ShouldRetry(discoveredTest.TestContext, e, i + 1))
                {
                    throw;
                }

                // Clear context for a new run
                cleanupExceptions.Clear();
                discoveredTest.TestContext.Result = null;

                await logger.LogWarningAsync($"""
                                              {discoveredTest.TestContext.GetClassTypeName()}.{discoveredTest.TestContext.GetTestDisplayName()} attempt {i + 1} failed, retrying...");
                                              Error was {e.GetType().Name}: {e.Message}
                                             """);

                await discoveredTest.ResetTestInstance();

                foreach (var testRetryEventReceiver in discoveredTest.TestContext.GetTestRetryEventObjects())
                {
                    await testRetryEventReceiver.OnTestRetry(discoveredTest.TestContext, i + 1);
                }

                discoveredTest.TestContext.CurrentRetryAttempt++;
            }
        }
    }

    private async ValueTask<bool> ShouldRetry(TestContext context, Exception e, int currentRetryCount)
    {
        try
        {
            var retryLogic = context.TestDetails.RetryLogic;

            if (retryLogic == null)
            {
                return false;
            }

            return await retryLogic(context, e, currentRetryCount);
        }
        catch (Exception exception)
        {
            await logger.LogErrorAsync(exception);
            return false;
        }
    }

    private async ValueTask ExecuteWithCancellationTokens(DiscoveredTest discoveredTest, List<Exception> cleanupExceptions)
    {
        var cancellationTokens = discoveredTest.TestContext.LinkedCancellationTokens;

        if (cancellationTokens.Count == 0)
        {
            await ExecuteTestMethodWithTimeout(discoveredTest, engineCancellationToken.Token, cleanupExceptions);
            return;
        }

        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource([engineCancellationToken.Token, .. cancellationTokens]);
        await ExecuteTestMethodWithTimeout(discoveredTest, linkedTokenSource.Token, cleanupExceptions);
    }

    private async Task ExecuteTestMethodWithTimeout(DiscoveredTest discoveredTest, CancellationToken cancellationToken, List<Exception> cleanupExceptions)
    {
        var timeout = discoveredTest.TestDetails.Timeout;

        if (timeout == null || timeout.Value == TimeSpan.Zero)
        {
            await RunTest(discoveredTest, cancellationToken, cleanupExceptions);
            return;
        }

        await RunHelpers.RunWithTimeoutAsync(token => RunTest(discoveredTest, token, cleanupExceptions), timeout.Value, cancellationToken);
    }

    private Task RunTest(DiscoveredTest discoveredTest, CancellationToken cancellationToken, List<Exception> cleanupExceptions)
    {
        return Task.Run(() => testInvoker.Invoke(discoveredTest, cancellationToken, cleanupExceptions), cancellationToken);
    }

    private async ValueTask WaitForDependencies(DiscoveredTest test, ITestExecutionFilter? filter,
        ExecuteRequestContext context)
    {
        // Reverse so most nested dependencies resolve first
        for (var index = test.Dependencies.Length - 1; index >= 0; index--)
        {
            var dependency = test.Dependencies[index];
            try
            {
                await ExecuteTestAsync(dependency.Test, filter, context, true);
            }
            catch (Exception e) when (dependency.ProceedOnFailure)
            {
                test.TestContext.OutputWriter.WriteLine(
                    $"A dependency has failed: {dependency.Test.TestDetails.TestName}", e);
            }
            catch (Exception e)
            {
                throw new InconclusiveTestException($"A dependency has failed: {dependency.Test.TestDetails.TestName}",
                    e);
            }
        }
    }

    public Task<bool> IsEnabledAsync()
    {
        return extension.IsEnabledAsync();
    }

    public string Uid => extension.Uid;

    public string Version => extension.Version;

    public string DisplayName => extension.DisplayName;

    public string Description => extension.Description;

    public Type[] DataTypesProduced { get; } =
    [
        typeof(TestNodeUpdateMessage),
        typeof(TestNodeFileArtifact)
    ];
}