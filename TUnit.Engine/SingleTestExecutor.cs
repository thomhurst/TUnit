using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Engine.Models;
using TimeoutException = TUnit.Core.Exceptions.TimeoutException;

namespace TUnit.Engine;

internal class SingleTestExecutor : IDataProducer
{
    private readonly IExtension _extension;
    private readonly Disposer _disposer;
    private readonly ILogger<SingleTestExecutor> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly IMessageBus _messageBus;
    private readonly TestInvoker _testInvoker;

    public GroupedTests GroupedTests { get; private set; } = null!;

    public SingleTestExecutor(
        IExtension extension,
        Disposer disposer,
        ILoggerFactory loggerFactory,
        CancellationTokenSource cancellationTokenSource,
        IMessageBus messageBus,
        TestInvoker testInvoker)
    {
        _extension = extension;
        _disposer = disposer;
        _logger = loggerFactory.CreateLogger<SingleTestExecutor>();
        _cancellationTokenSource = cancellationTokenSource;
        _messageBus = messageBus;
        _testInvoker = testInvoker;
    }

    public async Task<TUnitTestResult> ExecuteTestAsync(TestNode testNode, TestSessionContext session)
    {
        if(_cancellationTokenSource.IsCancellationRequested)
        {
            var now = DateTimeOffset.Now;
            
            await _messageBus.PublishAsync(this, new TestNodeUpdateMessage(session.SessionUid, new TestNode
            {
                Uid = testNode.Uid,
                DisplayName = testNode.DisplayName,
                Properties = new PropertyBag(new CancelledTestNodeStateProperty())
            }));

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

        await _messageBus.PublishAsync(this, new TestNodeUpdateMessage(session.SessionUid, new TestNode
        {
            Uid = testNode.Uid,
            DisplayName = testNode.DisplayName,
            Properties = new PropertyBag(InProgressTestNodeStateProperty.CachedInstance)
        }));

        UnInvokedTest? unInvokedTest;
        TestContext? testContext = null;
        try
        {
            unInvokedTest = TestDictionary.GetTest(testNode.Uid);
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

            await _messageBus.PublishAsync(this, new TestNodeUpdateMessage(session.SessionUid, new TestNode
            {
                Uid = testNode.Uid,
                DisplayName = testNode.DisplayName,
                Properties = new PropertyBag
                (
                    PassedTestNodeStateProperty.CachedInstance,
                    new TimingProperty(new TimingInfo(start, end, end - start))
                )
            }));

            return new TUnitTestResult
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
            await _logger.LogInformationAsync($"Skipping {testNode.DisplayName}...");
            
            await _messageBus.PublishAsync(this, new TestNodeUpdateMessage(session.SessionUid, new TestNode
            {
                Uid = testNode.Uid,
                DisplayName = testNode.DisplayName,
                Properties = new PropertyBag
                (
                    new SkippedTestNodeStateProperty(skipTestException.Reason)
                )
            }));
            
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

            await _messageBus.PublishAsync(this, new TestNodeUpdateMessage(session.SessionUid, new TestNode
            {
                Uid = testNode.Uid,
                DisplayName = testNode.DisplayName,
                Properties = new PropertyBag
                (
                    new FailedTestNodeStateProperty(e),
                    new TimingProperty(new TimingInfo(start, end, end-start))
                )
            }));
            
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

    internal void SetAllTests(GroupedTests tests)
    {
        GroupedTests = tests;
    }

    private readonly object _consoleStandardOutLock = new();

    private bool IsExplicitlyRun(TestNode testNode)
    {
        // TODO: Re-implement
        // if (_testFilterProvider.IsFilteredTestRun)
        // {
        //     return true;
        // }

        return false;

        // TODO:
        // var explicitFor = testNode.GetProperty<ExplicitForProperty>()?.ExplicitFor;
        //
        // if (string.IsNullOrEmpty(explicitFor))
        // {
        //     // Isn't required to be 'Explicitly' run
        //     return true;
        // }
        //
        // // If all tests being run are from the same "Explicit" attribute, e.g. same class or same method, then yes these have been run explicitly.
        // return GroupedTests.AllTests.All(x => x.GetProperty<ExplicitForProperty>()?.ExplicitFor == explicitFor);
    }

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