using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core;
using TimeoutException = TUnit.Core.Exceptions.TimeoutException;

namespace TUnit.Engine;

internal class SingleTestExecutor
{
    private readonly MethodInvoker _methodInvoker;
    private readonly TestClassCreator _testClassCreator;
    private readonly Disposer _disposer;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public SingleTestExecutor(MethodInvoker methodInvoker, 
        TestClassCreator testClassCreator,
        Disposer disposer,
        CancellationTokenSource cancellationTokenSource)
    {
        _methodInvoker = methodInvoker;
        _testClassCreator = testClassCreator;
        _disposer = disposer;
        _cancellationTokenSource = cancellationTokenSource;
    }
    
    private readonly ConcurrentDictionary<string, Task> _oneTimeSetUpRegistry = new();

    public async Task<TUnitTestResultWithDetails> ExecuteTest(TestDetails testDetails, Type[] allClasses)
    {
        var start = DateTimeOffset.Now;

        if (testDetails.IsSkipped)
        {
            return testDetails.SetResult(new TUnitTestResultWithDetails
            {
                TestDetails = testDetails,
                Duration = TimeSpan.Zero,
                Start = start,
                End = start,
                ComputerName = Environment.MachineName,
                Exception = null,
                Status = Status.Skipped
            });
        }
        
        object? @class = null;
        TestContext? testContext = null;
        try
        {
            await Task.Run(async () =>
            {
                @class = _testClassCreator.CreateTestClass(testDetails, allClasses);

                testContext = new TestContext(testDetails, @class);
                TestContext.Current = testContext;

                try
                { 
                    await ExecuteWithRetries(testContext, testDetails, @class);
                }
                finally
                {
                    await _disposer.DisposeAsync(@class);
                }
            });

            var end = DateTimeOffset.Now;

            return testDetails.SetResult(new TUnitTestResultWithDetails
            {
                TestDetails = testDetails,
                Duration = end - start,
                Start = start,
                End = end,
                ComputerName = Environment.MachineName,
                Exception = null,
                Status = Status.Passed,
                Output = testContext?.GetOutput()
            });
        }
        catch (Exception e)
        {
            var end = DateTimeOffset.Now;

            var unitTestResult = new TUnitTestResultWithDetails
            {
                TestDetails = testDetails,
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
            
            await ExecuteCleanUps(@class);
            
            return testDetails.SetResult(unitTestResult);
        }
    }

    private async Task ExecuteWithRetries(TestContext testContext, TestDetails testDetails, object? @class)
    {
        for (var i = 0; i < testDetails.RetryCount + 1; i++)
        {
            try
            {
                await ExecuteCore(testContext, testDetails, @class);
                break;
            }
            catch
            {
                if (i == testDetails.RetryCount)
                {
                    throw;
                }
            }
        }
    }

    private async Task ExecuteCore(TestContext testContext, TestDetails testDetails, object? @class)
    {
        testDetails.CurrentExecutionCount++;
        
        await ExecuteSetUps(@class, testDetails.ClassType);

        var testLevelCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);

        if (testDetails.Timeout != default)
        {
            testLevelCancellationTokenSource.CancelAfter(testDetails.Timeout);
        }

        testContext.CancellationToken = testLevelCancellationTokenSource.Token;

        try
        {
            await ExecuteTestMethodWithTimeout(testDetails, @class, testLevelCancellationTokenSource);
        }
        catch
        {
            testLevelCancellationTokenSource.Cancel();
            throw;
        }
    }

    private async Task ExecuteTestMethodWithTimeout(TestDetails testDetails, object? @class,
        CancellationTokenSource cancellationTokenSource)
    {
        var methodResult = _methodInvoker.InvokeMethod(@class, testDetails.MethodInfo, BindingFlags.Default,
            testDetails.ArgumentValues?.ToArray());

        if (testDetails.Timeout == default)
        {
            await methodResult;
            return;
        }
        
        var timeoutTask = Task.Delay(testDetails.Timeout, cancellationTokenSource.Token)
            .ContinueWith(_ => throw new TimeoutException(testDetails));

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
        
        if (exceptions.Any())
        {
            throw new AggregateException(exceptions);
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