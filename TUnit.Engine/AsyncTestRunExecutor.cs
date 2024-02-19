using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TUnit.Core;
using TUnit.Engine.Models;

namespace TUnit.Engine;

internal class AsyncTestRunExecutor
{
    private int _currentlyExecutingTests;
    private OneTimeCleanUpTracker _oneTimeCleanupTracker = null!;
    
    private readonly ConcurrentDictionary<Assembly, SemaphoreSlim> _semaphoreSlimByAssembly = new();

    private readonly SingleTestExecutor _singleTestExecutor;
    private readonly MethodInvoker _methodInvoker;
    private readonly ITestExecutionRecorder _testExecutionRecorder;
    private readonly CacheableAssemblyLoader _assemblyLoader;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly TestGrouper _testGrouper;
    private readonly SystemResourceMonitor _systemResourceMonitor;
    private readonly CacheableAssemblyLoader _cacheableAssemblyLoader;
    private readonly ClassWalker _classWalker;

    public AsyncTestRunExecutor(SingleTestExecutor singleTestExecutor, 
        MethodInvoker methodInvoker, 
        ITestExecutionRecorder testExecutionRecorder,
        CacheableAssemblyLoader assemblyLoader,
        CancellationTokenSource cancellationTokenSource,
        TestGrouper testGrouper,
        SystemResourceMonitor systemResourceMonitor,
        CacheableAssemblyLoader cacheableAssemblyLoader,
        ClassWalker classWalker)
    {
        _singleTestExecutor = singleTestExecutor;
        _methodInvoker = methodInvoker;
        _testExecutionRecorder = testExecutionRecorder;
        _assemblyLoader = assemblyLoader;
        _cancellationTokenSource = cancellationTokenSource;
        _testGrouper = testGrouper;
        _systemResourceMonitor = systemResourceMonitor;
        _cacheableAssemblyLoader = cacheableAssemblyLoader;
        _classWalker = classWalker;
    }

    public async Task RunInAsyncContext(IEnumerable<TestCase> testCases)
    {
        var tests = _testGrouper.OrganiseTests(testCases);

        var oneTimeCleanUps = new ConcurrentQueue<Task>();

        _oneTimeCleanupTracker = new OneTimeCleanUpTracker(tests.AllTests, (testCase, executingTask) =>
        {
            oneTimeCleanUps.Enqueue(ExecuteOneTimeCleanUps(testCase));
        });
        
        await ProcessTests(tests.Parallel, true);

        await ProcessKeyedNotInParallelTests(tests.KeyedNotInParallel);
        
        await ProcessTests(tests.NotInParallel, false);

        await WhenAllSafely(oneTimeCleanUps, _testExecutionRecorder);
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

        await WhenAllSafely(executing, _testExecutionRecorder);
    }

    private async Task ProcessTests(Queue<TestCase> queue, bool runInParallel)
    {
        var executing = new List<Task>();
        
        await foreach (var testWithResult in ProcessQueue(queue, runInParallel))
        {
            executing.Add(testWithResult.ResultTask);
            
            _oneTimeCleanupTracker.Remove(testWithResult.Test, testWithResult.ResultTask);
        }

        await WhenAllSafely(executing, _testExecutionRecorder);
    }

    private async IAsyncEnumerable<TestWithResult> ProcessQueue(Queue<TestCase> queue, bool runInParallel)
    {
        while (queue.Count > 0)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                break;
            }
            
            if (_currentlyExecutingTests < 1 || !_systemResourceMonitor.IsSystemStrained())
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
        var testAssembly = _cacheableAssemblyLoader.GetOrLoadAssembly(test.Source);
        var semaphoreSlim = _semaphoreSlimByAssembly.GetOrAdd(testAssembly ?? Assembly.GetCallingAssembly(), GetSemaphoreSlim);
        await semaphoreSlim.WaitAsync();
        
        Interlocked.Increment(ref _currentlyExecutingTests);

        var executionTask = _singleTestExecutor.ExecuteTest(test);

        _ = executionTask.ContinueWith(_ => semaphoreSlim.Release());

        if (!runInParallel)
        {
            await executionTask;
        }
                
        Interlocked.Decrement(ref _currentlyExecutingTests);
        
        return new TestWithResult(test, executionTask);
    }

    private async Task ExecuteOneTimeCleanUps(TestCase testDetails)
    {
        var assembly = _assemblyLoader.GetOrLoadAssembly(testDetails.Source);
        
        var classType =
            assembly?.GetType(testDetails.GetPropertyValue(TUnitTestProperties.AssemblyQualifiedClassName, ""));

        if (classType is null)
        {
            return;
        }
        
        var oneTimeCleanUpMethods = _classWalker.GetSelfAndBaseTypes(classType)
            .SelectMany(x => x.GetMethods())
            .Where(x => x.IsStatic)
            .Where(x => x.GetCustomAttributes<OnlyOnceCleanUpAttribute>().Any());

        foreach (var oneTimeCleanUpMethod in oneTimeCleanUpMethods)
        {
            await _methodInvoker.InvokeMethod(null, oneTimeCleanUpMethod, BindingFlags.Static | BindingFlags.Public, null, default);
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
    
    private SemaphoreSlim GetSemaphoreSlim(Assembly? assembly)
    {
        var maximumConcurrentTestsAttribute =
            assembly?.GetCustomAttribute<MaximumConcurrentTestsAttribute>();

        var maximumConcurrentTests = maximumConcurrentTestsAttribute?.MaximumConcurrentTests
                                     ?? Environment.ProcessorCount * Environment.ProcessorCount;

        return new SemaphoreSlim(maximumConcurrentTests, maximumConcurrentTests);
    }
}