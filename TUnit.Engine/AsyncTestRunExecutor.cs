using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TUnit.Core;
using TUnit.Engine.Extensions;
using TUnit.Engine.Models;

namespace TUnit.Engine;

internal class AsyncTestRunExecutor
    (
        SingleTestExecutor singleTestExecutor, 
        MethodInvoker methodInvoker, 
        ITestExecutionRecorder testExecutionRecorder,
        CacheableAssemblyLoader assemblyLoader,
        CancellationTokenSource cancellationTokenSource,
        TestGrouper testGrouper,
        SystemResourceMonitor systemResourceMonitor
        )
{
    private readonly ConcurrentDictionary<string, Task> _oneTimeCleanUpRegistry = new();
    private readonly List<Task> _setResultsTasks = [];
    
    private int _currentlyExecutingTests;

    public async Task RunInAsyncContext(IEnumerable<TestCase> testCases)
    {
        var tests = testGrouper.OrganiseTests(testCases);
        
        await ProcessTests(tests.Parallel, true, tests.LastTestOfClasses);

        await Task.WhenAll(tests.KeyedNotInParallel
            .Select(keyedTestsGroup =>
                ProcessTests(keyedTestsGroup.ToQueue(), false, tests.LastTestOfClasses))
        );
        
        await ProcessTests(tests.NotInParallel, false, tests.LastTestOfClasses);
    }

    private async Task ProcessTests(Queue<TestCase> queue, bool runInParallel, IReadOnlyCollection<TestCase> lastTestsOfClasses)
    {
        var executingTests = new List<TestWithResult>();
        
        await foreach (var testWithResult in ProcessQueue(queue, runInParallel))
        {
            cancellationTokenSource.Token.ThrowIfCancellationRequested();
            
            executingTests.Add(testWithResult);

            SetupRunOneTimeCleanUpForClass(testWithResult.Test, lastTestsOfClasses, executingTests);
            
            executingTests.RemoveAll(x => x.Result.IsCompletedSuccessfully);
            
            _setResultsTasks.Add(testWithResult.Result.ContinueWith(t =>
            {
                var result = t.Result;
                var testDetails = testWithResult.Test;
                
                testExecutionRecorder.RecordResult(new TestResult(testWithResult.Test)
                {
                    DisplayName = testDetails.DisplayName,
                    Outcome = GetOutcome(result.Status),
                    ComputerName = result.ComputerName,
                    Duration = result.Duration,
                    StartTime = result.Start,
                    EndTime = result.End,
                    Messages = { new TestResultMessage("Output", result.Output) },
                    ErrorMessage = result.Exception?.Message,
                    ErrorStackTrace = result.Exception?.StackTrace,
                });
            }));
        }
        
        executingTests.RemoveAll(x => x.Result.IsCompletedSuccessfully);

        await WhenAllSafely(executingTests.Select(x => x.Result), testExecutionRecorder);
        await WhenAllSafely(_oneTimeCleanUpRegistry.Values, testExecutionRecorder);
        await Task.WhenAll(_setResultsTasks);
    }

    private TestOutcome GetOutcome(Status resultStatus)
    {
        return resultStatus switch
        {
            Status.None => TestOutcome.None,
            Status.Passed => TestOutcome.Passed,
            Status.Failed => TestOutcome.Failed,
            Status.Skipped => TestOutcome.Skipped,
            _ => throw new ArgumentOutOfRangeException(nameof(resultStatus), resultStatus, null)
        };
    }

    private void SetupRunOneTimeCleanUpForClass(TestCase processingTestDetails,
        IEnumerable<TestCase> allTestsOrderedByClass,
        IEnumerable<TestWithResult> executingTests)
    {
        var processingTestFullyQualifiedClassName =
            processingTestDetails.GetPropertyValue(TUnitTestProperties.AssemblyQualifiedClassName, "");
        
        var lastTestForClass = allTestsOrderedByClass.Last(x =>
            x.GetPropertyValue(TUnitTestProperties.AssemblyQualifiedClassName, "") == processingTestFullyQualifiedClassName);

        if (processingTestDetails.GetPropertyValue(TUnitTestProperties.UniqueId, "") != lastTestForClass.GetPropertyValue(TUnitTestProperties.UniqueId, ""))
        {
            return;
        }

        var executingTestsForThisClass = executingTests
            .Where(x => x.Test.GetPropertyValue(TUnitTestProperties.AssemblyQualifiedClassName, "") == processingTestFullyQualifiedClassName)
            .Select(x => x.Result)
            .ToArray();

        Task.WhenAll(executingTestsForThisClass).ContinueWith(x =>
        {
            _ = _oneTimeCleanUpRegistry.GetOrAdd(processingTestFullyQualifiedClassName,
                ExecuteOneTimeCleanUps(processingTestDetails));
            
            return Task.CompletedTask;
        });
    }

    private async IAsyncEnumerable<TestWithResult> ProcessQueue(Queue<TestCase> queue, bool runInParallel)
    {
        while (queue.Count > 0)
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                break;
            }
            
            if (_currentlyExecutingTests < 1 || !systemResourceMonitor.IsSystemStrained())
            {
                var test = queue.Dequeue();

                Interlocked.Increment(ref _currentlyExecutingTests);

                var executionTask = singleTestExecutor.ExecuteTest(test);

                if (!runInParallel)
                {
                    await executionTask;
                }
                
                Interlocked.Decrement(ref _currentlyExecutingTests);
                
                yield return new TestWithResult(test, executionTask);
            }
            else
            {
                await Task.Delay(500);
            }
        }
    }
    
    private async Task ExecuteOneTimeCleanUps(TestCase testDetails)
    {
        var assembly = assemblyLoader.GetOrLoadAssembly(testDetails.Source);
        
        var classType =
            assembly?.GetType(testDetails.GetPropertyValue(TUnitTestProperties.AssemblyQualifiedClassName, ""));

        if (classType is null)
        {
            return;
        }
        
        var oneTimeCleanUpMethods = classType
            .GetMethods()
            .Where(x => x.IsStatic)
            .Where(x => x.CustomAttributes.Any(attributeData => attributeData.AttributeType == typeof(OneTimeCleanUpAttribute)));

        foreach (var oneTimeCleanUpMethod in oneTimeCleanUpMethods)
        {
            await methodInvoker.InvokeMethod(null, oneTimeCleanUpMethod, BindingFlags.Static | BindingFlags.Public, null);
        }
    }

    private async Task WhenAllSafely(IEnumerable<Task> tasks, IMessageLogger? messageLogger)
    {
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            messageLogger?.SendMessage(TestMessageLevel.Error, e.ToString());
        }
    }
}