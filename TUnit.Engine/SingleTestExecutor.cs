using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Messages;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Extensions;
using TUnit.Engine.Models;
using TUnit.Engine.Models.Properties;
using TimeoutException = TUnit.Core.Exceptions.TimeoutException;

namespace TUnit.Engine;

internal class SingleTestExecutor
{
    private readonly MethodInvoker _methodInvoker;
    private readonly TestClassCreator _testClassCreator;
    private readonly TestMethodRetriever _testMethodRetriever;
    private readonly Disposer _disposer;
    private readonly ILogger<SingleTestExecutor> _logger;
    private readonly IMessageBus _messageBus;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ClassWalker _classWalker;
    private readonly TestFilterProvider _testFilterProvider;
    private readonly ConsoleInterceptor _consoleInterceptor;

    public GroupedTests GroupedTests { get; private set; } = null!;

    public SingleTestExecutor(MethodInvoker methodInvoker, 
        TestClassCreator testClassCreator,
        TestMethodRetriever testMethodRetriever,
        Disposer disposer,
        ILogger<SingleTestExecutor> logger,
        IMessageBus messageBus,
        CancellationTokenSource cancellationTokenSource,
        ClassWalker classWalker,
        TestFilterProvider testFilterProvider,
        ConsoleInterceptor consoleInterceptor)
    {
        _methodInvoker = methodInvoker;
        _testClassCreator = testClassCreator;
        _testMethodRetriever = testMethodRetriever;
        _disposer = disposer;
        _logger = logger;
        _messageBus = messageBus;
        _cancellationTokenSource = cancellationTokenSource;
        _classWalker = classWalker;
        _testFilterProvider = testFilterProvider;
        _consoleInterceptor = consoleInterceptor;
    }
    
    private readonly ConcurrentDictionary<string, Task> _oneTimeSetUpRegistry = new();

    public async Task<TUnitTestResult> ExecuteTest(TestNode testNode)
    {
         var start = DateTimeOffset.Now;
        
        if (testNode.GetProperty<SkippedTestNodeStateProperty>() != null || !IsExplicitlyRun(testNode))
        {
            await _logger.LogInformationAsync($"Skipping {testNode.DisplayName}...");

            return new TUnitTestResult
            {
                Duration = TimeSpan.Zero,
                Start = start,
                End = start,
                ComputerName = Environment.MachineName,
                Exception = null,
                Status = Status.Skipped,
            };
        }
        
        object? classInstance = null;
        TestContext? testContext = null;
        try
        {
            await Task.Run(async () =>
            {
                classInstance = _testClassCreator.CreateClass(testNode, out var classType);

                var methodInfo = _testMethodRetriever.GetTestMethod(classType, testNode);

                var testInformation = testNode.ToTestInformation(classType, classInstance, methodInfo);

                testContext = new TestContext(testInformation);
                
                _consoleInterceptor.SetModule(testContext);
                
                TestContext.Current = testContext;

                var customTestAttributes = methodInfo.GetCustomAttributes()
                    .Concat(classType.GetCustomAttributes())
                    .OfType<ITestAttribute>();

                foreach (var customTestAttribute in customTestAttributes)
                {
                    await customTestAttribute.ApplyToTest(testContext);
                }

                if (testContext.FailReason != null)
                {
                    throw new Exception(testContext.FailReason);
                }

                if (testContext.SkipReason == null)
                {
                    await ExecuteWithRetries(testContext, testInformation, classInstance);
                }
            });

            var end = DateTimeOffset.Now;

            return new TUnitTestResult
            {
                TestContext = testContext,
                Duration = end - start,
                Start = start,
                End = end,
                ComputerName = Environment.MachineName,
                Exception = null,
                Status = testContext!.SkipReason != null ? Status.Skipped : Status.Passed,
                Output = testContext?.GetConsoleOutput() ?? testContext!.FailReason ?? testContext.SkipReason
            };
        }
        catch (Exception e)
        {
            var end = DateTimeOffset.Now;

            var unitTestResult = new TUnitTestResult
            {
                Duration = end - start,
                Start = start,
                End = end,
                ComputerName = Environment.MachineName,
                Exception = e,
                Status = testContext?.SkipReason != null ? Status.Skipped : Status.Failed,
                Output = testContext?.GetConsoleOutput()
            };

            if (testContext != null)
            {
                testContext.Result = unitTestResult;
            }

            await ExecuteCleanUps(classInstance);

            return unitTestResult;
        }
        finally
        {
            await _disposer.DisposeAsync(classInstance);

            lock (_consoleStandardOutLock)
            {
                testContext?.Dispose();
            }
        }
    }

    internal void SetAllTests(GroupedTests tests)
    {
        GroupedTests = tests;
    }

    private readonly object _consoleStandardOutLock = new();

    private bool IsExplicitlyRun(TestNode testNode)
    {
        if (_testFilterProvider.IsFilteredTestRun)
        {
            return true;
        }

        var explicitFor = testNode.GetProperty<ExplicitForProperty>()?.ExplicitFor;

        if (string.IsNullOrEmpty(explicitFor))
        {
            // Isn't required to be 'Explicitly' run
            return true;
        }

        // If all tests being run are from the same "Explicit" attribute, e.g. same class or same method, then yes these have been run explicitly.
        return GroupedTests.AllTests.All(x => x.GetProperty<ExplicitForProperty>()?.ExplicitFor == explicitFor);
    }

