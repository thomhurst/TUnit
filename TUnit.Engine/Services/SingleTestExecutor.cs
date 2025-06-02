﻿using System.Diagnostics;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Exceptions;
using TUnit.Core.Extensions;
using TUnit.Core.Logging;
using TUnit.Engine.Capabilities;
using TUnit.Engine.Exceptions;
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
    TestRegistrar testRegistrar,
    StopExecutionCapability stopExecutionCapability)
    : IDataProducer
{
    private static readonly Lock Lock = new();
    private static readonly SemaphoreSlim AssemblyEventsLock = new(1, 1);
    private static readonly SemaphoreSlim ClassEventsLock = new(1, 1);
    private static readonly SemaphoreSlim SessionEventsLock = new(1, 1);
    
    public Task ExecuteTestAsync(DiscoveredTest test, ITestExecutionFilter? filter,
        bool isStartedAsDependencyForAnotherTest)
    {
        lock (Lock)
        {
            return test.TestContext.TestTask ??= Task.Run(async () => await ExecuteTestInternalAsync(test, filter, isStartedAsDependencyForAnotherTest));
        }
    }

    private async ValueTask ExecuteTestInternalAsync(DiscoveredTest test, ITestExecutionFilter? filter,
        bool isStartedAsDependencyForAnotherTest)
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

            if (stopExecutionCapability.IsStopRequested || engineCancellationToken.Token.IsCancellationRequested)
            {
                throw new TestRunCanceledException();
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
                
                await WaitForDependencies(test, filter);

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
                
                TestContext.Current = testContext;
                
                await RunFirstTestEventReceivers(testContext);
                
                await ExecuteWithRetries(test, cleanUpExceptions);

                ExceptionsHelper.ThrowIfAny(cleanUpExceptions);
            }
            catch (SkipTestException skipTestException)
            {
                await logger.LogInformationAsync($"Skipping {testContext.GetClassTypeName()}.{testContext.GetTestDisplayName()}...");
                
                testContext.SetResult(skipTestException);
            }
            catch (Exception e)
            {
                if (testContext.Result is null
                    || testContext.Result?.IsOverridden is false
                    || testContext.Result?.Status is Status.Failed or Status.Cancelled)
                {
                    await logger.LogDebugAsync($"Error in test {testContext.TestDetails.TestClass.Type.FullName}.{testContext.GetTestDisplayName()}: {e}");
                    testContext.SetResult(e);
                    throw;
                }
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
                Status.Cancelled => messageBus.Cancelled(test.TestContext, start.GetValueOrDefault()),
                Status.Skipped => messageBus.Skipped(test.TestContext, test.TestContext.SkipReason!),
                _ => default,
            };

            await task;
        }
    }

    private async Task RunFirstTestEventReceivers(TestContext testContext)
    {
        await RunFirstTestInSessionEventReceivers(testContext);

        await RunFirstTestInAssemblyEventReceivers(testContext);
        
        await RunFirstTestInClassEventReceivers(testContext);
    }

    private async Task RunFirstTestInSessionEventReceivers(TestContext testContext)
    {
        var testSessionContext = testContext.AssemblyContext.TestSessionContext;

        if (testSessionContext.FirstTestStarted)
        {
            return;
        }
        
        await SessionEventsLock.WaitAsync();
        
        try
        {
            if (testSessionContext.FirstTestStarted)
            {
                return;
            }
            
            foreach (var firstTestInAssemblyEventReceiver in testContext.GetFirstTestInTestSessionEventObjects())
            {
                testContext.RestoreExecutionContext();
                await firstTestInAssemblyEventReceiver.OnFirstTestInTestSession(testSessionContext, testContext);
            }
        }
        finally
        {
            testSessionContext.FirstTestStarted = true;
            SessionEventsLock.Release();
        }
    }
    
    private async Task RunFirstTestInAssemblyEventReceivers(TestContext testContext)
    {
        var assemblyHookContext = testContext.AssemblyContext;

        if (assemblyHookContext.FirstTestStarted)
        {
            return;
        }
        
        await AssemblyEventsLock.WaitAsync();
        
        try
        {
            if (assemblyHookContext.FirstTestStarted)
            {
                return;
            }
            
            foreach (var firstTestInAssemblyEventReceiver in testContext.GetFirstTestInAssemblyEventObjects())
            {
                testContext.RestoreExecutionContext();

                await firstTestInAssemblyEventReceiver.OnFirstTestInAssembly(assemblyHookContext, testContext);
            }
        }
        finally
        {
            assemblyHookContext.FirstTestStarted = true;
            AssemblyEventsLock.Release();
        }
    }
    
    private async Task RunFirstTestInClassEventReceivers(TestContext testContext)
    {
        var classHookContext = testContext.ClassContext;

        if (classHookContext.FirstTestStarted)
        {
            return;
        }
        
        await ClassEventsLock.WaitAsync();
        
        try
        {
            if (classHookContext.FirstTestStarted)
            {
                return;
            }
            
            foreach (var firstTestInAssemblyEventReceiver in testContext.GetFirstTestInClassEventObjects())
            {
                testContext.RestoreExecutionContext();
                await firstTestInAssemblyEventReceiver.OnFirstTestInClass(classHookContext, testContext);
            }
        }
        finally
        {
            classHookContext.FirstTestStarted = true;
            ClassEventsLock.Release();
        }
    }

    private ValueTask RegisterIfNotAlready(TestContext testContext)
    {
        lock (Lock)
        {
            // Could not be registered if wasn't in the original filter and it's triggered from a [DependsOn]
            if (!testContext.IsRegistered)
            {
                return testRegistrar.RegisterInstance(testContext.InternalDiscoveredTest,
                    _ => default);
            }
            
            return default;
        }
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
                    await RunHelpers.RunValueTaskSafelyAsync(() => testSkippedEventReceiver.OnTestSkipped(testContext),
                        cleanUpExceptions);
                }
            }

            TestContext.Current = null;

            await ExecuteStaticAfterHooks(test, testContext, cleanUpExceptions);

            ExceptionsHelper.ThrowIfAny(cleanUpExceptions);
        }
        catch (Exception e)
        {
            testContext.SetResult(e);
            throw;
        }
    }

    private async ValueTask ExecuteStaticAfterHooks(DiscoveredTest test, TestContext testContext,
        List<Exception> cleanUpExceptions)
    {
        var afterClassHooks = classHookOrchestrator.CollectAfterHooks(testContext, test.TestContext.TestDetails.TestClass.Type);
        var classHookContext = test.TestContext.ClassContext;
                
        ClassHookContext.Current = classHookContext;
                
        foreach (var afterHook in afterClassHooks)
        {
            await RunHelpers.RunValueTaskSafelyAsync(() =>
            {
                try
                {
                    return afterHook.ExecuteAsync(classHookContext, CancellationToken.None);
                }
                catch (Exception e)
                {
                    throw new HookFailedException($"Error executing [After(Class)] hook: {afterHook.MethodInfo.Type.FullName}.{afterHook.Name}", e);
                }
            }, cleanUpExceptions);
        }
                
        ClassHookContext.Current = null;
        
        var afterAssemblyHooks = assemblyHookOrchestrator.CollectAfterHooks(testContext, test.TestContext.TestDetails.TestClass.Type.Assembly);
        var assemblyHookContext = test.TestContext.AssemblyContext;

        AssemblyHookContext.Current = assemblyHookContext;
                
        foreach (var afterHook in afterAssemblyHooks)
        {
            await RunHelpers.RunValueTaskSafelyAsync(() =>
            {
                try
                {
                    return afterHook.ExecuteAsync(assemblyHookContext, CancellationToken.None);
                }
                catch (Exception e)
                {
                    throw new HookFailedException($"Error executing [After(Assembly)] hook: {afterHook.MethodInfo.Type.FullName}.{afterHook.Name}", e);
                }
            }, cleanUpExceptions);
        }
                
        AssemblyHookContext.Current = null;
        
        if (instanceTracker.IsLastTest())
        {
            var testSessionContext = assemblyHookContext.TestSessionContext!;

            foreach (var testEndEventsObject in testContext.GetLastTestInTestSessionEventObjects())
            {
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

    private async ValueTask ExecuteTestMethodWithTimeout(DiscoveredTest discoveredTest, CancellationToken cancellationToken, List<Exception> cleanupExceptions)
    {
        var timeout = discoveredTest.TestDetails.Timeout;
        
        await RunHelpers.RunWithTimeoutAsync(token => RunTest(discoveredTest, token, cleanupExceptions), timeout, cancellationToken);
    }

    private Task RunTest(DiscoveredTest discoveredTest, CancellationToken cancellationToken, List<Exception> cleanupExceptions)
    {
        return Task.Run(() => testInvoker.Invoke(discoveredTest, cancellationToken, cleanupExceptions), cancellationToken);
    }

    private async ValueTask WaitForDependencies(DiscoveredTest test, ITestExecutionFilter? filter)
    {
        var dependencies = CollectDependencyChain(test).ToArray();
        
        // Reverse so most nested dependencies resolve first
        for (var index = dependencies.Length - 1; index >= 0; index--)
        {
            var dependency = dependencies[index];
            try
            {
                await ExecuteTestAsync(dependency.Test, filter, true);
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

    private static IEnumerable<Dependency> CollectDependencyChain(DiscoveredTest test)
    {
        foreach (var testDependency in test.Dependencies)
        {
            yield return testDependency;
        }

        foreach (var testDependency in test.Dependencies)
        {
            foreach (var dependency in CollectDependencyChain(testDependency.Test))
            {
                yield return dependency;
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
    ];
}