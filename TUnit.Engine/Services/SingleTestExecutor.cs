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
using TUnit.Engine.Helpers;
using TUnit.Engine.Hooks;
using TUnit.Engine.Logging;

#pragma warning disable TPEXP

namespace TUnit.Engine.Services;

internal class SingleTestExecutor(
    IExtension extension,
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
        lock (test.TestContext.Lock)
        {
            return test.TestContext.TestTask ??= ExecuteTestInternalAsync(test, filter, context, isStartedAsDependencyForAnotherTest);
        }
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

            if (engineCancellationToken.Token.IsCancellationRequested)
            {
                await messageBus.Cancelled(testContext);

                engineCancellationToken.Token.ThrowIfCancellationRequested();
            }

            await messageBus.InProgress(testContext);

            var cleanUpExceptions = new List<Exception>();

            var start = DateTimeOffset.Now;

            try
            {
                await WaitForDependsOnTests(test, filter, context);

                start = DateTimeOffset.Now;

                if (!explicitFilterService.CanRun(test.TestDetails, filter))
                {
                    throw new SkipTestException("Test with ExplicitAttribute was not explicitly run.");
                }

                if (testContext.SkipReason != null)
                {
                    throw new SkipTestException(testContext.SkipReason);
                }

                if (engineCancellationToken.Token.IsCancellationRequested)
                {
                    throw new SkipTestException("The test session has been cancelled...");
                }

                await ExecuteStaticBeforeHooks(test);

                TestContext.Current = testContext;

                await ExecuteOnTestStartEvents(testContext);

                await ExecuteWithRetries(test, cleanUpExceptions);

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
            }
            catch (SkipTestException skipTestException)
            {
                var timingProperty = GetTimingProperty(testContext, start);

                await logger.LogInformationAsync($"Skipping {testContext.GetClassTypeName()}.{testContext.GetTestDisplayName()}...");

                await messageBus.Skipped(testContext, skipTestException.Reason);

                testContext.Result = new TestResult
                {
                    Duration = timingProperty.GlobalTiming.Duration,
                    Start = timingProperty.GlobalTiming.StartTime,
                    End = timingProperty.GlobalTiming.EndTime,
                    ComputerName = Environment.MachineName,
                    Exception = null,
                    Status = Status.Skipped,
                    Output = $"{testContext.GetErrorOutput()}{Environment.NewLine}{testContext.GetStandardOutput()}"
                };
            }
            catch (Exception e)
            {
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
                }
                else
                {
                    foreach (var testSkippedEventReceiver in testContext.GetTestSkippedEventObjects())
                    {
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
            }
        }
        finally
        {
            semaphore?.Release();
        }
    }

    private async ValueTask ExecuteOnTestStartEvents(TestContext testContext)
    {
        foreach (var testStartEventsObject in testContext.GetTestStartEventObjects())
        {
            await testStartEventsObject.OnTestStart(new BeforeTestContext(testContext.InternalDiscoveredTest));
        }
    }

    private async Task ExecuteStaticBeforeHooks(DiscoveredTest test)
    {
        await assemblyHookOrchestrator.ExecuteBeforeHooks(test.TestContext.TestDetails.ClassType.Assembly);

        await classHookOrchestrator.ExecuteBeforeHooks(test.TestContext.TestDetails.ClassType);
    }

    private async Task ExecuteStaticAfterHooks(DiscoveredTest test, TestContext testContext,
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

            return new TimingProperty(new TimingInfo(overallStart, end, end - overallStart), [.. stepTimings]);
        }
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

        if (timeout == null || timeout.Value == default)
        {
            await RunTest(discoveredTest, cancellationToken, cleanupExceptions);
            return;
        }

        await RunHelpers.RunWithTimeoutAsync(token => RunTest(discoveredTest, token, cleanupExceptions), timeout.Value, cancellationToken);
    }

    private Task RunTest(DiscoveredTest discoveredTest, CancellationToken cancellationToken, List<Exception> cleanupExceptions)
    {
        return testInvoker.Invoke(discoveredTest, cancellationToken, cleanupExceptions);
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