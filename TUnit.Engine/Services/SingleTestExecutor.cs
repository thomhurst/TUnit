using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Engine.Extensions;
using TUnit.Engine.Helpers;
using TUnit.Engine.Hooks;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;
using TimeoutException = TUnit.Core.Exceptions.TimeoutException;
#pragma warning disable TPEXP

namespace TUnit.Engine.Services;

internal class SingleTestExecutor : IDataProducer
{
    private readonly IExtension _extension;
    private readonly Disposer _disposer;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly TestInvoker _testInvoker;
    private readonly ExplicitFilterService _explicitFilterService;
    private readonly ParallelLimitProvider _parallelLimitProvider;
    private readonly AssemblyHookOrchestrator _assemblyHookOrchestrator;
    private readonly ClassHookOrchestrator _classHookOrchestrator;
    private readonly TUnitLogger _logger;
    
    public SingleTestExecutor(
        IExtension extension,
        Disposer disposer,
        CancellationTokenSource cancellationTokenSource,
        TestInvoker testInvoker,
        ExplicitFilterService explicitFilterService,
        ParallelLimitProvider parallelLimitProvider,
        AssemblyHookOrchestrator assemblyHookOrchestrator,
        ClassHookOrchestrator classHookOrchestrator,
        TUnitLogger logger)
    {
        _extension = extension;
        _disposer = disposer;
        _cancellationTokenSource = cancellationTokenSource;
        _testInvoker = testInvoker;
        _explicitFilterService = explicitFilterService;
        _parallelLimitProvider = parallelLimitProvider;
        _assemblyHookOrchestrator = assemblyHookOrchestrator;
        _classHookOrchestrator = classHookOrchestrator;
        _logger = logger;
    }

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

            if (_cancellationTokenSource.IsCancellationRequested)
            {
                await context.MessageBus.PublishAsync(this,
                    new TestNodeUpdateMessage(context.Request.Session.SessionUid,
                        test.ToTestNode().WithProperty(new CancelledTestNodeStateProperty())));
                testContext.TaskCompletionSource.SetCanceled();
                return;
            }

            await context.MessageBus.PublishAsync(this,
                new TestNodeUpdateMessage(context.Request.Session.SessionUid,
                    test.ToTestNode().WithProperty(InProgressTestNodeStateProperty.CachedInstance)));

            var cleanUpExceptions = new List<Exception>();

            DateTimeOffset start = DateTimeOffset.Now;

