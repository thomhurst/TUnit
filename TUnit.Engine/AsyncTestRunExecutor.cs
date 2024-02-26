using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Engine.Extensions;
using TUnit.Engine.Models;
using TUnit.Engine.Models.Properties;

namespace TUnit.Engine;

internal class AsyncTestRunExecutor
{
    private int _currentlyExecutingTests;
    private OneTimeCleanUpTracker _oneTimeCleanupTracker = null!;
    
    private readonly ConcurrentDictionary<CachedAssemblyInformation, SemaphoreSlim> _semaphoreSlimByAssembly = new();

    private readonly SingleTestExecutor _singleTestExecutor;
    private readonly MethodInvoker _methodInvoker;
    private readonly CacheableAssemblyLoader _assemblyLoader;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly TestGrouper _testGrouper;
    private readonly SystemResourceMonitor _systemResourceMonitor;
    private readonly CacheableAssemblyLoader _cacheableAssemblyLoader;
    private readonly ClassWalker _classWalker;
    private readonly ConsoleInterceptor _consoleInterceptor;
    private readonly AssemblySetUpExecutor _assemblySetUpExecutor;
    private readonly AssemblyCleanUpExecutor _assemblyCleanUpExecutor;
    private readonly ILogger<AsyncTestRunExecutor> _logger;

    public AsyncTestRunExecutor(SingleTestExecutor singleTestExecutor, 
        MethodInvoker methodInvoker,
        CacheableAssemblyLoader assemblyLoader,
        CancellationTokenSource cancellationTokenSource,
        TestGrouper testGrouper,
        SystemResourceMonitor systemResourceMonitor,
        CacheableAssemblyLoader cacheableAssemblyLoader,
        ClassWalker classWalker,
        ConsoleInterceptor consoleInterceptor,
        AssemblySetUpExecutor assemblySetUpExecutor,
        AssemblyCleanUpExecutor assemblyCleanUpExecutor,
        ILoggerFactory loggerFactory)
    {
        _singleTestExecutor = singleTestExecutor;
        _methodInvoker = methodInvoker;
        _assemblyLoader = assemblyLoader;
        _cancellationTokenSource = cancellationTokenSource;
        _testGrouper = testGrouper;
        _systemResourceMonitor = systemResourceMonitor;
        _cacheableAssemblyLoader = cacheableAssemblyLoader;
        _classWalker = classWalker;
        _consoleInterceptor = consoleInterceptor;
        _assemblySetUpExecutor = assemblySetUpExecutor;
        _assemblyCleanUpExecutor = assemblyCleanUpExecutor;
        _logger = loggerFactory.CreateLogger<AsyncTestRunExecutor>();
    }

    public async Task RunInAsyncContext(IEnumerable<TestNode> testCases, TestSessionContext session)
    {
        _consoleInterceptor.Initialize();
        
        var tests = _testGrouper.OrganiseTests(testCases);
        
        _singleTestExecutor.SetAllTests(tests);

        var oneTimeCleanUps = new ConcurrentQueue<Task>();

        _oneTimeCleanupTracker = new OneTimeCleanUpTracker(tests.AllTests, (testNode, executingTask) =>
        {
            oneTimeCleanUps.Enqueue(ExecuteOneTimeCleanUps(testNode));
        });
        
        await ProcessTests(tests.Parallel, true, session);

        await ProcessKeyedNotInParallelTests(tests.KeyedNotInParallel, session);
        
        await ProcessTests(tests.NotInParallel, false, session);
        
        foreach (var cachedAssemblyInformation in _cacheableAssemblyLoader.CachedAssemblies)
        {
            oneTimeCleanUps.Enqueue(_assemblyCleanUpExecutor.ExecuteCleanUps(cachedAssemblyInformation));
        }

        await WhenAllSafely(oneTimeCleanUps);
    }

    private async Task ProcessKeyedNotInParallelTests(List<NotInParallelTestCase> testsToProcess, TestSessionContext session)
    {
        while (testsToProcess.Count > 0)
        {
            var executing = new List<Task>();
            var testsToRemove = new List<NotInParallelTestCase>();

            foreach (var notInParallelTestCases in GetOrderedTests())
            {
                var notInParallelTestCase = notInParallelTestCases.First();

                testsToRemove.Add(notInParallelTestCase);

                var testWithResult = await ProcessTest(notInParallelTestCase.TestNode, true, session);

                _oneTimeCleanupTracker.Remove(testWithResult.Test, testWithResult.ResultTask);

                executing.Add(testWithResult.ResultTask);
            }

            await WhenAllSafely(executing);

            foreach (var notInParallelTestCase in testsToRemove)
            {
                testsToProcess.Remove(notInParallelTestCase);
            }

            List<IGrouping<ConstraintKeysCollection, NotInParallelTestCase>> GetOrderedTests()
            {
                return testsToProcess
                    .OrderBy(x => x.TestNode.GetRequiredProperty<OrderProperty>().Order)
                    .GroupBy(x => x.TestNode.GetRequiredProperty<NotInParallelConstraintKeysProperty>().ConstraintKeys!)
                    .ToList();
            }
        }
    }

    private async Task ProcessTests(Queue<TestNode> queue, bool runInParallel, TestSessionContext session)
    {
        var executing = new List<Task>();
        
        await foreach (var testWithResult in ProcessQueue(queue, runInParallel, session))
        {
            executing.Add(testWithResult.ResultTask);
            
            _oneTimeCleanupTracker.Remove(testWithResult.Test, testWithResult.ResultTask);
        }

        await WhenAllSafely(executing);
    }

    private async IAsyncEnumerable<TestWithResult> ProcessQueue(Queue<TestNode> queue, bool runInParallel,
        TestSessionContext session)
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

                yield return await ProcessTest(test, runInParallel, session);
            }
            else
            {
                await Task.Delay(500);
            }
        }
    }

    private async Task<TestWithResult> ProcessTest(TestNode test, bool runInParallel, TestSessionContext session)
    {
        var cachedAssemblyInformation = _cacheableAssemblyLoader.GetOrLoadAssembly(test.GetRequiredProperty<AssemblyProperty>().FullyQualifiedAssembly);
        
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
        
        var executionTask = _singleTestExecutor.ExecuteTest(test, session);

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

    private async Task ExecuteOneTimeCleanUps(TestNode testDetails)
    {
        var cachedAssemblyInformation = _assemblyLoader.GetOrLoadAssembly(testDetails.GetRequiredProperty<AssemblyProperty>().FullyQualifiedAssembly);
        
        var classType =
            cachedAssemblyInformation.Assembly.GetType(testDetails.GetRequiredProperty<AssemblyProperty>().FullyQualifiedAssembly);

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

    private async Task WhenAllSafely(IEnumerable<Task> tasks)
    {
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            await _logger.LogErrorAsync(e);
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