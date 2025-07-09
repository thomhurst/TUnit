using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;
using TUnit.Engine.Services;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Test scheduler that respects both parallel constraints and execution order
/// </summary>
internal sealed class OrderedConstraintTestScheduler : ITestScheduler
{
    private readonly ITestGroupingService _groupingService;
    private readonly IParallelismStrategy _parallelismStrategy;
    private readonly TUnitFrameworkLogger _logger;
    private readonly TimeSpan _testTimeout;

    public OrderedConstraintTestScheduler(
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
        var completionTask = new TaskCompletionSource<bool>();
        var runningConstraintKeys = new ConcurrentDictionary<string, int>();
        var executedTests = new ConcurrentDictionary<string, bool>();
        
        var constraintExecutor = new ConstraintExecutor(
            graph,
            executor,
            runningConstraintKeys,
            executedTests,
            _logger,
            _testTimeout,
            cancellationToken);

        var tasks = new List<Task>();

        // Execute parallel tests (no constraints)
        if (groupedTests.Parallel.Any())
        {
            tasks.Add(ExecuteParallelTestsAsync(
                groupedTests.Parallel,
                graph,
                constraintExecutor,
                cancellationToken));
        }

        // Execute global NotInParallel tests (ordered)
        if (groupedTests.NotInParallel.Count > 0)
        {
            tasks.Add(ExecuteOrderedNotInParallelTestsAsync(
                groupedTests.NotInParallel,
                graph,
                constraintExecutor,
                "__global_not_in_parallel__",
                cancellationToken));
        }

        // Execute keyed NotInParallel tests (ordered within each key)
        foreach (var kvp in groupedTests.KeyedNotInParallel)
        {
            var key = kvp.Key;
            var queue = kvp.Value;
            
            tasks.Add(ExecuteOrderedNotInParallelTestsAsync(
                queue,
                graph,
                constraintExecutor,
                key,
                cancellationToken));
        }

        // Execute ParallelGroups (ordered between groups, parallel within groups)
        foreach (var groupKvp in groupedTests.ParallelGroups)
        {
            var groupName = groupKvp.Key;
            var orderGroups = groupKvp.Value;
            
            tasks.Add(ExecuteParallelGroupAsync(
                groupName,
                orderGroups,
                graph,
                constraintExecutor,
                cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task ExecuteParallelTestsAsync(
        IList<ExecutableTest> tests,
        Dictionary<string, TestExecutionState> graph,
        ConstraintExecutor constraintExecutor,
        CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        
        foreach (var test in tests)
        {
            if (graph.TryGetValue(test.TestId, out var state))
            {
                tasks.Add(constraintExecutor.ExecuteWhenDependenciesReadyAsync(state));
            }
        }

        await Task.WhenAll(tasks);
    }

    private async Task ExecuteOrderedNotInParallelTestsAsync(
        PriorityQueue<ExecutableTest, int> queue,
        Dictionary<string, TestExecutionState> graph,
        ConstraintExecutor constraintExecutor,
        string constraintKey,
        CancellationToken cancellationToken)
    {
        while (queue.TryDequeue(out var test, out var priority))
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (graph.TryGetValue(test.TestId, out var state))
            {
                await constraintExecutor.ExecuteWithConstraintKeyAsync(state, constraintKey);
            }
        }
    }

    private async Task ExecuteParallelGroupAsync(
        string groupName,
        SortedDictionary<int, List<ExecutableTest>> orderGroups,
        Dictionary<string, TestExecutionState> graph,
        ConstraintExecutor constraintExecutor,
        CancellationToken cancellationToken)
    {
        foreach (var orderKvp in orderGroups)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var tests = orderKvp.Value;
            var tasks = new List<Task>();
            
            foreach (var test in tests)
            {
                if (graph.TryGetValue(test.TestId, out var state))
                {
                    tasks.Add(constraintExecutor.ExecuteWithConstraintKeyAsync(state, groupName));
                }
            }

            await Task.WhenAll(tasks);
        }
    }

    private class ConstraintExecutor
    {
        private readonly Dictionary<string, TestExecutionState> _graph;
        private readonly ITestExecutor _executor;
        private readonly ConcurrentDictionary<string, int> _runningConstraintKeys;
        private readonly ConcurrentDictionary<string, bool> _executedTests;
        private readonly TUnitFrameworkLogger _logger;
        private readonly TimeSpan _testTimeout;
        private readonly CancellationToken _cancellationToken;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _constraintSemaphores = new();

        public ConstraintExecutor(
            Dictionary<string, TestExecutionState> graph,
            ITestExecutor executor,
            ConcurrentDictionary<string, int> runningConstraintKeys,
            ConcurrentDictionary<string, bool> executedTests,
            TUnitFrameworkLogger logger,
            TimeSpan testTimeout,
            CancellationToken cancellationToken)
        {
            _graph = graph;
            _executor = executor;
            _runningConstraintKeys = runningConstraintKeys;
            _executedTests = executedTests;
            _logger = logger;
            _testTimeout = testTimeout;
            _cancellationToken = cancellationToken;
        }

        public async Task ExecuteWhenDependenciesReadyAsync(TestExecutionState state)
        {
            await WaitForDependenciesAsync(state);
            
            if (!_cancellationToken.IsCancellationRequested)
            {
                await ExecuteTestAsync(state);
            }
        }

        public async Task ExecuteWithConstraintKeyAsync(TestExecutionState state, string constraintKey)
        {
            var semaphore = _constraintSemaphores.GetOrAdd(constraintKey, _ => new SemaphoreSlim(1, 1));
            
            await semaphore.WaitAsync(_cancellationToken);
            try
            {
                await WaitForDependenciesAsync(state);
                
                if (!_cancellationToken.IsCancellationRequested)
                {
                    await ExecuteTestAsync(state);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task WaitForDependenciesAsync(TestExecutionState state)
        {
            while (state.RemainingDependencies > 0 && !_cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(10, _cancellationToken);
            }
        }

        private async Task ExecuteTestAsync(TestExecutionState state)
        {
            if (_executedTests.TryAdd(state.Test.TestId, true))
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
                timeoutCts.CancelAfter(_testTimeout);
                state.TimeoutCts = timeoutCts;

                try
                {
                    state.State = TestState.Running;
                    await _executor.ExecuteTestAsync(state.Test, timeoutCts.Token);
                    state.State = TestState.Passed;
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !_cancellationToken.IsCancellationRequested)
                {
                    state.State = TestState.Failed;
                    await _logger.LogErrorAsync($"Test {state.Test.TestId} timed out after {_testTimeout}");
                }
                catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
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
                    foreach (var dependentId in state.Dependents)
                    {
                        if (_graph.TryGetValue(dependentId, out var dependent))
                        {
                            dependent.DecrementRemainingDependencies();
                        }
                    }
                }
            }
        }
    }
}