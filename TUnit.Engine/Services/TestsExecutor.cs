using System.Collections.Concurrent;
using EnumerableAsyncProcessor.Extensions;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Hooks;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

internal class TestsExecutor
{
    private int _currentlyExecutingTests;

    private readonly SingleTestExecutor _singleTestExecutor;
    private readonly TestGrouper _testGrouper;
    private readonly ConsoleInterceptor _consoleInterceptor;
    private readonly TUnitLogger _logger;
    private readonly ICommandLineOptions _commandLineOptions;

    private readonly ConcurrentDictionary<string, Semaphore> _notInParallelKeyedLocks = new();
    private readonly object _notInParallelDictionaryLock = new();
    
    private readonly int _maximumParallelTests;

    public TestsExecutor(SingleTestExecutor singleTestExecutor,
        TestGrouper testGrouper,
        ConsoleInterceptor consoleInterceptor,
        TUnitLogger logger,
        ICommandLineOptions commandLineOptions)
    {
        _singleTestExecutor = singleTestExecutor;
        _testGrouper = testGrouper;
        _consoleInterceptor = consoleInterceptor;
        _logger = logger;
        _commandLineOptions = commandLineOptions;

        _maximumParallelTests = GetMaximumParallelTests();
    }

    public async Task ExecuteAsync(List<DiscoveredTest> testNodes, ITestExecutionFilter? filter,  ExecuteRequestContext context)
    {
        _consoleInterceptor.Initialize();

        try
        {
            await AssemblyHookOrchestrators.ExecuteSetups();

            var tests = _testGrouper.OrganiseTests(testNodes);

            foreach (var test in tests.AllTests)
            {
                ClassHookOrchestrator.RegisterInstance(test.TestContext);
            }

            // These two can run together - We're ensuring same keyed tests don't run together, but no harm in running those alongside tests without a not in parallel constraint
            await Task.WhenAll(
                ProcessParallelTests(tests.Parallel, filter, context),
                ProcessKeyedNotInParallelTests(tests.KeyedNotInParallel, filter, context)
            );

            // These have to run on their own
            await ProcessNotInParallelTests(tests.NotInParallel, filter, context);
        }
        finally
        {
            await AssemblyHookOrchestrators.ExecuteCleanups();
        }
    }

    private async Task ProcessNotInParallelTests(Queue<DiscoveredTest> testsNotInParallel, ITestExecutionFilter? filter, ExecuteRequestContext context)
    {
        foreach (var testInformation in testsNotInParallel)
        {
            await ProcessTest(testInformation, filter, context, context.CancellationToken);
        }
    }

    private async Task ProcessKeyedNotInParallelTests(List<NotInParallelTestCase> testsToProcess,
        ITestExecutionFilter? filter, ExecuteRequestContext context)
    {
        await testsToProcess
            .GroupBy(x => x.ConstraintKeys)
            .ForEachAsync(async group => await Task.Run(() => ProcessGroup(filter, context, group)))
            .ProcessInParallel(_maximumParallelTests);
    }

    private async Task ProcessGroup(ITestExecutionFilter? filter, ExecuteRequestContext context,
        IEnumerable<NotInParallelTestCase> tests)
    {
        foreach (var test in tests.OrderBy(x => x.Test.TestInformation.Order))
        {
            var keys = test.ConstraintKeys;

            var locks = keys.Select(GetLockForKey).ToArray();

            while (!WaitHandle.WaitAll(locks, TimeSpan.FromMilliseconds(100), false))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }

            try
            {
                await ProcessTest(test.Test, filter, context, context.CancellationToken);
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
        }
    }

    private async Task ProcessParallelTests(Queue<DiscoveredTest> queue, ITestExecutionFilter? filter,
        ExecuteRequestContext context)
    {
        await ProcessQueue(queue, filter, context);
    }

    private async Task ProcessQueue(Queue<DiscoveredTest> queue,
        ITestExecutionFilter? filter,
        ExecuteRequestContext context)
    {
        await Parallel.ForEachAsync(queue, new ParallelOptions
        {
            MaxDegreeOfParallelism = GetMaximumParallelTests(),
            CancellationToken = context.CancellationToken
        }, async (test, token) =>
        {
            await ProcessTest(test, filter, context, token);
        });
        
        // while (queue.Count > 0)
        // {
        //     if (_cancellationTokenSource.IsCancellationRequested)
        //     {
        //         break;
        //     }
        //     
        //     if (_currentlyExecutingTests < _maximumParallelTests && !_systemResourceMonitor.IsSystemStrained())
        //     {
        //         var test = queue.Dequeue();
        //
        //         yield return ProcessTest(test, filter, context);
        //     }
        //     else
        //     {
        //         await Task.Delay(500);
        //     }
        // }
    }

    private async Task ProcessTest(DiscoveredTest test,
        ITestExecutionFilter? filter, ExecuteRequestContext context, CancellationToken cancellationToken)
    {
        NotifyTestStart();

        try
        {
            await Task.Run(() => _singleTestExecutor.ExecuteTestAsync(test, filter, context), cancellationToken);
        }
        catch
        {
            // Ignored
        }
        finally
        {
            NotifyTestEnd();
        }
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

    private int GetMaximumParallelTests()
    {
        if (_commandLineOptions.TryGetOptionArgumentList(MaximumParallelTestsCommandProvider.MaximumParallelTests,
                out var values))
        {
            return int.Parse(values[0]);
        }

        return int.MaxValue;
    }
}