using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using EnumerableAsyncProcessor.Extensions;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using Polyfills;
using TUnit.Core;
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
        using var executionContext = ExecutionContext.Capture();

        await Task.Run(async delegate
        {
            while (tests.TryDequeue(out var test, out _))
            {
                if (test.TestContext.SkipReason != null)
                {
                    await OnBeforeStart(test.TestContext, executionContext!);
                }

                await ProcessTest(test, filter, context, context.CancellationToken, executionContext!);
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
        using var executionContext = ExecutionContext.Capture();

        await Parallel.ForEachAsync(queue, new ParallelOptions
        {
            MaxDegreeOfParallelism = _maximumParallelTests,
            CancellationToken = context.CancellationToken
        }, async (test, token) =>
        {
            if (test.TestContext.SkipReason != null)
            {
                await OnBeforeStart(test.TestContext, executionContext!);
            }

            await ProcessTest(test, filter, context, token, executionContext!);
        });
#else
        using var executionContext = ExecutionContext.Capture();

        await queue
            .ForEachAsync(async test =>
            {
                if (test.TestContext.SkipReason != null)
                {
                    await OnBeforeStart(test.TestContext, executionContext!);
                }

                await ProcessTest(test, filter, context, context.CancellationToken, executionContext!);
            })
            .ProcessInParallel(_maximumParallelTests);
#endif
    }

#if NET
    private async ValueTask ProcessTest(DiscoveredTest test,
        ITestExecutionFilter? filter, ExecuteRequestContext context, CancellationToken cancellationToken,
        ExecutionContext executionContext)
    {
        try
        {
            ExecutionContext.Restore(executionContext);
            
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
        ITestExecutionFilter? filter, ExecuteRequestContext context, CancellationToken cancellationToken,
        ExecutionContext executionContext)
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

    private async Task OnBeforeStart(TestContext testContext, ExecutionContext executionContext)
    {
#if NET
        ExecutionContext.Restore(executionContext);
 #endif
 
        // Ideally all these 'Set up' hooks would be refactored into inner/classes and/or methods,
        // But users may want to set AsyncLocal values, and so the method must be a parent/ancestor of the method that starts the test!
        // So actually refactoring these into other methods would mean they wouldn't be a parent/ancestor and would break async local!
        var assemblyHooksTaskCompletionSource = _assemblyHookOrchestrator.PreviouslyRunBeforeHooks.GetOrAdd(
            testContext.TestDetails.TestClass.Type.Assembly, _ => new TaskCompletionSource<bool>(),
            out var assemblyHooksTaskPreviouslyExisted);

        if (assemblyHooksTaskPreviouslyExisted)
        {
            await assemblyHooksTaskCompletionSource.Task;
        }
        else
        {
            try
            {
                var beforeAssemblyHooks =
                    _assemblyHookOrchestrator.CollectBeforeHooks(testContext.TestDetails.TestClass.Type.Assembly);
                var assemblyHookContext =
                    _assemblyHookOrchestrator.GetContext(testContext.TestDetails.TestClass.Type.Assembly);

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
        }

        var classHooksTaskCompletionSource = _classHookOrchestrator.PreviouslyRunBeforeHooks.GetOrAdd(
            testContext.TestDetails.TestClass.Type, _ => new TaskCompletionSource<bool>(),
            out var classHooksTaskPreviouslyExisted);

        if (classHooksTaskPreviouslyExisted)
        {
            await classHooksTaskCompletionSource.Task;
        }
        else
        {
            try
            {
                var beforeClassHooks =
                    _classHookOrchestrator.CollectBeforeHooks(testContext.TestDetails.TestClass.Type);
                var classHookContext = _classHookOrchestrator.GetContext(testContext.TestDetails.TestClass.Type);

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
        }
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
