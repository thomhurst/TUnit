using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TUnit.Core;
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
            oneTimeCleanUps.Enqueue(ExecuteOneTimeCleanUps(testCase));
        });
        
        await ProcessTests(tests.Parallel, true);

        await ProcessKeyedNotInParallelTests(tests.KeyedNotInParallel);
        
        await ProcessTests(tests.NotInParallel, false);

        await WhenAllSafely(oneTimeCleanUps, testExecutionRecorder);
    }
    
    private async Task ProcessKeyedNotInParallelTests(List<TestCase> testsToProcess)
    {
        var currentlyExecutingByKeysLock = new object();
        var currentlyExecutingByKeys = new List<(string[] Keys, Task)>();

        var executing = new List<Task>();
        
        while (testsToProcess.Count > 0)
        {
            // Reversing allows us to remove from the collection
            for (var i = testsToProcess.Count - 1; i >= 0; i--)
            {
                var testToProcess = testsToProcess[i];

                var notInParallelKeys =
                    testToProcess.GetPropertyValue(TUnitTestProperties.NotInParallelConstraintKeys, Array.Empty<string>());

                lock (currentlyExecutingByKeysLock)
                {
                    if (currentlyExecutingByKeys.Any(x => x.Keys.Intersect(notInParallelKeys).Any()))
                    {
                        // There are currently executing tasks with that same 
                        continue;
                    }
                }

                // Remove from collection as we're now processing it
                testsToProcess.RemoveAt(i);

                var testWithResult = await ProcessTest(testToProcess, true);

                var tuple = (notInParallelKeys, testWithResult.ResultTask);

                lock (currentlyExecutingByKeysLock)
                {
                    currentlyExecutingByKeys.Add(tuple);
                }

                _ = testWithResult.ResultTask.ContinueWith(_ =>
                {
                    lock (currentlyExecutingByKeysLock)
                    {
                        return currentlyExecutingByKeys.Remove(tuple);
                    }
                });
                
                executing.Add(testWithResult.ResultTask);
            
                _oneTimeCleanupTracker.Remove(testWithResult.Test, testWithResult.ResultTask);
            }
        }

        await WhenAllSafely(executing, testExecutionRecorder);
    }

    private async Task ProcessTests(Queue<TestCase> queue, bool runInParallel)
    {
        var executing = new List<Task>();
        
        await foreach (var testWithResult in ProcessQueue(queue, runInParallel))
        {
            executing.Add(testWithResult.ResultTask);
            
            _oneTimeCleanupTracker.Remove(testWithResult.Test, testWithResult.ResultTask);
        }

        await WhenAllSafely(executing, testExecutionRecorder);
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
            .Where(x => x.GetCustomAttributes<OneTimeCleanUpAttribute>().Any());

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