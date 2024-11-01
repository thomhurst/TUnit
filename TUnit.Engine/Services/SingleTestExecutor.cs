using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Exceptions;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;
using TUnit.Engine.Extensions;
using TUnit.Engine.Helpers;
using TUnit.Engine.Hooks;
using TUnit.Engine.Logging;

#pragma warning disable TPEXP

namespace TUnit.Engine.Services;

internal class SingleTestExecutor(
    IExtension extension,
    CancellationTokenSource cancellationTokenSource,
    InstanceTracker instanceTracker, 
    TestInvoker testInvoker,
    ExplicitFilterService explicitFilterService,
    ParallelLimitLockProvider parallelLimitLockProvider,
    AssemblyHookOrchestrator assemblyHookOrchestrator,
    ClassHookOrchestrator classHookOrchestrator,
    ITestFinder testFinder,
    ITUnitMessageBus messageBus,
    TUnitFrameworkLogger logger,
    EngineCancellationToken engineCancellationToken)
    : IDataProducer
{
    public Task ExecuteTestAsync(DiscoveredTest test, ITestExecutionFilter? filter, ExecuteRequestContext context,
        bool isStartedAsDependencyForAnotherTest)
    {
        lock (test)
        {
            if (test.IsStarted)
            {
                return test.TestContext.TestTask;
            }

            test.IsStarted = true;
        }
        
        return ExecuteTestInternalAsync(test, filter, context, isStartedAsDependencyForAnotherTest);
    }

    private async Task ExecuteTestInternalAsync(DiscoveredTest test, ITestExecutionFilter? filter, ExecuteRequestContext context, bool isStartedAsDependencyForAnotherTest)
    {
        var semaphore = WaitForParallelLimiter(test, isStartedAsDependencyForAnotherTest);

        if (semaphore != null)
        {
            await semaphore.WaitAsync();
        }
        
        try
        {
            var testContext = test.TestContext;
            var timings = testContext.Timings;

            if (cancellationTokenSource.IsCancellationRequested)
            {
                await messageBus.Cancelled(testContext);
                
                testContext.TaskCompletionSource.SetCanceled();
                return;
            }

            await messageBus.InProgress(testContext);

            var cleanUpExceptions = new List<Exception>();

            DateTimeOffset start = DateTimeOffset.Now;

            try
            {
                await WaitForDependsOnTests(test, filter, context);

                if (!explicitFilterService.CanRun(test.TestDetails, filter))
                {
                    throw new SkipTestException("Test with ExplicitAttribute was not explicitly run.");
                }

                if (testContext.SkipReason != null)
                {
                    throw new SkipTestException(testContext.SkipReason);
                }
                
                start = DateTimeOffset.Now;
                
                await ExecuteTest(test, context, testContext, cleanUpExceptions);

                ExceptionsHelper.ThrowIfAny(cleanUpExceptions);

                var timingProperty = GetTimingProperty(testContext, start);
                
                await messageBus.Passed(testContext, start);

                testContext.Result = new TestResult
                {
                    TestContext = testContext,
                    Duration = timingProperty.GlobalTiming.Duration,
                    Start = timingProperty.GlobalTiming.StartTime,
                    End = timingProperty.GlobalTiming.EndTime,
                    ComputerName = Environment.MachineName,
                    Exception = null,
                    Status = Status.Passed,
                    Output = testContext.GetStandardOutput()
                };

                testContext.TaskCompletionSource.SetResult(null);
            }
            catch (SkipTestException skipTestException)
            {
                testContext.TaskCompletionSource.SetException(skipTestException);

                await logger.LogInformationAsync($"Skipping {testContext.GetClassTypeName()}.{testContext.GetTestDisplayName()}...");

                await messageBus.Skipped(testContext, skipTestException.Reason);

                var now = DateTimeOffset.Now;

                testContext.Result = new TestResult
                {
                    Duration = TimeSpan.Zero,
                    Start = timings.MinBy(x => x.Start)?.Start ?? now,
                    End = timings.MinBy(x => x.End)?.End ?? timings.MinBy(x => x.Start)?.Start ?? now,
                    ComputerName = Environment.MachineName,
                    Exception = null,
                    Status = Status.Skipped,
                };
            }
            catch (Exception e)
            {
                testContext.TaskCompletionSource.SetException(e);

                var timingProperty = GetTimingProperty(testContext, start);

                await messageBus.Failed(testContext, e, start);

                testContext.Result = new TestResult
                {
                    Duration = timingProperty.GlobalTiming.Duration,
                    Start = timingProperty.GlobalTiming.StartTime,
                    End = timingProperty.GlobalTiming.EndTime,
                    ComputerName = Environment.MachineName,
                    Exception = e,
                    Status = Status.Failed,
                    Output = $"{testContext.GetErrorOutput()}{Environment.NewLine}{testContext.GetStandardOutput()}"
                };
            }

            // Will only set if not already set - Last resort incase something weird happened
            testContext.TaskCompletionSource.TrySetException(
                new Exception("Unknown error setting TaskCompletionSource"));
        }
        finally
        {
            semaphore?.Release();
        }
    }

    private async Task ExecuteTest(DiscoveredTest test, ExecuteRequestContext context, TestContext testContext,
        List<Exception> cleanUpExceptions)
    {
        var start = DateTimeOffset.Now;

        try
        {
            await ExecuteStaticBeforeHooks(test, context, testContext);

            TestContext.Current = testContext;
            
            await ExecuteOnTestStartEvents(testContext);

            await ExecuteWithRetries(test, cleanUpExceptions);

            var timingProperty = GetTimingProperty(testContext, start);

            testContext.Result = new TestResult
            {
                Duration = timingProperty.GlobalTiming.Duration,
                Start = timingProperty.GlobalTiming.StartTime,
                End = timingProperty.GlobalTiming.EndTime,
                ComputerName = Environment.MachineName,
                Exception = null,
                Status = Status.Passed,
                Output = $"{testContext.GetErrorOutput()}{Environment.NewLine}{testContext.GetStandardOutput()}"
            };
        }
        catch (Exception e)
        {
            var timingProperty = GetTimingProperty(testContext, start);

            testContext.Result = new TestResult
            {
                Duration = timingProperty.GlobalTiming.Duration,
                Start = timingProperty.GlobalTiming.StartTime,
                End = timingProperty.GlobalTiming.EndTime,
                ComputerName = Environment.MachineName,
                Exception = e is SkipTestException ? null : e,
                Status = e is SkipTestException ? Status.Skipped : Status.Failed,
                Output = $"{testContext.GetErrorOutput()}{Environment.NewLine}{testContext.GetStandardOutput()}"
            };

            throw;
        }
        finally
        {
            if (testContext.Result?.Status != Status.Skipped)
            {
                foreach (var testEndEventsObject in testContext.GetTestEndEventObjects())
                {
                    await RunHelpers.RunValueTaskSafelyAsync(() => testEndEventsObject.OnTestEnd(testContext),
                        cleanUpExceptions);
                }

                TestContext.Current = null;

                await ExecuteStaticAfterHooks(test, context, testContext, cleanUpExceptions);

                foreach (var artifact in testContext.Artifacts)
                {
                    await messageBus.TestArtifact(testContext, artifact);
                }
            }
        }
    }

    private async ValueTask ExecuteOnTestStartEvents(TestContext testContext)
    {
        foreach (var testStartEventsObject in testContext.GetTestStartEventObjects())
        {
            await testStartEventsObject.OnTestStart(new BeforeTestContext(testContext.InternalDiscoveredTest));
        }
    }

    private async Task ExecuteStaticBeforeHooks(DiscoveredTest test, ExecuteRequestContext context, TestContext testContext)
    {
        try
        {
            await assemblyHookOrchestrator.ExecuteBeforeHooks(test.TestContext.TestDetails.ClassType.Assembly);

            await classHookOrchestrator.ExecuteBeforeHooks(test.TestContext.TestDetails.ClassType);
        }
        catch (Exception e)
        {
            throw new SkipTestException($"Skipped due to failing Before Hook {e.GetType().Name}: {e.Message}");
        }
    }

    private async Task ExecuteStaticAfterHooks(DiscoveredTest test, ExecuteRequestContext context, TestContext testContext,
        List<Exception> cleanUpExceptions)
    {
        await classHookOrchestrator.ExecuteCleanUpsIfLastInstance(testContext,
                test.TestContext.TestDetails.ClassType, cleanUpExceptions);

        await assemblyHookOrchestrator.ExecuteCleanUpsIfLastInstance(testContext,
            test.TestContext.TestDetails.ClassType.Assembly, cleanUpExceptions);

        if (instanceTracker.IsLastTest())
        {
            foreach (var testEndEventsObject in testContext.GetLastTestInTestSessionEventObjects())
            {
                await RunHelpers.RunValueTaskSafelyAsync(
                    () => testEndEventsObject.IfLastTestInTestSession(TestSessionContext.Current!, testContext),
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

    private static TimingProperty GetTimingProperty(TestContext testContext, DateTimeOffset overallStart)
    {
        var end = DateTimeOffset.Now;

        lock (testContext.Lock)
        {
            var stepTimings = testContext.Timings.Select(x =>
                new StepTimingInfo(x.StepName, string.Empty, new TimingInfo(x.Start, x.End, x.Duration)));

            return new TimingProperty(new TimingInfo(overallStart, end, end - overallStart), [..stepTimings]);
        }
    }

    private Task RunTest(DiscoveredTest discoveredTest, CancellationToken cancellationToken, List<Exception> cleanupExceptions)
    {
        return testInvoker.Invoke(discoveredTest, cancellationToken, cleanupExceptions);
    }
    
    private async ValueTask ExecuteWithRetries(DiscoveredTest discoveredTest, List<Exception> cleanupExceptions)
    {
        var testInformation = discoveredTest.TestContext.TestDetails;
        var retryCount = testInformation.RetryLimit;
        
        discoveredTest.TestContext.TestStart = DateTimeOffset.Now;
        
        // +1 for the original non-retry
        for (var i = 0; i < retryCount + 1; i++)
        {
            try
            {
                await ExecuteCore(discoveredTest, cleanupExceptions);
                break;
            }
            catch (Exception e)
            {
                if (i == retryCount 
                    || !await ShouldRetry(discoveredTest.TestContext, e, i + 1))
                {
                    throw;
                }
                
                cleanupExceptions.Clear();

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

    private async ValueTask ExecuteCore(DiscoveredTest discoveredTest, List<Exception> cleanupExceptions)
    {
        if (engineCancellationToken.Token.IsCancellationRequested)
        {
            throw new SkipTestException("The test session has been cancelled...");
        }
        
        if (cancellationTokenSource.IsCancellationRequested)
        {
            throw new SkipTestException("The test has been cancelled...");
        }

        using var linkedTokenSource = CreateLinkedToken(discoveredTest.TestContext, engineCancellationToken.CancellationTokenSource);
        
        await ExecuteTestMethodWithTimeout(discoveredTest, linkedTokenSource.Token, cleanupExceptions);
    }

    private static CancellationTokenSource CreateLinkedToken(TestContext testContext,
        CancellationTokenSource cancellationTokenSource)
    {
        return CancellationTokenSource.CreateLinkedTokenSource([cancellationTokenSource.Token, ..testContext.LinkedCancellationTokens.ToArray()]);
    }

    private async ValueTask WaitForDependsOnTests(DiscoveredTest testContext, ITestExecutionFilter? filter,
        ExecuteRequestContext context)
    {
        foreach (var dependency in GetDependencies(testContext.TestDetails))
        {
            try
            {
                await ExecuteTestAsync(dependency.Test, filter, context, true);
            }
            catch when (dependency.ProceedOnFailure)
            {
                // Ignore
            }
            catch (Exception e)
            {
                throw new InconclusiveTestException($"A dependency has failed: {dependency.Test.TestDetails.TestName}", e);
            }
        }
    }

    private (DiscoveredTest Test, bool ProceedOnFailure)[] GetDependencies(TestDetails testDetails)
    {
        return GetDependencies(testDetails, testDetails, [testDetails]).ToArray();
    }

    private IEnumerable<(DiscoveredTest Test, bool ProceedOnFailure)> GetDependencies(TestDetails original, TestDetails testDetails, List<TestDetails> currentChain)
    {
        foreach (var dependsOnAttribute in testDetails.Attributes.OfType<DependsOnAttribute>())
        {
            var dependencies = GetDependencies(testDetails, dependsOnAttribute);

            foreach (var dependency in dependencies)
            {
                currentChain.Add(dependency.TestDetails);
                
                if (dependency.TestDetails.IsSameTest(original))
                {
                    throw new DependencyConflictException(currentChain);
                }
                
                yield return (dependency.InternalDiscoveredTest, dependsOnAttribute.ProceedOnFailure);

                foreach (var nestedDependency in GetDependencies(original, dependency.TestDetails, currentChain))
                {
                    yield return nestedDependency;
                }
            }
        }
    }

    private TestContext[] GetDependencies(TestDetails testDetails, DependsOnAttribute dependsOnAttribute)
    {
        var testsForClass = testFinder.GetTests(dependsOnAttribute.TestClass ?? testDetails.ClassType);

        if (dependsOnAttribute.TestClass == null)
        {
            testsForClass = testsForClass
                .Where(x => x.TestDetails.TestClassArguments.SequenceEqual(testDetails.TestClassArguments));
        }
        
        if (dependsOnAttribute.TestName != null)
        {
            testsForClass = testsForClass.Where(x => x.TestDetails.TestName == dependsOnAttribute.TestName);
        }

        if (dependsOnAttribute.ParameterTypes != null)
        {
            testsForClass = testsForClass.Where(x =>
                x.TestDetails.TestMethodParameterTypes.SequenceEqual(dependsOnAttribute.ParameterTypes));
        }
        
        return testsForClass.ToArray();
    }

    private async Task ExecuteTestMethodWithTimeout(DiscoveredTest discoveredTest, CancellationToken cancellationToken, List<Exception> cleanupExceptions)
    {
        var testDetails = discoveredTest.TestDetails;
        
        if (testDetails.Timeout == null || testDetails.Timeout.Value == default)
        {
            await RunTest(discoveredTest, cancellationToken, cleanupExceptions);
            return;
        }

        await RunHelpers.RunWithTimeoutAsync(token => RunTest(discoveredTest, token, cleanupExceptions), testDetails.Timeout, cancellationToken);
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