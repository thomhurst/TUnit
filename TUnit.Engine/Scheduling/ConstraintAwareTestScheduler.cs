using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;
using TUnit.Engine.Services;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Constraint-aware test scheduler that respects parallel execution constraints
/// while maximizing parallelism where possible
/// </summary>
internal sealed class ConstraintAwareTestScheduler : ITestScheduler
{
    private readonly ITestGroupingService _groupingService;
    private readonly IParallelismStrategy _parallelismStrategy;
    private readonly TUnitFrameworkLogger _logger;
    private readonly TimeSpan _testTimeout;

    public ConstraintAwareTestScheduler(
        ITestGroupingService groupingService,
        IParallelismStrategy parallelismStrategy,
        TUnitFrameworkLogger logger,
        TimeSpan? testTimeout = null)
    {
        _groupingService = groupingService;
        _parallelismStrategy = parallelismStrategy;
        _logger = logger;
        _testTimeout = testTimeout ?? TimeSpan.FromMinutes(5);
    }

    public async Task ScheduleAndExecuteAsync(
        IEnumerable<ExecutableTest> tests,
        ITestExecutor executor,
        CancellationToken cancellationToken)
    {
        var testList = tests.ToList();
        if (!testList.Any())
        {
            return;
        }

        var groupedTests = await _groupingService.GroupTestsByConstraintsAsync(testList);
        
        var executionGraph = BuildExecutionGraph(testList);
        
        ValidateDependencies(executionGraph);
        
        await ExecuteTestsAsync(executionGraph, groupedTests, executor, cancellationToken);
    }

    private Dictionary<string, TestExecutionState> BuildExecutionGraph(List<ExecutableTest> tests)
    {
        var graph = tests.ToDictionary(t => t.TestId, t => new TestExecutionState(t));

        foreach (var test in tests)
        {
            foreach (var dependency in test.Dependencies)
            {
                if (graph.TryGetValue(dependency.TestId, out var depState))
                {
                    depState.Dependents.Add(test.TestId);
                }
            }
        }

        return graph;
    }

