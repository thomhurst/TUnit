using System.Collections.Concurrent;
using TUnit.Core.Logging;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// DAG-based test scheduler that maximizes parallelism while respecting dependencies
/// </summary>
public sealed class DagTestScheduler : ITestScheduler
{
    private readonly IParallelismStrategy _parallelismStrategy;
    private readonly IProgressMonitor _progressMonitor;
    private readonly TUnitFrameworkLogger _logger;
    private readonly TimeSpan _testTimeout;

    public DagTestScheduler(
        IParallelismStrategy parallelismStrategy,
        IProgressMonitor progressMonitor,
        TUnitFrameworkLogger logger,
        TimeSpan? testTimeout = null)
    {
        _parallelismStrategy = parallelismStrategy;
        _progressMonitor = progressMonitor;
        _logger = logger;
        _testTimeout = testTimeout ?? TimeSpan.FromMinutes(5);
    }

    public async Task ScheduleAndExecuteAsync(
        IEnumerable<ExecutableTest> tests,
        ITestExecutor executor,
        CancellationToken cancellationToken)
    {
        var testList = tests.ToList();
        if (!testList.Any()) return;

        // Build execution state
        var executionGraph = BuildExecutionGraph(testList);

        // Validate for circular dependencies
        ValidateDependencies(executionGraph);

        // Execute tests
        await ExecuteTestsAsync(executionGraph, executor, cancellationToken);
    }

    private Dictionary<string, TestExecutionState> BuildExecutionGraph(List<ExecutableTest> tests)
    {
        var graph = tests.ToDictionary(t => t.TestId, t => new TestExecutionState(t));

        // Build dependency relationships
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
        // Topological sort to detect cycles
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
                continue;

            if (!visited.Contains(dependencyId))
            {
                if (HasCycle(dependencyId, graph, visited, recursionStack))
                    return true;
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
        ITestExecutor executor,
        CancellationToken cancellationToken)
    {
        var readyQueue = new ConcurrentQueue<TestExecutionState>();
        var completionTracker = new TestCompletionTracker(graph, readyQueue);

        // Enqueue tests with no dependencies
        foreach (var state in graph.Values.Where(s => s.RemainingDependencies == 0))
        {
            readyQueue.Enqueue(state);
        }

        // Start progress monitoring
        using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var progressTask = _progressMonitor.MonitorProgressAsync(completionTracker, progressCts.Token);

        // Start worker tasks
        var workers = CreateWorkers(readyQueue, graph, executor, completionTracker, progressCts.Token);

        try
        {
            await Task.WhenAll(workers);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // This is expected when cancellation is requested (either user or fail-fast)
            await _logger.LogInformationAsync("Test execution cancelled");
        }
        finally
        {
            // Always cancel progress monitoring and wait for it to complete
            progressCts.Cancel();
            try
            {
                await progressTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when progress monitoring is cancelled
            }
        }
    }

    private Task[] CreateWorkers(
        ConcurrentQueue<TestExecutionState> readyQueue,
        Dictionary<string, TestExecutionState> graph,
        ITestExecutor executor,
        TestCompletionTracker completionTracker,
        CancellationToken cancellationToken)
    {
        var workerCount = _parallelismStrategy.CurrentParallelism;
        var workers = new Task[workerCount];
        var workStealingQueues = new WorkStealingQueue<TestExecutionState>[workerCount];

        for (int i = 0; i < workerCount; i++)
        {
            workStealingQueues[i] = new WorkStealingQueue<TestExecutionState>();
        }

        for (int i = 0; i < workerCount; i++)
        {
            var workerId = i;
            workers[i] = Task.Run(async () =>
            {
                await WorkerLoopAsync(
                    workerId,
                    readyQueue,
                    workStealingQueues,
                    graph,
                    executor,
                    completionTracker,
                    cancellationToken);
            }, cancellationToken);
        }

        return workers;
    }

    private async Task WorkerLoopAsync(
        int workerId,
        ConcurrentQueue<TestExecutionState> globalQueue,
        WorkStealingQueue<TestExecutionState>[] workStealingQueues,
        Dictionary<string, TestExecutionState> graph,
        ITestExecutor executor,
        TestCompletionTracker completionTracker,
        CancellationToken cancellationToken)
    {
        var localQueue = workStealingQueues[workerId];

        while (!cancellationToken.IsCancellationRequested)
        {
            TestExecutionState? state = null;

            // Try local queue first
            if (localQueue.TryDequeue(out state) ||
                // Then global queue
                globalQueue.TryDequeue(out state) ||
                // Finally steal from others
                TryStealWork(workerId, workStealingQueues, out state))
            {
                if (state != null)
                {
                    await ExecuteTestWithTimeoutAsync(state, executor, completionTracker, cancellationToken);
                }
            }
            else if (completionTracker.AllTestsCompleted)
            {
                // No more work
                break;
            }
            else
            {
                // Wait for new work
                try
                {
                    await Task.Delay(10, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
            }
        }
    }

    private bool TryStealWork(
        int workerId,
        WorkStealingQueue<TestExecutionState>[] queues,
        out TestExecutionState? state)
    {
        state = null;

        for (int i = 1; i < queues.Length; i++)
        {
            var targetId = (workerId + i) % queues.Length;
            if (queues[targetId].TrySteal(out state))
            {
                return true;
            }
        }

        return false;
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
            // Test was cancelled (either by user or fail-fast)
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