            try
            {
                await WaitForDependsOnTests(test, filter, context);

                if (!_explicitFilterService.CanRun(test.TestDetails, filter))
                {
                    throw new SkipTestException("Test with ExplicitAttribute was not explicitly run.");
                }

                start = DateTimeOffset.Now;

                try
                {
                    await ExecuteBeforeHooks(test, context, testContext);

                    TestContext.Current = testContext;

                    foreach (var beforeTestAttribute in test.BeforeTestAttributes)
                    {
                        await Timings.Record($"{beforeTestAttribute.GetType().Name}.OnBeforeTest", testContext,
                            () => beforeTestAttribute.OnBeforeTest(
                                new BeforeTestContext(testContext.InternalDiscoveredTest)));
                    }

                    await ExecuteWithRetries(test);
                }
                finally
                {
                    await DecrementSharedData(test);

                    foreach (var afterTestAttribute in test.AfterTestAttributes)
                    {
                        await Timings.Record($"{afterTestAttribute.GetType().Name}.OnAfterTest", testContext,
                            () => RunHelpers.RunSafelyAsync(() => afterTestAttribute.OnAfterTest(testContext),
                                cleanUpExceptions));
                    }

                    await DisposeTest(testContext, cleanUpExceptions);

                    await RunHelpers.RunSafelyAsync(
                        () => test.ClassConstructor?.DisposeAsync(test.TestDetails.ClassInstance) ?? Task.CompletedTask,
                        cleanUpExceptions);

                    TestContext.Current = null;

                    await ExecuteAfterHooks(test, context, testContext, cleanUpExceptions);

                    foreach (var artifact in testContext.Artifacts)
                    {
                        await context.MessageBus.PublishAsync(this,
                            new TestNodeFileArtifact(context.Request.Session.SessionUid, test.ToTestNode(),
                                artifact.File,
                                artifact.DisplayName, artifact.Description));
                    }
                }

                ExceptionsHelper.ThrowIfAny(cleanUpExceptions);

                var timingProperty = GetTimingProperty(testContext, start);

                await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
                    context.Request.Session.SessionUid,
                    test.ToTestNode()
                        .WithProperty(PassedTestNodeStateProperty.CachedInstance)
                        .WithProperty(new StandardOutputProperty(testContext.GetStandardOutput()))
                        .WithProperty(new StandardErrorProperty(testContext.GetErrorOutput()))
                        .WithProperty(timingProperty)
                ));

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

                await _logger.LogInformationAsync($"Skipping {test.TestDetails.DisplayName}...");

                await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
                    context.Request.Session.SessionUid,
                    test.ToTestNode()
                        .WithProperty(new SkippedTestNodeStateProperty(skipTestException.Reason))
                        .WithProperty(new StandardOutputProperty(testContext.GetStandardOutput()))
                        .WithProperty(new StandardErrorProperty(testContext.GetErrorOutput()))
                ));

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

                await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
                    context.Request.Session.SessionUid, test.ToTestNode()
                        .WithProperty(GetFailureStateProperty(testContext, e, timingProperty.GlobalTiming.Duration))
                        .WithProperty(timingProperty)
                        .WithProperty(new StandardOutputProperty(testContext.GetStandardOutput()))
                        .WithProperty(new StandardErrorProperty(testContext.GetErrorOutput()))
                        .WithProperty(new TrxExceptionProperty(e.Message, e.StackTrace))));

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
            if (semaphore != null)
            {
                semaphore.Dispose();
            }
        }
    }

    private async Task ExecuteBeforeHooks(DiscoveredTest test, ExecuteRequestContext context, TestContext testContext)
    {
        try
        {
            await _assemblyHookOrchestrator.ExecuteBeforeHooks(context,
                test.TestContext.TestDetails.ClassType.Assembly,
                testContext);

            await _classHookOrchestrator.ExecuteBeforeHooks(context, test.TestContext.TestDetails.ClassType);
        }
        catch (Exception e)
        {
            throw new SkipTestException($"Skipped due to failing Before Hook {e.GetType().Name}: {e.Message}");
        }
    }

    private async Task ExecuteAfterHooks(DiscoveredTest test, ExecuteRequestContext context, TestContext testContext,
        List<Exception> cleanUpExceptions)
    {
        try
        {
            await _classHookOrchestrator.ExecuteCleanUpsIfLastInstance(context,
                test.TestContext.TestDetails.ClassType, cleanUpExceptions);

            await _assemblyHookOrchestrator.ExecuteCleanups(context,
                test.TestContext.TestDetails.ClassType.Assembly, testContext, cleanUpExceptions);
        }
        catch
        {
            // Ignored - Will be counted as its own test failure - We don't need to bind it to this test
        }
    }

    private async Task DisposeTest(TestContext testContext, List<Exception> cleanUpExceptions)
    {
        await TestHookOrchestrator.ExecuteAfterHooks(testContext.TestDetails.ClassInstance!, testContext.InternalDiscoveredTest, cleanUpExceptions);
            
        await RunHelpers.RunValueTaskSafelyAsync(() => _disposer.DisposeAsync(testContext.TestDetails.ClassInstance), cleanUpExceptions);
        
        await _consoleStandardOutLock.WaitAsync();

        try
        {
            await Dispose(testContext);
        }
        finally
        {
            _consoleStandardOutLock.Release();
        }
    }

    private SemaphoreSlim? WaitForParallelLimiter(DiscoveredTest test, bool isStartedAsDependencyForAnotherTest)
    {
        if (test.TestDetails.ParallelLimit is { } parallelLimit && !isStartedAsDependencyForAnotherTest)
        {
            return _parallelLimitProvider.GetLock(parallelLimit);
        }

        return null;
    }

    private static TimingProperty GetTimingProperty(TestContext testContext, DateTimeOffset overallStart)
    {
        var end = DateTimeOffset.Now;

        lock (testContext.Timings)
        {
            var stepTimings = testContext.Timings.Select(x =>
                new StepTimingInfo(x.StepName, string.Empty, new TimingInfo(x.Start, x.End, x.Duration)));

            return new TimingProperty(new TimingInfo(overallStart, end, end - overallStart), [..stepTimings]);
        }
    }

    private static IProperty GetFailureStateProperty(TestContext testContext, Exception e, TimeSpan duration)
    {
        if (testContext.TestDetails.Timeout.HasValue
            && e is TaskCanceledException or OperationCanceledException or TimeoutException
            && duration >= testContext.TestDetails.Timeout.Value)
        {
            return new TimeoutTestNodeStateProperty(e)
            {
                Timeout = testContext.TestDetails.Timeout,
            };
        }
        
        return new FailedTestNodeStateProperty(e);
    }

    private async ValueTask Dispose(TestContext testContext)
    {
        var testInformation = testContext.TestDetails;

        foreach (var methodArgument in testInformation.InternalTestMethodArguments)
        {
            await DisposeInjectedData(methodArgument);
        }
        
        foreach (var classArgument in testInformation.InternalTestClassArguments)
        {
            await DisposeInjectedData(classArgument);
        }

        await _disposer.DisposeAsync(testContext);
    }

    private async ValueTask DisposeInjectedData(TestData? testData)
    {
        if (testData?.InjectedDataType 
            is InjectedDataType.SharedGlobally 
            or InjectedDataType.SharedByKey
            or InjectedDataType.SharedByTestClassType)
        {
            // Handled later - Might be shared with other tests too so we can't just dispose it without checking
            return;
        }

        if (testData?.DisposeAfterTest == true)
        {
            await _disposer.DisposeAsync(testData);
        }
    }
    
    private static async ValueTask DecrementSharedData(DiscoveredTest discoveredTest)
    {
        foreach (var methodArgument in discoveredTest.TestContext.TestDetails.InternalTestMethodArguments)
        {
            if (methodArgument.InjectedDataType == InjectedDataType.SharedByKey)
            {
                await TestDataContainer.ConsumeKey(methodArgument.StringKey!, methodArgument.Type);
            }
            
            if (methodArgument.InjectedDataType == InjectedDataType.SharedGlobally)
            {
                await TestDataContainer.ConsumeGlobalCount(methodArgument.Type);
            }
        }
        
        foreach (var classArgument in discoveredTest.TestContext.TestDetails.InternalTestClassArguments)
        {
            if (classArgument.InjectedDataType == InjectedDataType.SharedByKey)
            {
                await TestDataContainer.ConsumeKey(classArgument.StringKey!, classArgument.Type);
            }
            
            if (classArgument.InjectedDataType == InjectedDataType.SharedGlobally)
            {
                await TestDataContainer.ConsumeGlobalCount(classArgument.Type);
            }
        }
    }

    private Task RunTest(DiscoveredTest discoveredTest, CancellationToken cancellationToken)
    {
        return _testInvoker.Invoke(discoveredTest, cancellationToken);
    }

    private readonly SemaphoreSlim _consoleStandardOutLock = new(1, 1);

    private async ValueTask ExecuteWithRetries(DiscoveredTest discoveredTest)
    {
        var testInformation = discoveredTest.TestContext.TestDetails;
        var retryCount = testInformation.RetryLimit;
        
        discoveredTest.TestContext.TestStart = DateTimeOffset.Now;
        
        // +1 for the original non-retry
        for (var i = 0; i < retryCount + 1; i++)
        {
            try
            {
                await ExecuteCore(discoveredTest);
                break;
            }
            catch (Exception e)
            {
                if (i == retryCount 
                    || !await ShouldRetry(discoveredTest.TestContext, e, i + 1))
                {
                    throw;
                }

                await _logger.LogWarningAsync($"{testInformation.TestName} failed, retrying... (attempt {i + 1})");
                await discoveredTest.ResetTestInstance();
                discoveredTest.TestContext.CurrentRetryAttempt++;
            }
        }
    }

    private async ValueTask<bool> ShouldRetry(TestContext context, Exception e, int currentRetryCount)
    {
        try
        {
            var retryAttribute = context.TestDetails.RetryAttribute;

            if (retryAttribute == null)
            {
                return false;
            }

            return await retryAttribute.ShouldRetry(context, e, currentRetryCount);
        }
        catch (Exception exception)
        {
            await _logger.LogErrorAsync(exception);
            return false;
        }
    }

    private async ValueTask ExecuteCore(DiscoveredTest discoveredTest)
    {
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            return;
        }
        
        await ExecuteTestMethodWithTimeout(discoveredTest);
    }

    private async ValueTask WaitForDependsOnTests(DiscoveredTest testContext, ITestExecutionFilter? filter,
        ExecuteRequestContext context)
    {
        foreach (var dependency in GetDependencies(testContext.TestDetails))
        {
            AssertDoNotDependOnEachOther(testContext.TestContext, dependency.TestContext);
            
            try
            {
                await ExecuteTestAsync(dependency, filter, context, true);
            }
            catch (Exception e)
            {
                throw new InconclusiveTestException($"A dependency has failed: {dependency.TestDetails.TestName}", e);
            }
        }
    }

    private IEnumerable<DiscoveredTest> GetDependencies(TestDetails testDetails)
    {
        return GetDependencies(testDetails, testDetails);
    }

    private IEnumerable<DiscoveredTest> GetDependencies(TestDetails original, TestDetails testDetails)
    {
        foreach (var dependency in testDetails.Attributes
                     .OfType<DependsOnAttribute>()
                     .SelectMany(dependsOnAttribute => TestDictionary.GetTestsByNameAndParameters(dependsOnAttribute.TestName,
                         dependsOnAttribute.ParameterTypes, testDetails.ClassType,
                         testDetails.TestClassParameterTypes)))
        {
            yield return dependency;

            if (dependency.TestDetails.IsSameTest(original))
            {
                yield break;
            }
            
            foreach (var nestedDependency in GetDependencies(original, dependency.TestDetails))
            {
                yield return nestedDependency;
                
                if (nestedDependency.TestDetails.IsSameTest(original))
                {
                    yield break;
                }
            }
        }
    }

    private void AssertDoNotDependOnEachOther(TestContext testContext, TestContext dependency)
    {
        TestContext[] dependencies = [dependency, ..GetDependencies(dependency.TestDetails).Select(x => x.TestContext)];
        
        foreach (var dependencyOfDependency in dependencies)
        {
            if (dependencyOfDependency.TestDetails.IsSameTest(testContext.TestDetails))
            {
                throw new DependencyConflictException(testContext.TestDetails, dependencies.Select(x => x.TestDetails));
            }

            if (dependencyOfDependency.TestDetails.NotInParallelConstraintKeys != null)
            {
                throw new DependsOnNotInParallelException(testContext.TestDetails.TestName);
            }
        }
    }

    private async Task ExecuteTestMethodWithTimeout(DiscoveredTest discoveredTest)
    {
        var testDetails = discoveredTest.TestDetails;
        
        if (testDetails.Timeout == null || testDetails.Timeout.Value == default)
        {
            await RunTest(discoveredTest, EngineCancellationToken.Token);
            return;
        }

        await RunHelpers.RunWithTimeoutAsync(token => RunTest(discoveredTest, token), testDetails.Timeout);
    }


    public Task<bool> IsEnabledAsync()
    {
        return _extension.IsEnabledAsync();
    }

    public string Uid => _extension.Uid;

    public string Version => _extension.Version;

    public string DisplayName => _extension.DisplayName;

    public string Description => _extension.Description;

    public Type[] DataTypesProduced { get; } =
    [
        typeof(TestNodeUpdateMessage),
        typeof(TestNodeFileArtifact)
    ];
}