using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using Semaphores;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Helpers;
using TUnit.Engine.Data;
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

        if (_cancellationTokenSource.IsCancellationRequested)
        {
            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, test.TestNode.WithProperty(new CancelledTestNodeStateProperty())));
            testContext._taskCompletionSource.SetCanceled();
            return;
        }
        
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, test.TestNode.WithProperty(InProgressTestNodeStateProperty.CachedInstance)));

        var cleanUpExceptions = new List<Exception>();
        
        DateTimeOffset? start = null;
        try
        {
            await WaitForDependsOnTests(test.TestContext);
            
            start = DateTimeOffset.Now;

            if (!_explicitFilterService.CanRun(test.TestInformation, filter))
            {
                throw new SkipTestException("Test with ExplicitAttribute was not explicitly run.");
            }

            var unInvokedTest = test.UnInvokedTest;
            
            foreach (var applicableTestAttribute in unInvokedTest.BeforeTestAttributes)
            {
                await applicableTestAttribute.OnBeforeTest(testContext);
            }

            try
            {
                await ClassHookOrchestrator.ExecuteSetups(unInvokedTest.TestContext.TestInformation.ClassType);
                await ExecuteWithRetries(unInvokedTest, cleanUpExceptions);
            }
            finally
            {
                await DecrementSharedData(unInvokedTest);
                
                foreach (var applicableTestAttribute in unInvokedTest.AfterTestAttributes)
                {
                    await RunHelpers.RunSafelyAsync(() => applicableTestAttribute.OnAfterTest(testContext), cleanUpExceptions);
                }
                
                await ClassHookOrchestrator.ExecuteCleanUpsIfLastInstance(unInvokedTest.TestContext.TestInformation.ClassType, cleanUpExceptions);
            }

            if (cleanUpExceptions.Count == 1)
            {
                throw cleanUpExceptions[0];
            }
            
            if (cleanUpExceptions.Count > 1)
            {
                throw new AggregateException(cleanUpExceptions);
            }

            var end = DateTimeOffset.Now;

            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, test.TestNode
                    .WithProperty(PassedTestNodeStateProperty.CachedInstance)
                    .WithProperty(new TimingProperty(new TimingInfo(start.Value, end, end - start.Value)))
            ));
            
            testContext._taskCompletionSource.SetResult();

            testContext.Result = new TUnitTestResult
            {
                TestContext = testContext,
                Duration = end - start.Value,
                Start = start.Value,
                End = end,
                ComputerName = Environment.MachineName,
                Exception = null,
                Status = Status.Passed,
                Output = testContext.GetConsoleOutput()
            };
        }
        catch (SkipTestException skipTestException)
        {
            await _logger.LogInformationAsync($"Skipping {test.TestInformation.DisplayName}...");
            
            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, 
                test.TestNode.WithProperty(new SkippedTestNodeStateProperty(skipTestException.Reason))));

            var now = DateTimeOffset.Now;
            
            testContext._taskCompletionSource.SetException(skipTestException);
                
            testContext.Result = new TUnitTestResult
            {
                Duration = TimeSpan.Zero,
                Start = start ?? now,
                End = start ?? now,
                ComputerName = Environment.MachineName,
                Exception = null,
                Status = Status.Skipped,
            };
        }
        catch (Exception e)
        {
            var end = DateTimeOffset.Now;

            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, test.TestNode
                .WithProperty(new FailedTestNodeStateProperty(e))
                .WithProperty(new TimingProperty(new TimingInfo(start ?? end, end, start.HasValue ? end-start.Value : TimeSpan.Zero)))
                .WithProperty(new TrxExceptionProperty(e.Message, e.StackTrace))));

            testContext._taskCompletionSource.SetException(e);
            
            testContext.Result = new TUnitTestResult
            {
                Duration = start.HasValue ? end - start.Value : TimeSpan.Zero,
                Start = start ?? end,
                End = end,
                ComputerName = Environment.MachineName,
                Exception = e,
                Status = Status.Failed,
                Output = testContext.GetConsoleOutput()
            };
        }
        finally
        {
            testContext._taskCompletionSource.TrySetException(new Exception("Unknown error setting TaskCompletionSource"));


            using var lockHandle = await _consoleStandardOutLock.WaitAsync();
            
            await Dispose(testContext);
        }
    }

    private async Task Dispose(TestContext testContext)
    {
        var testInformation = testContext.TestInformation;

        foreach (var methodArgument in testInformation.InternalTestMethodArguments)
        {
            await DisposeInjectedData(methodArgument.Argument, methodArgument.InjectedDataType);
        }
        
        foreach (var classArgument in testInformation.InternalTestClassArguments)
        {
            await DisposeInjectedData(classArgument.Argument, classArgument.InjectedDataType);
        }

        await _disposer.DisposeAsync(testContext);
    }

    private async Task DisposeInjectedData(object? obj, InjectedDataType injectedDataType)
    {
        if (injectedDataType 
            is InjectedDataType.SharedGlobally 
            or InjectedDataType.SharedByKey
            or InjectedDataType.SharedByTestClassType)
        {
            // Handled later - Might be shared with other tests too so we can't just dispose it without checking
            return;
        }

        await _disposer.DisposeAsync(obj);
    }
    
    private static async Task DecrementSharedData(UnInvokedTest unInvokedTest)
    {
        foreach (var methodArgument in unInvokedTest.TestContext.TestInformation.InternalTestMethodArguments)
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
        
        foreach (var classArgument in unInvokedTest.TestContext.TestInformation.InternalTestClassArguments)
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

    private async Task RunTest(UnInvokedTest unInvokedTest, List<Exception> cleanUpExceptions)
    {
        ConsoleInterceptor.Instance.SetModule(unInvokedTest.TestContext);
        await _testInvoker.Invoke(unInvokedTest, cleanUpExceptions);
    }

    private readonly AsyncSemaphore _consoleStandardOutLock = new(1);

    private async Task ExecuteWithRetries(UnInvokedTest unInvokedTest, List<Exception> cleanUpExceptions)
    {
        var testInformation = unInvokedTest.TestContext.TestInformation;
        var retryCount = testInformation.RetryLimit;
        
        // +1 for the original non-retry
        for (var i = 0; i < retryCount + 1; i++)
        {
            try
            {
                await ExecuteCore(unInvokedTest, cleanUpExceptions);
                break;
            }
            catch (Exception e)
            {
                if (i == retryCount 
                    || !await ShouldRetry(testInformation, e, i + 1))
                {
                    throw;
                }

                await _logger.LogWarningAsync($"{testInformation.TestName} failed, retrying...");
                unInvokedTest.ResetTestInstance();
                testInformation.CurrentRetryAttempt++;
            }
        }
    }

    private async Task<bool> ShouldRetry(TestInformation testInformation, Exception e, int currentRetryCount)
    {
        try
        {
            var retryAttribute = testInformation.RetryAttribute;

            if (retryAttribute == null)
            {
                return false;
            }

            return await retryAttribute.ShouldRetry(testInformation, e, currentRetryCount);
        }
        catch (Exception exception)
        {
            await _logger.LogErrorAsync(exception);
            return false;
        }
    }

    private async Task ExecuteCore(UnInvokedTest unInvokedTest, List<Exception> cleanUpExceptions)
    {
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            return;
        }
        
        var testContext = unInvokedTest.TestContext;
        var testInformation = testContext.TestInformation;
        
        using var testLevelCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);

        if (testInformation.Timeout != null && testInformation.Timeout.Value != default)
        {
            testLevelCancellationTokenSource.CancelAfter(testInformation.Timeout.Value);
        }

        testContext.CancellationTokenSource = testLevelCancellationTokenSource;

        await ExecuteTestMethodWithTimeout(
            testInformation,
            () => RunTest(unInvokedTest, cleanUpExceptions),
            testLevelCancellationTokenSource
        );
    }

    private async Task WaitForDependsOnTests(TestContext testContext)
    {
        foreach (var dependency in GetDependencies(testContext.TestInformation))
        {
            AssertDoNotDependOnEachOther(testContext, dependency);
            await dependency.TestTask;
        }
    }

    private IEnumerable<TestContext> GetDependencies(TestInformation testInformation)
    {
        return GetDependencies(testInformation, testInformation);
    }

    private IEnumerable<TestContext> GetDependencies(TestInformation original, TestInformation testInformation)
    {
        foreach (var dependency in testInformation.Attributes
                     .OfType<DependsOnAttribute>()
                     .SelectMany(dependsOnAttribute => TestDictionary.GetTestsByNameAndParameters(dependsOnAttribute.TestName,
                         dependsOnAttribute.ParameterTypes, testInformation.ClassType,
                         testInformation.TestClassParameterTypes)))
        {
            yield return dependency;

            if (dependency.TestInformation.IsSameTest(original))
            {
                yield break;
            }
            
            foreach (var nestedDependency in GetDependencies(original, dependency.TestInformation))
            {
                yield return nestedDependency;
                
                if (nestedDependency.TestInformation.IsSameTest(original))
                {
                    yield break;
                }
            }
        }
    }

    private void AssertDoNotDependOnEachOther(TestContext testContext, TestContext dependency)
    {
        TestContext[] dependencies = [dependency, ..GetDependencies(dependency.TestInformation)];
        
        foreach (var dependencyOfDependency in dependencies)
        {
            if (dependencyOfDependency.TestInformation.IsSameTest(testContext.TestInformation))
            {
                throw new DependencyConflictException(testContext.TestInformation, dependencies.Select(x => x.TestInformation));
            }

            if (dependencyOfDependency.TestInformation.NotInParallelConstraintKeys != null)
            {
                throw new DependsOnNotInParallelException(testContext.TestInformation.TestName);
            }
        }
    }

    private async Task ExecuteTestMethodWithTimeout(TestInformation testInformation, Func<Task> testDelegate,
        CancellationTokenSource cancellationTokenSource)
    {
        var methodResult = testDelegate();

        if (testInformation.Timeout == null || testInformation.Timeout.Value == default)
        {
            await methodResult;
            return;
        }
        
        var timeoutTask = Task.Delay(testInformation.Timeout.Value, cancellationTokenSource.Token)
            .ContinueWith(_ =>
            {
                if (methodResult.IsCompleted)
                {
                    return;
                }
                
                throw new TimeoutException(testInformation);
            });

        var failQuicklyTask = Task.Run(async () =>
        {
            while (!methodResult.IsCompleted && !timeoutTask.IsCompleted)
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
        });

        await await Task.WhenAny(methodResult, timeoutTask, failQuicklyTask);
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