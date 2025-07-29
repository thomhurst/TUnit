using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using EnumerableAsyncProcessor.Extensions;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Requests;
using Polyfills;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Engine.CommandLineProviders;
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

    public async Task ExecuteAsync(GroupedTests tests, ITestExecutionFilter? filter, CancellationToken cancellationToken)
    {
#if NET
        await
#endif
        using var _ = _engineCancellationToken.Token.Register(() =>
        {
            _onFinished.TrySetCanceled();
        });

        try
        {
            _executionCounter.Increment();

            await ProcessParallelTests(tests.Parallel, filter, cancellationToken);

            await ProcessParallelGroups(tests.ParallelGroups, filter, cancellationToken);

            await ProcessKeyedNotInParallelTests(tests.KeyedNotInParallel, filter, cancellationToken);

            await ProcessNotInParallelTests(tests.NotInParallel, filter, cancellationToken);
        }
        finally
        {
            _executionCounter.Decrement();
        }
    }

    public Task WaitForFinishAsync() => _onFinished.Task;

    private async Task ProcessNotInParallelTests(PriorityQueue<DiscoveredTest, int> testsNotInParallel, ITestExecutionFilter? filter, CancellationToken cancellationToken)
    {
        await ProcessQueue(filter, testsNotInParallel, cancellationToken);
    }

    private async Task ProcessKeyedNotInParallelTests(IDictionary<ConstraintKeysCollection, PriorityQueue<DiscoveredTest, int>> testsToProcess,
        ITestExecutionFilter? filter, CancellationToken cancellationToken)
    {
        await testsToProcess.Values
            .ForEachAsync(async group => await Task.Run(() => ProcessQueue(filter, group, cancellationToken), cancellationToken))
            .ProcessInParallel(_maximumParallelTests);
    }

    private async Task ProcessParallelGroups(ConcurrentDictionary<ParallelGroupConstraint, List<DiscoveredTest>> groups,
        ITestExecutionFilter? filter, CancellationToken cancellationToken)
    {
        foreach (var (_, value) in groups.OrderBy(x => x.Key.Order))
        {
            await ProcessParallelTests(value, filter, cancellationToken);
        }
    }

    private async Task ProcessQueue(ITestExecutionFilter? filter,
        PriorityQueue<DiscoveredTest, int> tests, CancellationToken cancellationToken)
    {
        await Task.Run(async delegate
        {
            while (tests.TryDequeue(out var test, out _))
            {
                if (test.TestContext.SkipReason == null)
                {
                    await _assemblyHookOrchestrator.ExecuteBeforeAssemblyHooks(test.TestContext);
                    
                    test.TestContext.AssemblyContext.RestoreExecutionContext();

                    await _classHookOrchestrator.ExecuteBeforeClassHooks(test.TestContext);
                    
                    test.TestContext.ClassContext.RestoreExecutionContext();
                }

                await ProcessTest(test, filter, cancellationToken);
            }
        }, cancellationToken);
    }

    private async Task ProcessParallelTests(IEnumerable<DiscoveredTest> queue, ITestExecutionFilter? filter,
        CancellationToken cancellationToken)
    {
        await ProcessCollection(queue, filter, cancellationToken);
    }

    [UnconditionalSuppressMessage("ReSharper", "AccessToDisposedClosure")]
    private async Task ProcessCollection(IEnumerable<DiscoveredTest> queue,
        ITestExecutionFilter? filter,
        CancellationToken cancellationToken)
    {
#if NET
        await Parallel.ForEachAsync(queue, new ParallelOptions
        {
            MaxDegreeOfParallelism = _maximumParallelTests,
            CancellationToken = cancellationToken
        }, async (test, token) =>
        {
            if (test.TestContext.SkipReason == null)
            {
                await _assemblyHookOrchestrator.ExecuteBeforeAssemblyHooks(test.TestContext);

                test.TestContext.AssemblyContext.RestoreExecutionContext();

                await _classHookOrchestrator.ExecuteBeforeClassHooks(test.TestContext);
                
                test.TestContext.ClassContext.RestoreExecutionContext();
            }

            await ProcessTest(test, filter, token);
        });
#else
        await queue
            .ForEachAsync(async test =>
            {
                if (test.TestContext.SkipReason == null)
                {
                    await _assemblyHookOrchestrator.ExecuteBeforeAssemblyHooks(test.TestContext);

                    test.TestContext.AssemblyContext.RestoreExecutionContext();
                    
                    await _classHookOrchestrator.ExecuteBeforeClassHooks(test.TestContext);
                    
                    test.TestContext.ClassContext.RestoreExecutionContext();
                }

                await ProcessTest(test, filter, cancellationToken);
            })
            .ProcessInParallel(_maximumParallelTests);
#endif
    }

#if NET
    private async ValueTask ProcessTest(DiscoveredTest test,
        ITestExecutionFilter? filter, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(() => _singleTestExecutor.ExecuteTestAsync(test, filter, false), cancellationToken);
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
        ITestExecutionFilter? filter, CancellationToken cancellationToken)
    {
        try
        {
            await _singleTestExecutor.ExecuteTestAsync(test, filter, false);
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
        if (Debugger.IsAttached)
        {
            return 1;
        }
        
        if (_commandLineOptions.TryGetOptionArgumentList(MaximumParallelTestsCommandProvider.MaximumParallelTests,
                out var values))
        {
            return int.Parse(values[0]);
        }

        return int.MaxValue;
    }
}
