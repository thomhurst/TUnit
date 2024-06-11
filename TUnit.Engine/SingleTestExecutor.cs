using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Engine.Extensions;
using TimeoutException = TUnit.Core.Exceptions.TimeoutException;

namespace TUnit.Engine;

internal class SingleTestExecutor : IDataProducer
{
    private readonly IExtension _extension;
    private readonly Disposer _disposer;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly IMessageBus _messageBus;
    private readonly TestInvoker _testInvoker;
    private readonly ExplicitFilterService _explicitFilterService;
    private readonly TUnitLogger _logger;

    public SingleTestExecutor(
        IExtension extension,
        Disposer disposer,
        CancellationTokenSource cancellationTokenSource,
        IMessageBus messageBus,
        TestInvoker testInvoker,
        ExplicitFilterService explicitFilterService,
        TUnitLogger logger)
    {
        _extension = extension;
        _disposer = disposer;
        _cancellationTokenSource = cancellationTokenSource;
        _messageBus = messageBus;
        _testInvoker = testInvoker;
        _explicitFilterService = explicitFilterService;
        _logger = logger;
    }

    public async Task<TUnitTestResult> ExecuteTestAsync(TestInformation test, ITestExecutionFilter? filter,
        TestSessionContext session)
    {
        if(_cancellationTokenSource.IsCancellationRequested)
        {
            var now = DateTimeOffset.Now;
            
            await _messageBus.PublishAsync(this, new TestNodeUpdateMessage(session.SessionUid, test.ToTestNode().WithProperty(new CancelledTestNodeStateProperty())));

            return new TUnitTestResult
            {
                Duration = TimeSpan.Zero,
                Start = now,
                End = now,
                ComputerName = Environment.MachineName,
                Exception = null,
                Status = Status.None,
            };
        }
        
        var start = DateTimeOffset.Now;

        await _messageBus.PublishAsync(this, new TestNodeUpdateMessage(session.SessionUid, test.ToTestNode().WithProperty(InProgressTestNodeStateProperty.CachedInstance)));

        TestContext? testContext = null;
        try
        {
            if (!_explicitFilterService.CanRun(test, filter))
            {
                throw new SkipTestException("Test with ExplicitAttribute was not explicitly run.");
            }
            
            if (!TestDictionary.TryGetTest(test.TestId, out var unInvokedTest))
            {
                var failedInitializationTest = TestDictionary.GetFailedInitializationTest(test.TestId);
                throw new TestFailedInitializationException($"The test {test.DisplayName} at {test.TestFilePath}:{test.TestLineNumber} failed to initialize", failedInitializationTest.Exception);
            }

            testContext = unInvokedTest.TestContext;

            foreach (var applicableTestAttribute in unInvokedTest.ApplicableTestAttributes)
            {
                await applicableTestAttribute.Apply(testContext);
            }
            
            await ClassHookOrchestrator.ExecuteSetups(unInvokedTest.TestContext.TestInformation.ClassType);
            await GlobalTestHookOrchestrator.ExecuteSetups(unInvokedTest.TestContext);
            
            try
            {
                await Task.Run(async () => { await ExecuteWithRetries(unInvokedTest); });
            }
            finally
            {
                var cleanUpExceptions = new List<Exception>();
                await GlobalTestHookOrchestrator.ExecuteCleanUps(unInvokedTest.TestContext, cleanUpExceptions);
                await ClassHookOrchestrator.ExecuteCleanUpsIfLastInstance(unInvokedTest.TestContext.TestInformation.ClassInstance, unInvokedTest.TestContext.TestInformation.ClassType, cleanUpExceptions);
            }

            var end = DateTimeOffset.Now;

            await _messageBus.PublishAsync(this, new TestNodeUpdateMessage(session.SessionUid, test.ToTestNode()
                    .WithProperty(PassedTestNodeStateProperty.CachedInstance)
                    .WithProperty(new TimingProperty(new TimingInfo(start, end, end - start)))
            ));

            return testContext.Result = new TUnitTestResult
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
        }
        catch (SkipTestException skipTestException)
        {
            await _logger.LogInformationAsync($"Skipping {test.DisplayName}...");
            
            await _messageBus.PublishAsync(this, new TestNodeUpdateMessage(session.SessionUid, 
                test.ToTestNode().WithProperty(new SkippedTestNodeStateProperty(skipTestException.Reason))));
            
            var unitTestResult = new TUnitTestResult
            {
                Duration = TimeSpan.Zero,
                Start = start,
                End = start,
                ComputerName = Environment.MachineName,
                Exception = null,
                Status = Status.Skipped,
            };
            
            if (testContext != null)
            {
                testContext.Result = unitTestResult;
            }
            
            return unitTestResult;
        }
        catch (Exception e)
        {
            var end = DateTimeOffset.Now;

            await _messageBus.PublishAsync(this, new TestNodeUpdateMessage(session.SessionUid, test.ToTestNode()
                .WithProperty(new FailedTestNodeStateProperty(e))
                .WithProperty(new TimingProperty(new TimingInfo(start, end, end-start)))
                .WithProperty(new KeyValuePairStringProperty("trxreport.exceptionmessage", e.Message))
                .WithProperty(new KeyValuePairStringProperty("trxreport.exceptionstacktrace", e.StackTrace!))));
            
            var unitTestResult = new TUnitTestResult
            {
                Duration = end - start,
                Start = start,
                End = end,
                ComputerName = Environment.MachineName,
                Exception = e,
                Status = Status.Failed,
                Output = testContext?.GetConsoleOutput()
            };

            if (testContext != null)
            {
                testContext.Result = unitTestResult;
            }
            
            return unitTestResult;
        }
        finally
        {
            await _disposer.DisposeAsync(testContext?.TestInformation.ClassInstance);

            lock (_consoleStandardOutLock)
            {
                testContext?.Dispose();
            }
        }
    }

    private async Task RunTest(UnInvokedTest unInvokedTest)
    {
        await Task.Run(async () =>
        {
            ConsoleInterceptor.Instance.SetModule(unInvokedTest.TestContext);
            await _testInvoker.Invoke(unInvokedTest);
        });
    }

    private readonly object _consoleStandardOutLock = new();

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
                    || !await ShouldRetry(testInformation, e))
                {
                    throw;
                }
                
                await _logger.LogWarningAsync($"{testInformation.TestName} failed, retrying...");
            }
        }
    }

    private async Task<bool> ShouldRetry(TestInformation testInformation, Exception e)
    {
        try
        {
            var retryAttribute = testInformation.LazyRetryAttribute.Value;

            if (retryAttribute == null)
            {
                return false;
            }

            return await retryAttribute.ShouldRetry(testInformation, e);
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

        try
        {
            await ExecuteTestMethodWithTimeout(
                testInformation,
                () => RunTest(unInvokedTest),
                testLevelCancellationTokenSource
            );
        }
        finally
        {
            unInvokedTest.ResetTestInstance();
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
            .ContinueWith(t =>
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
                await Task.Delay(TimeSpan.FromSeconds(5));
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
    
    public Type[] DataTypesProduced { get; } = [typeof(TestNodeUpdateMessage)];
}