    private void ValidateDependencies(Dictionary<string, TestExecutionState> graph)
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var testId in graph.Keys)
        {
            if (!visited.Contains(testId))
            {
                if (HasCycle(testId, graph, visited, recursionStack))
                {
                    throw new InvalidOperationException(
                        $"Circular dependency detected involving test: {testId}");
                }
            }
        }
    }

    private bool HasCycle(
        string testId,
        Dictionary<string, TestExecutionState> graph,
        HashSet<string> visited,
        HashSet<string> recursionStack)
    {
        visited.Add(testId);
        recursionStack.Add(testId);

        var state = graph[testId];
        foreach (var dependencyId in state.Test.Dependencies.Select(d => d.TestId))
        {
            if (!graph.ContainsKey(dependencyId))
            {
                continue;
            }

            if (!visited.Contains(dependencyId))
            {
                if (HasCycle(dependencyId, graph, visited, recursionStack))
                {
                    return true;
                }
            }
            else if (recursionStack.Contains(dependencyId))
            {
                return true;
            }
        }

        recursionStack.Remove(testId);
        return false;
    }

    private async Task ExecuteTestsAsync(
        Dictionary<string, TestExecutionState> graph,
        GroupedTests groupedTests,
        ITestExecutor executor,
        CancellationToken cancellationToken)
    {
        var readyQueue = new ConcurrentQueue<TestExecutionState>();
        var completionTracker = new TestCompletionTracker(graph, readyQueue);
        var runningConstraintKeys = new ConcurrentDictionary<string, int>();

        foreach (var state in graph.Values.Where(s => s.Test.State == TestState.Failed))
        {
            state.State = TestState.Failed;
            completionTracker.OnTestCompleted(state);
        }

        foreach (var state in graph.Values.Where(s => s.RemainingDependencies == 0 && s.Test.State != TestState.Failed))
        {
            readyQueue.Enqueue(state);
        }

        var workers = CreateWorkers(
            readyQueue, 
            graph, 
            executor, 
            completionTracker, 
            runningConstraintKeys,
            groupedTests,
            cancellationToken);

        try
        {
            await Task.WhenAll(workers);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await _logger.LogInformationAsync("Test execution cancelled");
        }
    }

    private Task[] CreateWorkers(
        ConcurrentQueue<TestExecutionState> readyQueue,
        Dictionary<string, TestExecutionState> graph,
        ITestExecutor executor,
        TestCompletionTracker completionTracker,
        ConcurrentDictionary<string, int> runningConstraintKeys,
        GroupedTests groupedTests,
        CancellationToken cancellationToken)
    {
        var workerCount = _parallelismStrategy.CurrentParallelism;
        var workers = new Task[workerCount];

#if NET
        var capturedContext = ExecutionContext.Capture();
#endif

        for (var i = 0; i < workerCount; i++)
        {
            var workerId = i;
            workers[i] = Task.Run(async () =>
            {
#if NET
                if (capturedContext != null)
                {
                    // Restore the execution context at the beginning of the worker
                    ExecutionContext.Restore(capturedContext);
                }
#endif
                await WorkerLoopAsync(
                    workerId,
                    readyQueue,
                    graph,
                    executor,
                    completionTracker,
                    runningConstraintKeys,
                    groupedTests,
                    cancellationToken);
            }, cancellationToken);
        }

        return workers;
    }

    private async Task WorkerLoopAsync(
        int workerId,
        ConcurrentQueue<TestExecutionState> readyQueue,
        Dictionary<string, TestExecutionState> graph,
        ITestExecutor executor,
        TestCompletionTracker completionTracker,
        ConcurrentDictionary<string, int> runningConstraintKeys,
        GroupedTests groupedTests,
        CancellationToken cancellationToken)
    {
        var deferredTests = new Queue<TestExecutionState>();

        while (!cancellationToken.IsCancellationRequested)
        {
            TestExecutionState? state = null;

            if (readyQueue.TryDequeue(out state) || 
                TryGetDeferredTest(deferredTests, runningConstraintKeys, out state))
            {
                if (state != null)
                {
                    if (CanRunTest(state, runningConstraintKeys))
                    {
                        await ExecuteTestWithConstraintAsync(
                            state, 
                            executor, 
                            completionTracker, 
                            runningConstraintKeys,
                            cancellationToken);
                    }
                    else
                    {
                        deferredTests.Enqueue(state);
                    }
                }
            }
            else if (completionTracker.AllTestsCompleted)
            {
                break;
            }
            else
            {
                if (deferredTests.Count > 0)
                {
                    foreach (var deferredTest in deferredTests)
                    {
                        readyQueue.Enqueue(deferredTest);
                    }
                    deferredTests.Clear();
                }

                try
                {
                    await Task.Delay(10, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private bool TryGetDeferredTest(
        Queue<TestExecutionState> deferredTests,
        ConcurrentDictionary<string, int> runningConstraintKeys,
        out TestExecutionState? state)
    {
        state = null;
        
        var count = deferredTests.Count;
        for (var i = 0; i < count; i++)
        {
            var test = deferredTests.Dequeue();
            if (CanRunTest(test, runningConstraintKeys))
            {
                state = test;
                return true;
            }
            deferredTests.Enqueue(test);
        }
        
        return false;
    }

    private bool CanRunTest(TestExecutionState state, ConcurrentDictionary<string, int> runningConstraintKeys)
    {
        if (state.Constraint is NotInParallelConstraint notInParallel)
        {
            if (notInParallel.NotInParallelConstraintKeys.Count == 0)
            {
                return runningConstraintKeys.IsEmpty || !runningConstraintKeys.ContainsKey("__global_not_in_parallel__");
            }
            
            return notInParallel.NotInParallelConstraintKeys.All(key => !runningConstraintKeys.ContainsKey(key));
        }

        return true;
    }

    private async Task ExecuteTestWithConstraintAsync(
        TestExecutionState state,
        ITestExecutor executor,
        TestCompletionTracker completionTracker,
        ConcurrentDictionary<string, int> runningConstraintKeys,
        CancellationToken cancellationToken)
    {
        var constraintKeys = GetConstraintKeys(state);
        
        foreach (var key in constraintKeys)
        {
            runningConstraintKeys.AddOrUpdate(key, 1, (k, v) => v + 1);
        }

        try
        {
            await ExecuteTestWithTimeoutAsync(state, executor, completionTracker, cancellationToken);
        }
        finally
        {
            foreach (var key in constraintKeys)
            {
                runningConstraintKeys.AddOrUpdate(key, 0, (k, v) => Math.Max(0, v - 1));
                if (runningConstraintKeys.TryGetValue(key, out var count) && count == 0)
                {
                    runningConstraintKeys.TryRemove(key, out _);
                }
            }
        }
    }

    private List<string> GetConstraintKeys(TestExecutionState state)
    {
        var keys = new List<string>();
        
        if (state.Constraint is NotInParallelConstraint notInParallel)
        {
            if (notInParallel.NotInParallelConstraintKeys.Count == 0)
            {
                keys.Add("__global_not_in_parallel__");
            }
            else
            {
                keys.AddRange(notInParallel.NotInParallelConstraintKeys);
            }
        }
        
        return keys;
    }

    private async Task ExecuteTestWithTimeoutAsync(
        TestExecutionState state,
        ITestExecutor executor,
        TestCompletionTracker completionTracker,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_testTimeout);
        state.TimeoutCts = timeoutCts;

        try
        {
            state.State = TestState.Running;
            await executor.ExecuteTestAsync(state.Test, timeoutCts.Token);
            state.State = TestState.Passed;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            state.State = TestState.Failed;
            await _logger.LogErrorAsync($"Test {state.Test.TestId} timed out after {_testTimeout}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            state.State = TestState.Skipped;
            await _logger.LogInformationAsync($"Test {state.Test.TestId} was cancelled");
        }
        catch (Exception ex)
        {
            state.State = TestState.Failed;
            await _logger.LogErrorAsync($"Test {state.Test.TestId} failed: {ex.Message}");
        }
        finally
        {
            completionTracker.OnTestCompleted(state);
        }
    }
}