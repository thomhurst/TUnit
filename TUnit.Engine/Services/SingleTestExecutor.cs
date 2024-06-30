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

        if (_cancellationTokenSource.IsCancellationRequested)
        {
            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, test.TestNode.WithProperty(new CancelledTestNodeStateProperty())));
            testContext._taskCompletionSource.SetCanceled();
            return;
        }
        
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, test.TestNode.WithProperty(InProgressTestNodeStateProperty.CachedInstance)));

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
            
            foreach (var applicableTestAttribute in unInvokedTest.ApplicableTestAttributes)
            {
                await applicableTestAttribute.Apply(testContext);
            }

            try
            {
                await ClassHookOrchestrator.ExecuteSetups(unInvokedTest.TestContext.TestInformation.ClassType);
                await ExecuteWithRetries(unInvokedTest);
            }
            finally
            {
                await ClassHookOrchestrator.ExecuteCleanUpsIfLastInstance(unInvokedTest.TestContext.TestInformation.ClassType, new List<Exception>());
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
                .WithProperty(new KeyValuePairStringProperty("trxreport.exceptionmessage", e.Message))
                .WithProperty(new KeyValuePairStringProperty("trxreport.exceptionstacktrace", e.StackTrace!))));

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
            
            await _disposer.DisposeAsync(testContext?.TestInformation.ClassInstance);

            using var lockHandle = await _consoleStandardOutLock.WaitAsync();
            
            await _disposer.DisposeAsync(testContext);
        }
    }

    private async Task RunTest(UnInvokedTest unInvokedTest)
    {
        ConsoleInterceptor.Instance.SetModule(unInvokedTest.TestContext);
        await _testInvoker.Invoke(unInvokedTest);
    }

    private readonly AsyncSemaphore _consoleStandardOutLock = new(1);

    private async Task ExecuteWithRetries(UnInvokedTest unInvokedTest)
    {
        var testInformation = unInvokedTest.TestContext.TestInformation;
        var retryCount = testInformation.RetryCount;
        
        // +1 for the original non-retry
        for (var i = 0; i < retryCount + 1; i++)
        {
            try
            {
                await ExecuteCore(unInvokedTest);
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

    private async Task ExecuteCore(UnInvokedTest unInvokedTest)
    {
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            return;
        }
        
        var testContext = unInvokedTest.TestContext;
        var testInformation = testContext.TestInformation;

        testInformation.CurrentExecutionCount++;

        using var testLevelCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);

        if (testInformation.Timeout != null && testInformation.Timeout.Value != default)
        {
            testLevelCancellationTokenSource.CancelAfter(testInformation.Timeout.Value);
        }

        testContext.CancellationTokenSource = testLevelCancellationTokenSource;

        await ExecuteTestMethodWithTimeout(
            testInformation,
            () => RunTest(unInvokedTest),
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
        foreach (var dependency in testInformation.TestAndClassAttributes
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
        if (testContext.TestInformation.IsSameTest(dependency.TestInformation))
        {
            throw new DependencyConflictException(testContext.TestInformation, [dependency.TestInformation]);
        }

        var nestedDependencies = GetDependencies(dependency.TestInformation).ToArray();
        
        foreach (var dependencyOfDependency in nestedDependencies)
        {
            if (dependencyOfDependency.TestInformation.IsSameTest(testContext.TestInformation))
            {
                throw new DependencyConflictException(testContext.TestInformation, [dependency.TestInformation, ..nestedDependencies.Select(x => x.TestInformation)]);
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