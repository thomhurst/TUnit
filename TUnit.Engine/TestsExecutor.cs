using System.Collections.Concurrent;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Engine.Models;

namespace TUnit.Engine;

internal class TestsExecutor
{
    private int _currentlyExecutingTests;

    private readonly SingleTestExecutor _singleTestExecutor;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly TestGrouper _testGrouper;
    private readonly SystemResourceMonitor _systemResourceMonitor;
    private readonly ConsoleInterceptor _consoleInterceptor;
    private readonly TUnitLogger _logger;
    
    private readonly ConcurrentDictionary<string, Semaphore> _notInParallelKeyedLocks = new();
    private readonly object _notInParallelDictionaryLock = new();

    public TestsExecutor(SingleTestExecutor singleTestExecutor,
        CancellationTokenSource cancellationTokenSource,
        TestGrouper testGrouper,
        SystemResourceMonitor systemResourceMonitor,
        ConsoleInterceptor consoleInterceptor,
        TUnitLogger logger)
    {
        _singleTestExecutor = singleTestExecutor;
        _cancellationTokenSource = cancellationTokenSource;
        _testGrouper = testGrouper;
        _systemResourceMonitor = systemResourceMonitor;
        _consoleInterceptor = consoleInterceptor;
        _logger = logger;
    }

    public async Task ExecuteAsync(IEnumerable<TestInformation> testNodes, ITestExecutionFilter? filter,  TestSessionContext session)
    {
        _consoleInterceptor.Initialize();
        
        var start = DateTimeOffset.Now;

        try
        {
            await AssemblyHookOrchestrators.ExecuteSetups();
        
            var tests = _testGrouper.OrganiseTests(testNodes);
            
            foreach (var test in tests.AllTests)
            {
                if (TestDictionary.TryGetTest(test.TestId, out var matchingTest))
                {
                    ClassHookOrchestrator.RegisterInstance(matchingTest.TestContext.TestInformation.ClassType);
                }
            }

            // These two can run together - We're ensuring same keyed tests don't run together, but no harm in running those alongside tests without a not in parallel constraint
            await Task.WhenAll(
                ProcessTests(tests.Parallel, true, filter, session),
                ProcessKeyedNotInParallelTests(tests.KeyedNotInParallel, filter, session)
            );
        
            // These have to run on their own
            await ProcessTests(tests.NotInParallel, false, filter, session);
        }
        finally
        {
            await AssemblyHookOrchestrators.ExecuteCleanups();
        }
    }

    private async Task ProcessKeyedNotInParallelTests(List<NotInParallelTestCase> testsToProcess,
        ITestExecutionFilter? filter, TestSessionContext session)
    {
        var tasks = testsToProcess
            .OrderBy(x => x.Test.Order)
            .Select(notInParallelTestCase => Task.Run(async () =>
            {
                var keys = notInParallelTestCase.ConstraintKeys;
                
                var locks = keys.Select(GetLockForKey).ToArray();
                
                while (!WaitHandle.WaitAll(locks, TimeSpan.FromMilliseconds(100), false))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                }

                try
                {
                    await ProcessTest(notInParallelTestCase.Test, true, filter, session);
                }
                catch (Exception e)
                {
                    await _logger.LogErrorAsync(e);
                }
                finally
                {
                    foreach (var semaphore in locks)
                    {
                        semaphore.Release();
                    }
                }
            }));

        await Task.WhenAll(tasks);
    }

    private async Task ProcessTests(Queue<TestInformation> queue, bool runInParallel, ITestExecutionFilter? filter,
        TestSessionContext session)
    {
        var executing = new List<Task>();
        
        await foreach (var testWithResult in ProcessQueue(queue, runInParallel, filter, session))
        {
            executing.Add(testWithResult.ResultTask);
        }

        await WhenAllSafely(executing);
    }

    private async IAsyncEnumerable<TestWithResult> ProcessQueue(Queue<TestInformation> queue, bool runInParallel,
        ITestExecutionFilter? filter,
        TestSessionContext session)
    {
        while (queue.Count > 0)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                break;
            }
            
            if (Thread.VolatileRead(ref _currentlyExecutingTests) < 1 || !_systemResourceMonitor.IsSystemStrained())
            {
                var test = queue.Dequeue();

                yield return await ProcessTest(test, runInParallel, filter, session);
            }
            else
            {
                await Task.Delay(500);
            }
        }
    }

    private async Task<TestWithResult> ProcessTest(TestInformation test, bool runInParallel,
        ITestExecutionFilter? filter, TestSessionContext session)
    {
        NotifyTestStart();
        
        var executionTask = _singleTestExecutor.ExecuteTestAsync(test, filter, session);

        _ = executionTask.ContinueWith(_ =>
        {
            NotifyTestEnd();
        });

        if (!runInParallel)
        {
            await executionTask;
        }
        
        return new TestWithResult(test, executionTask);
    }
    
    private Semaphore GetLockForKey(string key)
    {
        lock (_notInParallelDictionaryLock)
        {
            return _notInParallelKeyedLocks.GetOrAdd(key, _ => new Semaphore(1, 1));
        }
    }

    private void NotifyTestEnd()
    {
        Interlocked.Decrement(ref _currentlyExecutingTests);
    }

    private void NotifyTestStart()
    {
        Interlocked.Increment(ref _currentlyExecutingTests);
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
}