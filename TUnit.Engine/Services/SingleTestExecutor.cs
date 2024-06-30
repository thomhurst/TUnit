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
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, test.TestNode.WithProperty(new CancelledTestNodeStateProperty())));
            test.TestContext._taskCompletionSource.SetCanceled();
            return;
        }
        
        var start = DateTimeOffset.Now;

        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, test.TestNode.WithProperty(InProgressTestNodeStateProperty.CachedInstance)));

        TestContext? testContext = null;
        try
        {
            if (!_explicitFilterService.CanRun(test.TestInformation, filter))
            {
                throw new SkipTestException("Test with ExplicitAttribute was not explicitly run.");
            }
            
            if (!TestDictionary.TryGetTest(test.TestInformation.TestId, out var unInvokedTest))
            {
                var failedInitializationTest = TestDictionary.GetFailedInitializationTest(test.TestInformation.TestId);
                throw new TestFailedInitializationException($"The test {test.TestInformation.DisplayName} at {test.TestInformation.TestFilePath}:{test.TestInformation.TestLineNumber} failed to initialize", failedInitializationTest.Exception);
            }

            testContext = unInvokedTest.TestContext;

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
                    .WithProperty(new TimingProperty(new TimingInfo(start, end, end - start)))
            ));

            testContext.Result = new TUnitTestResult
            {
                TestContext = testContext,
                Duration = end - start,
                Start = start,
                End = end,
                ComputerName = Environment.MachineName,
                Exception = null,
                Status = Status.Passed,
                Output = testContext.GetConsoleOutput()
            };
            
            testContext._taskCompletionSource.SetResult();
        }
        catch (SkipTestException skipTestException)
        {
            await _logger.LogInformationAsync($"Skipping {test.TestInformation.DisplayName}...");
            
            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, 
                test.TestNode.WithProperty(new SkippedTestNodeStateProperty(skipTestException.Reason))));
            
            if (testContext != null)
            {
                testContext.Result = new TUnitTestResult
                {
                    Duration = TimeSpan.Zero,
                    Start = start,
                    End = start,
                    ComputerName = Environment.MachineName,
                    Exception = null,
                    Status = Status.Skipped,
                };
                
                testContext._taskCompletionSource.SetException(skipTestException);
            }
        }
        catch (Exception e)
        {
            var end = DateTimeOffset.Now;

            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, test.TestNode
                .WithProperty(new FailedTestNodeStateProperty(e))
                .WithProperty(new TimingProperty(new TimingInfo(start, end, end-start)))
                .WithProperty(new KeyValuePairStringProperty("trxreport.exceptionmessage", e.Message))
                .WithProperty(new KeyValuePairStringProperty("trxreport.exceptionstacktrace", e.StackTrace!))));
            
            if (testContext != null)
            {
                testContext.Result = new TUnitTestResult
                {
                    Duration = end - start,
                    Start = start,
                    End = end,
                    ComputerName = Environment.MachineName,
                    Exception = e,
                    Status = Status.Failed,
                    Output = testContext.GetConsoleOutput()
                };
                
                testContext._taskCompletionSource.SetException(e);
            }
        }
        finally
        {
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
            if (e is TestNotExecutedException)
            {
                return false;
            }
            
            var retryAttribute = testInformation.LazyRetryAttribute.Value;

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

        await WaitForDependsOnTests(unInvokedTest.TestContext);

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
        foreach (var dependsOnAttribute in testContext.TestInformation.LazyTestAndClassAttributes.Value.OfType<DependsOnAttribute>())
        {
            var dependencies = TestDictionary.GetTestsByNameAndParameters(dependsOnAttribute.TestName,
                dependsOnAttribute.ParameterTypes, testContext.TestInformation.ClassType,
                testContext.TestInformation.TestClassParameterTypes);

            foreach (var dependency in dependencies)
            {
                await dependency.TestTask;
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