    private async Task ExecuteWithRetries(TestContext testContext, TestInformation testInformation, object? @class)
    {
        var retryCount = testInformation.RetryCount;
        
        // +1 for the original non-retry
        for (var i = 0; i < retryCount + 1; i++)
        {
            try
            {
                await ExecuteCore(testContext, testInformation, @class);
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

        ;
    }

    private async Task ExecuteCore(TestContext testContext, TestInformation testInformation, object? @class)
    {
        testInformation.CurrentExecutionCount++;
        
        await ExecuteSetUps(@class, testInformation.ClassType);

        var testLevelCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);

        if (testInformation.Timeout != null && testInformation.Timeout.Value != default)
        {
            testLevelCancellationTokenSource.CancelAfter(testInformation.Timeout.Value);
        }

        testContext.CancellationTokenSource = testLevelCancellationTokenSource;

        try
        {
            await ExecuteTestMethodWithTimeout(testInformation, @class, testLevelCancellationTokenSource);
        }
        catch
        {
            testLevelCancellationTokenSource.Cancel();
            testLevelCancellationTokenSource.Dispose();
            throw;
        }
    }

    private async Task ExecuteTestMethodWithTimeout(TestInformation testInformation, object? @class,
        CancellationTokenSource cancellationTokenSource)
    {
        
        var methodResult = _methodInvoker.InvokeMethod(@class, testInformation.MethodInfo, BindingFlags.Default,
            testInformation.TestMethodArguments, cancellationTokenSource.Token);

        if (testInformation.Timeout == null || testInformation.Timeout.Value == default)
        {
            await methodResult;
            return;
        }
        
        var timeoutTask = Task.Delay(testInformation.Timeout.Value, cancellationTokenSource.Token)
            .ContinueWith(_ => throw new TimeoutException(testInformation));

        await await Task.WhenAny(timeoutTask, methodResult);
    }

    private async Task ExecuteSetUps(object? @class, Type testDetailsClassType)
    {
        await _oneTimeSetUpRegistry.GetOrAdd(testDetailsClassType.FullName!, _ => ExecuteOnlyOnceSetUps(@class, testDetailsClassType));

        var setUpMethods = _classWalker.GetSelfAndBaseTypes(testDetailsClassType)
            .Reverse()
            .SelectMany(x => x.GetMethods())
            .Where(x => !x.IsStatic)
            .Where(x => x.GetCustomAttributes<SetUpAttribute>().Any());

        foreach (var setUpMethod in setUpMethods)
        {
            await _methodInvoker.InvokeMethod(@class, setUpMethod, BindingFlags.Default, null, default);
        }
    }
    
    private async Task ExecuteCleanUps(object? @class)
    {
        if (@class is null)
        {
            return;
        }
        
        var cleanUpMethods = _classWalker.GetSelfAndBaseTypes(@class.GetType())
            .SelectMany(x => x.GetMethods())
            .Where(x => !x.IsStatic)
            .Where(x => x.GetCustomAttributes<CleanUpAttribute>().Any());

        var exceptions = new List<Exception>();
        
        foreach (var cleanUpMethod in cleanUpMethods)
        {
            try
            {
                await _methodInvoker.InvokeMethod(@class, cleanUpMethod, BindingFlags.Default, null, default);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        if (exceptions.Count == 1)
        {
            await _logger.LogErrorAsync("Error running CleanUp");
            await _logger.LogErrorAsync(exceptions.First());
        }
        else if (exceptions.Count > 1)
        {
            var aggregateException = new AggregateException(exceptions);
            await _logger.LogErrorAsync("Error running CleanUp");
            await _logger.LogErrorAsync(aggregateException);
        }
    }
                                         
    private async Task ExecuteOnlyOnceSetUps(object? @class, Type testDetailsClassType)
    {
        var oneTimeSetUpMethods = _classWalker.GetSelfAndBaseTypes(testDetailsClassType)
            .Reverse()
            .SelectMany(x => x.GetMethods())
            .Where(x => x.IsStatic)
            .Where(x => x.GetCustomAttributes<OnlyOnceSetUpAttribute>().Any());

        foreach (var oneTimeSetUpMethod in oneTimeSetUpMethods)
        {
            await _methodInvoker.InvokeMethod(@class, oneTimeSetUpMethod, BindingFlags.Static | BindingFlags.Public, null, default);
        }
    }

    private IProperty GetOutcomeProperty(TestContext? resultTestContext, Status resultStatus)
    {
        if (!string.IsNullOrEmpty(resultTestContext?.FailReason))
        {
            return new FailedTestNodeStateProperty(resultTestContext.FailReason);
        }

        if (!string.IsNullOrEmpty(resultTestContext?.SkipReason))
        {
            return new SkippedTestNodeStateProperty(resultTestContext.SkipReason);
        }
        
        return resultStatus switch
        {
            Status.None => new ErrorTestNodeStateProperty(),
            Status.Passed => new PassedTestNodeStateProperty(),
            Status.Failed => new FailedTestNodeStateProperty(),
            Status.Skipped => new SkippedTestNodeStateProperty(),
            _ => throw new ArgumentOutOfRangeException(nameof(resultStatus), resultStatus, null)
        };
    }
}