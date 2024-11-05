﻿using System.Collections.Concurrent;
using EnumerableAsyncProcessor.Extensions;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Extensions;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

internal class TestsExecutor
{
    private int _currentlyExecutingTests;

    private readonly SingleTestExecutor _singleTestExecutor;
    private readonly TUnitFrameworkLogger _logger;
    private readonly ICommandLineOptions _commandLineOptions;
    private readonly EngineCancellationToken _engineCancellationToken;
    
    private readonly Counter _executionCounter = new();
    private readonly TaskCompletionSource _onFinished = new();

    private readonly ConcurrentDictionary<string, Semaphore> _notInParallelKeyedLocks = new();
#if NET9_0_OR_GREATER
    private readonly Lock _notInParallelDictionaryLock = new();
#else
    private readonly object _notInParallelDictionaryLock = new();
#endif
    
    private readonly int _maximumParallelTests;

    public TestsExecutor(SingleTestExecutor singleTestExecutor,
        TUnitFrameworkLogger logger,
        ICommandLineOptions commandLineOptions,
        EngineCancellationToken engineCancellationToken)
    {
        _singleTestExecutor = singleTestExecutor;
        _logger = logger;
        _commandLineOptions = commandLineOptions;
        _engineCancellationToken = engineCancellationToken;

        _maximumParallelTests = GetParallelTestsLimit();

        _executionCounter.OnCountChanged += (sender, count) =>
        {
            if (count == 0)
            {
                _onFinished.TrySetResult();
            }
        };
    }

    public async Task ExecuteAsync(GroupedTests tests, ITestExecutionFilter? filter,  ExecuteRequestContext context)
    {
        await using var _ = _engineCancellationToken.Token.Register(() => _onFinished.TrySetCanceled());

        try
        {
            _executionCounter.Increment();
            
            await ProcessParallelTests(tests.Parallel, filter, context);

            await ProcessParallelGroups(tests.ParallelGroups, filter, context);

            await ProcessKeyedNotInParallelTests(tests.KeyedNotInParallel, filter, context);

            await ProcessNotInParallelTests(tests.NotInParallel, filter, context);
        }
        finally
        {
            _executionCounter.Decrement();
        }
    }
    
    public Task WaitForFinishAsync() => _onFinished.Task;

    private async Task ProcessNotInParallelTests(PriorityQueue<DiscoveredTest, int> testsNotInParallel, ITestExecutionFilter? filter, ExecuteRequestContext context)
    {
        while (testsNotInParallel.TryDequeue(out var testInformation, out _))
        {
            await ProcessTest(testInformation, filter, context, context.CancellationToken);
        }
    }
    
    private async Task ProcessKeyedNotInParallelTests(IList<NotInParallelTestCase> testsToProcess,
        ITestExecutionFilter? filter, ExecuteRequestContext context)
    {
        await testsToProcess
            .GroupBy(x => x.ConstraintKeys)
            .ForEachAsync(async group => await Task.Run(() => ProcessGroup(filter, context, group)))
            .ProcessInParallel(_maximumParallelTests);
    }

    private async Task ProcessParallelGroups(IDictionary<string, List<DiscoveredTest>> groups,
        ITestExecutionFilter? filter, ExecuteRequestContext context)
    {
        foreach (var (_, value) in groups)
        {
            await ProcessParallelTests(value, filter, context);
        }
    }

    private async Task ProcessGroup(ITestExecutionFilter? filter, ExecuteRequestContext context,
        IEnumerable<NotInParallelTestCase> tests)
    {
        foreach (var test in tests)
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

    private async Task ProcessParallelTests(IEnumerable<DiscoveredTest> queue, ITestExecutionFilter? filter,
        ExecuteRequestContext context)
    {
        await ProcessCollection(queue, filter, context);
    }

    private async Task ProcessCollection(IEnumerable<DiscoveredTest> queue,
        ITestExecutionFilter? filter,
        ExecuteRequestContext context)
    {
        await Parallel.ForEachAsync(queue, new ParallelOptions
        {
            MaxDegreeOfParallelism = _maximumParallelTests,
            CancellationToken = context.CancellationToken
        }, (test, token) => ProcessTest(test, filter, context, token));
    }

    private async ValueTask ProcessTest(DiscoveredTest test,
        ITestExecutionFilter? filter, ExecuteRequestContext context, CancellationToken cancellationToken)
    {
        NotifyTestStart();

        try
        {
            await Task.Run(() => _singleTestExecutor.ExecuteTestAsync(test, filter, context, false), cancellationToken);
        }
        catch (Exception exception)
        {
            await _logger.LogErrorAsync(exception);
            
            if (_commandLineOptions.IsOptionSet(FailFastCommandProvider.FailFast))
            {
                await _engineCancellationToken.CancellationTokenSource.CancelAsync();
            }
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

    private int GetParallelTestsLimit()
    {
        if (_commandLineOptions.TryGetOptionArgumentList(MaximumParallelTestsCommandProvider.MaximumParallelTests,
                out var values))
        {
            return int.Parse(values[0]);
        }

        return int.MaxValue;
    }
}
