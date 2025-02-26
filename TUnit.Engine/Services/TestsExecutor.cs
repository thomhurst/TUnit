using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using EnumerableAsyncProcessor.Extensions;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using Polyfills;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Core.Logging;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Hooks;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

internal class TestsExecutor
{
    private readonly SingleTestExecutor _singleTestExecutor;
    private readonly TUnitFrameworkLogger _logger;
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
        _logger = logger;
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
                    RestoreContexts(await ExecuteBeforeAssemblyHooks(test.TestContext));

                    RestoreContexts(await ExecuteBeforeClassHooks(test.TestContext));
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
                RestoreContexts(await ExecuteBeforeAssemblyHooks(test.TestContext));

                RestoreContexts(await ExecuteBeforeClassHooks(test.TestContext));
            }

            await ProcessTest(test, filter, context, token);
        });
#else
        await queue
            .ForEachAsync(async test =>
            {
                if (test.TestContext.SkipReason != null)
                {
                    RestoreContexts(await ExecuteBeforeAssemblyHooks(test.TestContext));

                    RestoreContexts(await ExecuteBeforeClassHooks(test.TestContext));
                }

                await ProcessTest(test, filter, context, context.CancellationToken);
            })
            .ProcessInParallel(_maximumParallelTests);
#endif
    }

    private void RestoreContexts(List<ExecutionContext> executionContexts)
    {
#if NET
        foreach (var executionContext in executionContexts)
        {
            ExecutionContext.Restore(executionContext);
        }
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

    private async Task<List<ExecutionContext>> ExecuteBeforeClassHooks(TestContext testContext)
    {
        var classHookContext = _classHookOrchestrator.GetContext(testContext.TestDetails.TestClass.Type);

        var classHooksTaskCompletionSource = _classHookOrchestrator.PreviouslyRunBeforeHooks.GetOrAdd(
            testContext.TestDetails.TestClass.Type, _ => new TaskCompletionSource<bool>(),
            out var classHooksTaskPreviouslyExisted);

        if (classHooksTaskPreviouslyExisted)
        {
            await classHooksTaskCompletionSource.Task;
            return classHookContext.ExecutionContexts;
        }

        try
        {
            var beforeClassHooks =
                _classHookOrchestrator.CollectBeforeHooks(testContext.TestDetails.TestClass.Type);

            ClassHookContext.Current = classHookContext;

            foreach (var beforeHook in beforeClassHooks)
            {
                if (beforeHook.IsSynchronous)
                {
                    await _logger.LogDebugAsync("Executing synchronous [Before(Class)] hook");

                    beforeHook.Execute(classHookContext, CancellationToken.None);
                }
                else
                {
                    await _logger.LogDebugAsync("Executing asynchronous [Before(Class)] hook");

                    await beforeHook.ExecuteAsync(classHookContext, CancellationToken.None);
                }
            }

            ClassHookContext.Current = null;
            classHooksTaskCompletionSource.SetResult(false);
        }
        catch (Exception e)
        {
            classHooksTaskCompletionSource.SetException(e);
            throw;
        }

        return classHookContext.ExecutionContexts;
    }

    private async Task<List<ExecutionContext>> ExecuteBeforeAssemblyHooks(TestContext testContext)
    {
        var assemblyHookContext =_assemblyHookOrchestrator.GetContext(testContext.TestDetails.TestClass.Type.Assembly);

        var assemblyHooksTaskCompletionSource = _assemblyHookOrchestrator.PreviouslyRunBeforeHooks.GetOrAdd(
            testContext.TestDetails.TestClass.Type.Assembly, _ => new TaskCompletionSource<bool>(),
            out var assemblyHooksTaskPreviouslyExisted);

        if (assemblyHooksTaskPreviouslyExisted)
        {
            await assemblyHooksTaskCompletionSource.Task;
            return assemblyHookContext.ExecutionContexts;
        }

        try
        {
            var beforeAssemblyHooks = _assemblyHookOrchestrator.CollectBeforeHooks(testContext.TestDetails.TestClass.Type.Assembly);

            AssemblyHookContext.Current = assemblyHookContext;

            foreach (var beforeHook in beforeAssemblyHooks)
            {
                if (beforeHook.IsSynchronous)
                {
                    await _logger.LogDebugAsync("Executing synchronous [Before(Assembly)] hook");

                    beforeHook.Execute(assemblyHookContext, CancellationToken.None);
                }
                else
                {
                    await _logger.LogDebugAsync("Executing asynchronous [Before(Assembly)] hook");

                    await beforeHook.ExecuteAsync(assemblyHookContext, CancellationToken.None);
                }
            }

            AssemblyHookContext.Current = null;
            assemblyHooksTaskCompletionSource.SetResult(false);
        }
        catch (Exception e)
        {
            assemblyHooksTaskCompletionSource.SetException(e);
            throw;
        }

        return assemblyHookContext.ExecutionContexts;
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
