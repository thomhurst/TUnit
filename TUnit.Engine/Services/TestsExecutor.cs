using System.Collections.Concurrent;
using EnumerableAsyncProcessor.Extensions;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using Polyfills;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

internal class TestsExecutor
{
    private readonly SingleTestExecutor _singleTestExecutor;
    private readonly TUnitFrameworkLogger _logger;
    private readonly ICommandLineOptions _commandLineOptions;
    private readonly EngineCancellationToken _engineCancellationToken;

    private readonly Counter _executionCounter = new();
    private readonly TaskCompletionSource<bool> _onFinished = new();

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
                _onFinished.TrySetResult(false);
            }
        };
    }

    public async Task ExecuteAsync(GroupedTests tests, ITestExecutionFilter? filter, ExecuteRequestContext context)
    {
        using var _ = _engineCancellationToken.Token.Register(() =>
        {
            _onFinished.TrySetCanceled();
        });

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
        await ProcessQueue(filter, context, testsNotInParallel);
    }

    private async Task ProcessKeyedNotInParallelTests(IDictionary<ConstraintKeysCollection, PriorityQueue<DiscoveredTest, int>> testsToProcess,
        ITestExecutionFilter? filter, ExecuteRequestContext context)
    {
        await testsToProcess.Values
            .ForEachAsync(async group => await Task.Run(() => ProcessQueue(filter, context, group)))
            .ProcessInParallel(_maximumParallelTests);
    }

    private async Task ProcessParallelGroups(ConcurrentDictionary<ParallelGroupConstraint, List<DiscoveredTest>> groups,
        ITestExecutionFilter? filter, ExecuteRequestContext context)
    {
        foreach (var (_, value) in groups.OrderBy(x => x.Key.Order))
        {
            await ProcessParallelTests(value, filter, context);
        }
    }

    private async Task ProcessQueue(ITestExecutionFilter? filter, ExecuteRequestContext context,
        PriorityQueue<DiscoveredTest, int> tests)
    {
        await Task.Run(async delegate
        {
            while (tests.TryDequeue(out var testInformation, out _))
            {
                await ProcessTest(testInformation, filter, context, context.CancellationToken);
            }
        });
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
#if NET
        await Parallel.ForEachAsync(queue, new ParallelOptions
        {
            MaxDegreeOfParallelism = _maximumParallelTests,
            CancellationToken = context.CancellationToken
        }, (test, token) => ProcessTest(test, filter, context, token));
#else
        await queue
            .ForEachAsync(test => ProcessTest(test, filter, context, context.CancellationToken))
            .ProcessInParallel(_maximumParallelTests);
#endif
    }

#if NET
    private async ValueTask ProcessTest(DiscoveredTest test,
        ITestExecutionFilter? filter, ExecuteRequestContext context, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(() => _singleTestExecutor.ExecuteTestAsync(test, filter, context, false), cancellationToken);
        }
        catch
        {
            if (_commandLineOptions.IsOptionSet(FailFastCommandProvider.FailFast))
            {
                await _engineCancellationToken.CancellationTokenSource.CancelAsync();
            }
        }
    }
#else
    private async Task ProcessTest(DiscoveredTest test,
        ITestExecutionFilter? filter, ExecuteRequestContext context, CancellationToken cancellationToken)
    {
        try
        {
            await _singleTestExecutor.ExecuteTestAsync(test, filter, context, false);
        }
        catch
        {
            if (_commandLineOptions.IsOptionSet(FailFastCommandProvider.FailFast))
            {
                _engineCancellationToken.CancellationTokenSource.Cancel();
            }
        }
    }
#endif

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
