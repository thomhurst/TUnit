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
{
    private int _currentlyExecutingTests;
    private OneTimeCleanUpTracker _oneTimeCleanupTracker = null!;
    
    private readonly ConcurrentDictionary<CachedAssemblyInformation, SemaphoreSlim> _semaphoreSlimByAssembly = new();

    private readonly SingleTestExecutor _singleTestExecutor;
    private readonly MethodInvoker _methodInvoker;
    private readonly ITestExecutionRecorder _testExecutionRecorder;
    private readonly CacheableAssemblyLoader _assemblyLoader;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly TestGrouper _testGrouper;
    private readonly SystemResourceMonitor _systemResourceMonitor;
    private readonly CacheableAssemblyLoader _cacheableAssemblyLoader;
    private readonly ClassWalker _classWalker;
    private readonly ConsoleInterceptor _consoleInterceptor;
    private readonly AssemblySetUpExecutor _assemblySetUpExecutor;
    private readonly AssemblyCleanUpExecutor _assemblyCleanUpExecutor;

    public AsyncTestRunExecutor(SingleTestExecutor singleTestExecutor, 
        MethodInvoker methodInvoker, 
        ITestExecutionRecorder testExecutionRecorder,
        CacheableAssemblyLoader assemblyLoader,
        CancellationTokenSource cancellationTokenSource,
        TestGrouper testGrouper,
        SystemResourceMonitor systemResourceMonitor,
        CacheableAssemblyLoader cacheableAssemblyLoader,
        ClassWalker classWalker,
        ConsoleInterceptor consoleInterceptor,
        AssemblySetUpExecutor assemblySetUpExecutor,
        AssemblyCleanUpExecutor assemblyCleanUpExecutor)
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
        _consoleInterceptor = consoleInterceptor;
        _assemblySetUpExecutor = assemblySetUpExecutor;
        _assemblyCleanUpExecutor = assemblyCleanUpExecutor;
    }

    public async Task RunInAsyncContext(IEnumerable<TestCase> testCases)
    {
        _consoleInterceptor.Initialize();
        
        var tests = _testGrouper.OrganiseTests(testCases);
        
        _singleTestExecutor.SetAllTests(tests);

        var oneTimeCleanUps = new ConcurrentQueue<Task>();

        _oneTimeCleanupTracker = new OneTimeCleanUpTracker(tests.AllTests, (testCase, executingTask) =>
        {
            oneTimeCleanUps.Enqueue(ExecuteOneTimeCleanUps(testCase));
        });
        
        await ProcessTests(tests.Parallel, true);

        await ProcessKeyedNotInParallelTests(tests.KeyedNotInParallel);
        
        await ProcessTests(tests.NotInParallel, false);
        
        foreach (var cachedAssemblyInformation in _cacheableAssemblyLoader.CachedAssemblies)
        {
            oneTimeCleanUps.Enqueue(_assemblyCleanUpExecutor.ExecuteCleanUps(cachedAssemblyInformation));
        }

        await WhenAllSafely(oneTimeCleanUps, _testExecutionRecorder);
    }

    private async Task ProcessKeyedNotInParallelTests(List<TestCase> testsToProcess)
    {
        var currentlyExecutingByKeysLock = new object();
        var currentlyExecutingByKeys = new List<(ConstraintKeysCollection Keys, Task)>();

        var executing = new List<Task>();

        var orderedKeyedTests = testsToProcess
            .Where(x => x.GetPropertyValue(TUnitTestProperties.Order, int.MaxValue) != int.MaxValue)
            .Where(x => x.GetConstraintKeys().Count > 0);
        
        foreach (var group in orderedKeyedTests.GroupBy(x => x.GetConstraintKeys()))
        {
            executing.Add(Task.Run(async () =>
            {
                foreach (var testToProcess in group.OrderBy(x => x.GetPropertyValue(TUnitTestProperties.Order, int.MaxValue)))
                {
                    testsToProcess.Remove(testToProcess);

                    var testWithResult = await ProcessTest(testToProcess, true);

                    _oneTimeCleanupTracker.Remove(testWithResult.Test, testWithResult.ResultTask);

                    await testWithResult.ResultTask;
                }
            }));
        }

        await WhenAllSafely(executing, _testExecutionRecorder);
        
        executing.Clear();

        while (testsToProcess.Count > 0)
        {
            // Reversing allows us to remove from the collection
            for (var i = testsToProcess.Count - 1; i >= 0; i--)
            {
                var testToProcess = testsToProcess[i];

                var notInParallelKeys = testToProcess.GetConstraintKeys();

                lock (currentlyExecutingByKeysLock)
                {
                    if (currentlyExecutingByKeys.Any(x => x.Keys == notInParallelKeys))
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
        var cachedAssemblyInformation = _cacheableAssemblyLoader.GetOrLoadAssembly(test.Source);
        
        var semaphoreSlim = _semaphoreSlimByAssembly.GetOrAdd(cachedAssemblyInformation, GetSemaphoreSlim);
        await semaphoreSlim.WaitAsync();
        
        Interlocked.Increment(ref _currentlyExecutingTests);

        try
        {
            await _assemblySetUpExecutor.ExecuteSetUps(cachedAssemblyInformation);

        }
        catch
        {
            semaphoreSlim.Release();
            Interlocked.Decrement(ref _currentlyExecutingTests);
            throw;
        }
        
        var executionTask = _singleTestExecutor.ExecuteTest(test);

        _ = executionTask.ContinueWith(_ =>
        {
            Interlocked.Decrement(ref _currentlyExecutingTests);
            return semaphoreSlim.Release();
        });

        if (!runInParallel)
        {
            await executionTask;
        }
        
        return new TestWithResult(test, executionTask);
    }

    private async Task ExecuteOneTimeCleanUps(TestCase testDetails)
    {
        var cachedAssemblyInformation = _assemblyLoader.GetOrLoadAssembly(testDetails.Source);
        
        var classType =
            cachedAssemblyInformation.Assembly.GetType(testDetails.GetPropertyValue(TUnitTestProperties.AssemblyQualifiedClassName, ""));

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
    
    private SemaphoreSlim GetSemaphoreSlim(CachedAssemblyInformation? cachedAssemblyInformation)
    {
        var maximumConcurrentTestsAttribute =
            cachedAssemblyInformation?.Assembly.GetCustomAttribute<MaximumConcurrentTestsAttribute>();

        var maximumConcurrentTests = maximumConcurrentTestsAttribute?.MaximumConcurrentTests
                                     ?? Environment.ProcessorCount * Environment.ProcessorCount;

        return new SemaphoreSlim(maximumConcurrentTests, maximumConcurrentTests);
    }
}