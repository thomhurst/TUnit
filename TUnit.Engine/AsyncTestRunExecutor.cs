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

    private async Task ProcessKeyedNotInParallelTests(List<NotInParallelTestCase> testsToProcess)
    {
        while (testsToProcess.Count > 0)
        {
            var executing = new List<Task>();
            var testsToRemove = new List<NotInParallelTestCase>();

            foreach (var notInParallelTestCases in GetOrderedTests())
            {
                var notInParallelTestCase = notInParallelTestCases.First();

                testsToProcess.Add(notInParallelTestCase);

                var testWithResult = await ProcessTest(notInParallelTestCase.TestCase, true);

                _oneTimeCleanupTracker.Remove(testWithResult.Test, testWithResult.ResultTask);

                executing.Add(testWithResult.ResultTask);
            }

            await WhenAllSafely(executing, _testExecutionRecorder);

            foreach (var notInParallelTestCase in testsToRemove)
            {
                testsToProcess.Remove(notInParallelTestCase);
            }

            List<IGrouping<ConstraintKeysCollection, NotInParallelTestCase>> GetOrderedTests()
            {
                return testsToProcess
                    .OrderBy(x => x.TestCase.GetPropertyValue(TUnitTestProperties.Order, int.MaxValue))
                    .GroupBy(x => x.ConstraintKeys)
                    .ToList();
            }
        }
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
        
        await NotifyTestStart(semaphoreSlim);

        try
        {
            await _assemblySetUpExecutor.ExecuteSetUps(cachedAssemblyInformation);
        }
        catch
        {
            NotifyTestEnd(semaphoreSlim);
            throw;
        }
        
        var executionTask = _singleTestExecutor.ExecuteTest(test);

        _ = executionTask.ContinueWith(_ =>
        {
            NotifyTestEnd(semaphoreSlim);
        });

        if (!runInParallel)
        {
            await executionTask;
        }
        
        return new TestWithResult(test, executionTask);
    }

    private void NotifyTestEnd(SemaphoreSlim semaphoreSlim)
    {
        Interlocked.Decrement(ref _currentlyExecutingTests);
        semaphoreSlim.Release();
    }

    private async Task NotifyTestStart(SemaphoreSlim semaphoreSlim)
    {
        await semaphoreSlim.WaitAsync();
        Interlocked.Increment(ref _currentlyExecutingTests);
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