﻿using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using Semaphores;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Engine.Extensions;
using TUnit.Engine.Helpers;
using TUnit.Engine.Hooks;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;
using TimeoutException = TUnit.Core.Exceptions.TimeoutException;

namespace TUnit.Engine.Services;

internal class SingleTestExecutor : IDataProducer
{
    private readonly IExtension _extension;
    private readonly Disposer _disposer;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly TestInvoker _testInvoker;
    private readonly ExplicitFilterService _explicitFilterService;
    private readonly TUnitLogger _logger;

    public SingleTestExecutor(
        IExtension extension,
        Disposer disposer,
        CancellationTokenSource cancellationTokenSource,
        TestInvoker testInvoker,
        ExplicitFilterService explicitFilterService,
        TUnitLogger logger)
    {
        _extension = extension;
        _disposer = disposer;
        _cancellationTokenSource = cancellationTokenSource;
        _testInvoker = testInvoker;
        _explicitFilterService = explicitFilterService;
        _logger = logger;
    }

    public async Task ExecuteTestAsync(DiscoveredTest test, ITestExecutionFilter? filter,
        ExecuteRequestContext context)
    {
        var testContext = test.TestContext;
        var timings = testContext.Timings;

        if (_cancellationTokenSource.IsCancellationRequested)
        {
            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, test.ToTestNode().WithProperty(new CancelledTestNodeStateProperty())));
            testContext.TaskCompletionSource.SetCanceled();
            return;
        }
        
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, test.ToTestNode().WithProperty(InProgressTestNodeStateProperty.CachedInstance)));

        var cleanUpExceptions = new List<Exception>();
        
        try
        {
            await WaitForDependsOnTests(test.TestContext);
            
            if (!_explicitFilterService.CanRun(test.TestDetails, filter))
            {
                throw new SkipTestException("Test with ExplicitAttribute was not explicitly run.");
            }
            
            foreach (var applicableTestAttribute in test.BeforeTestAttributes)
            {
                await Timings.Record($"{applicableTestAttribute.GetType().Name}.OnBeforeTest", testContext,
                    () => applicableTestAttribute.OnBeforeTest(testContext));
            }

            try
            {
                await AssemblyHookOrchestrator.ExecuteSetups(test.TestContext.TestDetails.ClassType.Assembly, testContext);
                await ClassHookOrchestrator.ExecuteSetups(test.TestContext.TestDetails.ClassType, testContext);
                await ExecuteWithRetries(test, cleanUpExceptions);
            }
            finally
            {
                await DecrementSharedData(test);
                
                foreach (var applicableTestAttribute in test.AfterTestAttributes)
                {
                    await Timings.Record($"{applicableTestAttribute.GetType().Name}.OnAfterTest", testContext,
                        () => RunHelpers.RunSafelyAsync(() => applicableTestAttribute.OnAfterTest(testContext),
                            cleanUpExceptions));
                }
                
                await ClassHookOrchestrator.ExecuteCleanUpsIfLastInstance(test.TestContext.TestDetails.ClassType, testContext, cleanUpExceptions);
                await AssemblyHookOrchestrator.ExecuteCleanups(test.TestContext.TestDetails.ClassType.Assembly, testContext, cleanUpExceptions);
            }

            if (cleanUpExceptions.Count == 1)
            {
                throw cleanUpExceptions[0];
            }
            
            if (cleanUpExceptions.Count > 1)
            {
                throw new AggregateException(cleanUpExceptions);
            }
            
            var timingProperty = GetTimingProperty(testContext);
            
            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, test.ToTestNode()
                    .WithProperty(PassedTestNodeStateProperty.CachedInstance)
                    .WithProperty(timingProperty)
            ));
            
            testContext.TaskCompletionSource.SetResult(null);

            testContext.Result = new TUnitTestResult
            {
                TestContext = testContext,
                Duration = timingProperty.GlobalTiming.Duration,
                Start = timingProperty.GlobalTiming.StartTime,
                End = timingProperty.GlobalTiming.EndTime,
                ComputerName = Environment.MachineName,
                Exception = null,
                Status = Status.Passed,
                Output = testContext.GetConsoleStandardOutput()
            };
        }
        catch (SkipTestException skipTestException)
        {
            await _logger.LogInformationAsync($"Skipping {test.TestDetails.DisplayName}...");
            
            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, 
                test.ToTestNode().WithProperty(new SkippedTestNodeStateProperty(skipTestException.Reason))));

            var now = DateTimeOffset.Now;
            
            testContext.TaskCompletionSource.SetException(skipTestException);
                
            testContext.Result = new TUnitTestResult
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
            var timingProperty = GetTimingProperty(testContext);
            
            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, test.ToTestNode()
                .WithProperty(GetFailureStateProperty(testContext, e, timingProperty.GlobalTiming.Duration))
                .WithProperty(timingProperty)
                .WithProperty(new TrxExceptionProperty(e.Message, e.StackTrace))));

            testContext.TaskCompletionSource.SetException(e);
            
            testContext.Result = new TUnitTestResult
            {
                Duration = timingProperty.GlobalTiming.Duration,
                Start = timingProperty.GlobalTiming.StartTime,
                End = timingProperty.GlobalTiming.EndTime,
                ComputerName = Environment.MachineName,
                Exception = e,
                Status = Status.Failed,
                Output = testContext.GetConsoleStandardOutput()
            };
        }
        finally
        {
            testContext.TaskCompletionSource.TrySetException(new Exception("Unknown error setting TaskCompletionSource"));

            using var lockHandle = await _consoleStandardOutLock.WaitAsync();
            
            await Dispose(testContext);
        }
    }

    private static TimingProperty GetTimingProperty(TestContext testContext)
    {
        var now = DateTimeOffset.Now;

        var start = testContext.Timings.MinBy(x => x.Start)?.Start ?? now;
        var end = testContext.Timings.MaxBy(x => x.End)?.End ?? now;

        var stepTimings = testContext.Timings.Select(x =>
            new StepTimingInfo(x.StepName, string.Empty, new TimingInfo(x.Start, x.End, x.Duration)));
        
        return new TimingProperty(new TimingInfo(start, end, end - start), [..stepTimings]);
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

    private async Task Dispose(TestContext testContext)
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

    private async Task DisposeInjectedData(TestData? testData)
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
    
    private static async Task DecrementSharedData(DiscoveredTest discoveredTest)
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

    private async Task RunTest(DiscoveredTest discoveredTest, List<Exception> cleanUpExceptions,
        CancellationToken cancellationToken)
    {
        StandardOutConsoleInterceptor.Instance.SetModule(discoveredTest.TestContext);
        StandardErrorConsoleInterceptor.Instance.SetModule(discoveredTest.TestContext);
        await _testInvoker.Invoke(discoveredTest, cleanUpExceptions, cancellationToken);
    }

    private readonly AsyncSemaphore _consoleStandardOutLock = new(1);

    private async Task ExecuteWithRetries(DiscoveredTest discoveredTest, List<Exception> cleanUpExceptions)
    {
        var testInformation = discoveredTest.TestContext.TestDetails;
        var retryCount = testInformation.RetryLimit;
        
        // +1 for the original non-retry
        for (var i = 0; i < retryCount + 1; i++)
        {
            try
            {
                await ExecuteCore(discoveredTest, cleanUpExceptions);
                break;
            }
            catch (Exception e)
            {
                if (i == retryCount 
                    || !await ShouldRetry(discoveredTest.TestContext, e, i + 1))
                {
                    throw;
                }

                await _logger.LogWarningAsync($"{testInformation.TestName} failed, retrying...");
                discoveredTest.ResetTestInstance();
                discoveredTest.TestContext.CurrentRetryAttempt++;
            }
        }
    }

    private async Task<bool> ShouldRetry(TestContext context, Exception e, int currentRetryCount)
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

    private async Task ExecuteCore(DiscoveredTest discoveredTest, List<Exception> cleanUpExceptions)
    {
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            return;
        }
        
        await ExecuteTestMethodWithTimeout(discoveredTest, cleanUpExceptions);
    }

    private async Task WaitForDependsOnTests(TestContext testContext)
    {
        foreach (var dependency in GetDependencies(testContext.TestDetails))
        {
            AssertDoNotDependOnEachOther(testContext, dependency);
            await dependency.TestTask;
        }
    }

    private IEnumerable<TestContext> GetDependencies(TestDetails testDetails)
    {
        return GetDependencies(testDetails, testDetails);
    }

    private IEnumerable<TestContext> GetDependencies(TestDetails original, TestDetails testDetails)
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
        TestContext[] dependencies = [dependency, ..GetDependencies(dependency.TestDetails)];
        
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

    private async Task ExecuteTestMethodWithTimeout(DiscoveredTest discoveredTest, List<Exception> cleanUpExceptions)
    {
        var testDetails = discoveredTest.TestDetails;
        
        if (testDetails.Timeout == null || testDetails.Timeout.Value == default)
        {
            await RunTest(discoveredTest, cleanUpExceptions, EngineCancellationToken.Token);
            return;
        }

        await RunHelpers.RunWithTimeoutAsync(token => RunTest(discoveredTest, cleanUpExceptions, token), testDetails.Timeout);
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
        typeof(TestNodeUpdateMessage)
    ];
}