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
    private int _currentlyExecutingTests;
    private OneTimeCleanUpTracker _oneTimeCleanupTracker = null!;

    public async Task RunInAsyncContext(IEnumerable<TestCase> testCases)
    {
        var tests = testGrouper.OrganiseTests(testCases);

        var oneTimeCleanUps = new ConcurrentQueue<Task>();

        _oneTimeCleanupTracker = new OneTimeCleanUpTracker(tests.AllTests, (testCase, executingTask) =>
        {
            oneTimeCleanUps.Enqueue(executingTask.ContinueWith(t =>
                ExecuteOneTimeCleanUps(testCase)
            ));
        });
        
        await ProcessTests(tests.Parallel, true, tests.LastTestOfClasses);

        await ProcessKeyedNotInParallelTests(tests);
        
        await ProcessTests(tests.NotInParallel, false, tests.LastTestOfClasses);

        await Task.WhenAll(oneTimeCleanUps);
    }
    
    private async Task ProcessKeyedNotInParallelTests(GroupedTests tests)
    {
        var currentlyExecutingByKeysLock = new object();
        var currentlyExecutingByKeys = new List<(string[] Keys, Task)>();

        var testsToProcess = tests.KeyedNotInParallel;

        while (testsToProcess.Count > 0)
        {
            // Reversing allows us to remove from the collection
            for (var i = testsToProcess.Count - 1; i >= 0; i--)
            {
                var testToProcess = testsToProcess[i];

                var notInParallelKeys =
                    testToProcess.GetPropertyValue(TUnitTestProperties.NotInParallelConstraintKey, Array.Empty<string>());

                if (currentlyExecutingByKeys.Any(x => x.Keys.Intersect(notInParallelKeys).Any()))
                {
                    // There are currently executing tasks with that same 
                    continue;
                }
            
                // Remove from collection as we're now processing it
                testsToProcess.RemoveAt(i);

                var testWithResult = await ProcessTest(testToProcess, true);

                var tuple = (notInParallelKeys, Result: testWithResult.ResultTask);

                lock (currentlyExecutingByKeysLock)
                {
                    currentlyExecutingByKeys.Add(tuple);
                }

                _ = testWithResult.ResultTask.ContinueWith(t =>
                {
                    lock (currentlyExecutingByKeysLock)
                    {
                        return currentlyExecutingByKeys.Remove(tuple);
                    }
                });
            
                _oneTimeCleanupTracker.Remove(testWithResult.Test, ProcessResult(testWithResult));
            }
        }

        await Task.WhenAll(currentlyExecutingByKeys.Select(x => x.Item2));
    }

    private async Task ProcessTests(Queue<TestCase> queue, bool runInParallel, IReadOnlyCollection<TestCase> lastTestsOfClasses)
    {
        await foreach (var testWithResult in ProcessQueue(queue, runInParallel))
        {
            _oneTimeCleanupTracker.Remove(testWithResult.Test, ProcessResult(testWithResult));
        }
    }

    private Task ProcessResult(TestWithResult testWithResult)
    {
        cancellationTokenSource.Token.ThrowIfCancellationRequested();
        
        return testWithResult.ResultTask.ContinueWith(t =>
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
        });
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
        IEnumerable<TestWithResult> executingTests, ConcurrentDictionary<string, Task> oneTimeCleanUpRegistry)
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
            .Select(x => x.ResultTask)
            .ToArray();

        Task.WhenAll(executingTestsForThisClass).ContinueWith(_ =>
            oneTimeCleanUpRegistry.GetOrAdd(processingTestFullyQualifiedClassName,
                ExecuteOneTimeCleanUps(processingTestDetails))
        );
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

                yield return await ProcessTest(test, runInParallel);
            }
            else
            {
                await Task.Delay(500);
            }
        }
    }

    private async Task<TestWithResult> ProcessTest(TestCase test, bool runInParallel)
    {
        Interlocked.Increment(ref _currentlyExecutingTests);

        var executionTask = singleTestExecutor.ExecuteTest(test);

        if (!runInParallel)
        {
            await executionTask;
        }
                
        Interlocked.Decrement(ref _currentlyExecutingTests);
        
        return new TestWithResult(test, executionTask);
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