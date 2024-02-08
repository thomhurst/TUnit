using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Extensions;
using TimeoutException = TUnit.Core.Exceptions.TimeoutException;

namespace TUnit.Engine;

internal class SingleTestExecutor
{
    private readonly MethodInvoker _methodInvoker;
    private readonly TestClassCreator _testClassCreator;
    private readonly TestMethodRetriever _testMethodRetriever;
    private readonly Disposer _disposer;
    private readonly IMessageLogger _messageLogger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    public SingleTestExecutor(MethodInvoker methodInvoker, 
        TestClassCreator testClassCreator,
        TestMethodRetriever testMethodRetriever,
        Disposer disposer,
        IMessageLogger messageLogger,
        CancellationTokenSource cancellationTokenSource)
    {
        _methodInvoker = methodInvoker;
        _testClassCreator = testClassCreator;
        _testMethodRetriever = testMethodRetriever;
        _disposer = disposer;
        _messageLogger = messageLogger;
        _cancellationTokenSource = cancellationTokenSource;
    }
    
    private readonly ConcurrentDictionary<string, Task> _oneTimeSetUpRegistry = new();

    public async Task<TUnitTestResult> ExecuteTest(TestCase testCase)
    {
        var start = DateTimeOffset.Now;
        
        if (testCase.GetPropertyValue(TUnitTestProperties.IsSkipped, false))
        {
            _messageLogger.SendMessage(TestMessageLevel.Informational, $"Skipping {testCase.DisplayName}...");

            return new TUnitTestResult
            {
                Duration = TimeSpan.Zero,
                Start = start,
                End = start,
                ComputerName = Environment.MachineName,
                Exception = null,
                Status = Status.Skipped
            };
        }
        
        object? classInstance = null;
        TestContext? testContext = null;
        try
        {
            await Task.Run(async () =>
            {
                classInstance = _testClassCreator.CreateClass(testCase, out var classType);

                var methodInfo = _testMethodRetriever.GetTestMethod(classType, testCase);

                var testInformation = testCase.ToTestInformation(classType, classInstance, methodInfo);
                
                testContext = new TestContext(testInformation);
                TestContext.Current = testContext;

                var customTestAttributes = methodInfo.GetCustomAttributes()
                    .Concat(classType.GetCustomAttributes())
                    .OfType<ITestAttribute>();
                
                foreach (var customTestAttribute in customTestAttributes)
                {
                    await customTestAttribute.ApplyToTest(testContext);
                }
                
                try
                {
                    if (testContext.FailReason != null)
                    {
                        throw new Exception(testContext.FailReason);
                    }

                    if(testContext.SkipReason == null)
                    {
                        await ExecuteWithRetries(testContext, testInformation, classInstance);
                    }
                }
                finally
                {
                    await _disposer.DisposeAsync(classInstance);
                }
            });

            var end = DateTimeOffset.Now;

            return new TUnitTestResult
            {
                Duration = end - start,
                Start = start,
                End = end,
                ComputerName = Environment.MachineName,
                Exception = null,
                Status = testContext!.SkipReason != null ? Status.Skipped : Status.Passed,
                Output = testContext?.GetOutput() ?? testContext!.FailReason ?? testContext.SkipReason
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
                Status = Status.Failed,
                Output = testContext?.GetOutput()
            };
            
            if (testContext != null)
            {
                testContext.Result = unitTestResult;
            }
            
            await ExecuteCleanUps(classInstance);
            
            return unitTestResult;
        }
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
            catch
            {
                if (i == retryCount)
                {
                    throw;
                }
                
                _messageLogger.SendMessage(TestMessageLevel.Warning, $"{testInformation.TestName} failed, retrying...");
            }
        }
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

        testContext.CancellationToken = testLevelCancellationTokenSource.Token;

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
            testInformation.TestMethodArguments);

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
        await _oneTimeSetUpRegistry.GetOrAdd(testDetailsClassType.FullName!, _ => ExecuteOneTimeSetUps(@class, testDetailsClassType));

        var setUpMethods = testDetailsClassType
            .GetMethods()
            .Where(x => !x.IsStatic)
            .Where(x => x.CustomAttributes.Any(attributeData => attributeData.AttributeType == typeof(SetUpAttribute)));

        foreach (var setUpMethod in setUpMethods)
        {
            await _methodInvoker.InvokeMethod(@class, setUpMethod, BindingFlags.Default, null);
        }
    }
    
    private async Task ExecuteCleanUps(object? @class)
    {
        if (@class is null)
        {
            return;
        }
        
        var cleanUpMethods = @class.GetType()
            .GetMethods()
            .Where(x => !x.IsStatic)
            .Where(x => x.CustomAttributes.Any(attributeData => attributeData.AttributeType == typeof(CleanUpAttribute)));

        var exceptions = new List<Exception>();
        
        foreach (var cleanUpMethod in cleanUpMethods)
        {
            try
            {
                await _methodInvoker.InvokeMethod(@class, cleanUpMethod, BindingFlags.Default, null);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        if (exceptions.Count == 1)
        {
            _messageLogger.SendMessage(TestMessageLevel.Error, $"""
                                                               Error running CleanUp:
                                                                  {exceptions.First().Message}
                                                                  {exceptions.First().StackTrace}
                                                               """);
        }
        else if (exceptions.Count > 1)
        {
            var aggregateException = new AggregateException(exceptions);
            _messageLogger.SendMessage(TestMessageLevel.Error, $"""
                                                                Error running CleanUp:
                                                                   {aggregateException.Message}
                                                                   {aggregateException.StackTrace}
                                                                """);
        }
    }

    private async Task ExecuteOneTimeSetUps(object? @class, Type testDetailsClassType)
    {
        var oneTimeSetUpMethods = testDetailsClassType
            .GetMethods()
            .Where(x => x.IsStatic)
            .Where(x => x.CustomAttributes.Any(attributeData => attributeData.AttributeType == typeof(OneTimeSetUpAttribute)));

        foreach (var oneTimeSetUpMethod in oneTimeSetUpMethods)
        {
            await _methodInvoker.InvokeMethod(@class, oneTimeSetUpMethod, BindingFlags.Static | BindingFlags.Public, null);
        }
    }
}