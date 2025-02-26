using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using EnumerableAsyncProcessor.Extensions;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using Polyfills;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Helpers;
using TUnit.Engine.Hooks;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

internal class TestsExecutor
{
    private readonly SingleTestExecutor _singleTestExecutor;
    private readonly ICommandLineOptions _commandLineOptions;
    private readonly EngineCancellationToken _engineCancellationToken;

    private readonly AssemblyHookOrchestrator _assemblyHookOrchestrator;
    private readonly ClassHookOrchestrator _classHookOrchestrator;
    
    private readonly Counter _executionCounter = new();
    private readonly TaskCompletionSource<bool> _onFinished = new();

    private readonly int _maximumParallelTests;

    public TestsExecutor(SingleTestExecutor singleTestExecutor,
        TUnitFrameworkLogger logger,
        ICommandLineOptions commandLineOptions,
        EngineCancellationToken engineCancellationToken, AssemblyHookOrchestrator assemblyHookOrchestrator, ClassHookOrchestrator classHookOrchestrator)
    {
        _singleTestExecutor = singleTestExecutor;
        _commandLineOptions = commandLineOptions;
        _engineCancellationToken = engineCancellationToken;
        _assemblyHookOrchestrator = assemblyHookOrchestrator;
        _classHookOrchestrator = classHookOrchestrator;

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
            while (tests.TryDequeue(out var test, out _))
            {
                if (test.TestContext.SkipReason == null)
                {
                    ExecutionContextHelper.RestoreContext(await _assemblyHookOrchestrator.ExecuteBeforeAssemblyHooks(test.TestContext));

                    ExecutionContextHelper.RestoreContext(await _classHookOrchestrator.ExecuteBeforeClassHooks(test.TestContext));
                }

                await ProcessTest(test, filter, context, context.CancellationToken);
            }
        });
    }

    private async Task ProcessParallelTests(IEnumerable<DiscoveredTest> queue, ITestExecutionFilter? filter,
        ExecuteRequestContext context)
    {
        await ProcessCollection(queue, filter, context);
    }

    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    private async Task ProcessCollection(IEnumerable<DiscoveredTest> queue,
        ITestExecutionFilter? filter,
        ExecuteRequestContext context)
    {
#if NET
        await Parallel.ForEachAsync(queue, new ParallelOptions
        {
            MaxDegreeOfParallelism = _maximumParallelTests,
            CancellationToken = context.CancellationToken
        }, async (test, token) =>
        {
            if (test.TestContext.SkipReason == null)
            {
                ExecutionContextHelper.RestoreContext(await _assemblyHookOrchestrator.ExecuteBeforeAssemblyHooks(test.TestContext));

                ExecutionContextHelper.RestoreContext(await _classHookOrchestrator.ExecuteBeforeClassHooks(test.TestContext));
            }

            await ProcessTest(test, filter, context, token);
        });
#else
        await queue
            .ForEachAsync(async test =>
            {
                if (test.TestContext.SkipReason != null)
                {
                    ExecutionContextHelper.RestoreContext(await _assemblyHookOrchestrator.ExecuteBeforeAssemblyHooks(test.TestContext));

                    ExecutionContextHelper.RestoreContext(await _classHookOrchestrator.ExecuteBeforeClassHooks(test.TestContext));
                }

                await ProcessTest(test, filter, context, context.CancellationToken);
            })
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
