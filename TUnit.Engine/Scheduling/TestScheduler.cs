using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;
using TUnit.Engine.Services;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// A clean, simplified test scheduler that uses an execution plan
/// </summary>
internal sealed class TestScheduler : ITestScheduler, IDisposable
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestGroupingService _groupingService;
    private readonly ITUnitMessageBus _messageBus;
    private readonly SchedulerConfiguration _configuration;
    private AdaptiveParallelismController? _adaptiveController;
    private IDisposable? _semaphore;

    public TestScheduler(
        TUnitFrameworkLogger logger,
        ITestGroupingService groupingService,
        ITUnitMessageBus messageBus,
        SchedulerConfiguration configuration)
    {
        _logger = logger;
        _groupingService = groupingService;
        _messageBus = messageBus;
        _configuration = configuration;
    }

    public async Task ScheduleAndExecuteAsync(
        IEnumerable<AbstractExecutableTest> tests,
        ITestExecutor executor,
        CancellationToken cancellationToken)
    {
        if (tests == null) throw new ArgumentNullException(nameof(tests));
        if (executor == null) throw new ArgumentNullException(nameof(executor));

        // Create execution plan upfront
        var plan = ExecutionPlan.Create(tests);


        if (plan.ExecutableTests.Count == 0)
        {
            await _logger.LogDebugAsync("No executable tests found");
            return;
        }

        // Group tests by constraints
        var groupedTests = await _groupingService.GroupTestsByConstraintsAsync(plan.ExecutableTests);

        // Execute tests
        await ExecuteGroupedTestsAsync(plan, groupedTests, executor, cancellationToken);
    }

    private async Task ExecuteGroupedTestsAsync(
        ExecutionPlan plan,
        GroupedTests groupedTests,
        ITestExecutor executor,
        CancellationToken cancellationToken)
    {
        var runningTasks = new ConcurrentDictionary<AbstractExecutableTest, Task>();
        var completedTests = new ConcurrentDictionary<AbstractExecutableTest, bool>();

        // Create appropriate semaphore based on strategy
        object semaphore;
        if (_configuration.Strategy == ParallelismStrategy.Adaptive)
        {
            var initialParallelism = Environment.ProcessorCount * 4;
            var adaptiveSemaphore = new AdaptiveSemaphore(initialParallelism, _configuration.AdaptiveMaxParallelism);
            _adaptiveController = new AdaptiveParallelismController(
                adaptiveSemaphore,
                _logger,
                _configuration.AdaptiveMinParallelism,
                _configuration.AdaptiveMaxParallelism,
                initialParallelism,
                _configuration.EnableAdaptiveMetrics);
            semaphore = adaptiveSemaphore;
            _semaphore = adaptiveSemaphore;
        }
        else
        {
            var maxParallelism = _configuration.MaxParallelism > 0 ? _configuration.MaxParallelism : Environment.ProcessorCount * 4;
            var fixedSemaphore = new SemaphoreSlim(maxParallelism, maxParallelism);
            semaphore = fixedSemaphore;
            _semaphore = fixedSemaphore;
        }

        // Process all test groups
        var allTestTasks = new List<Task>();

        // 1. NotInParallel tests (global) - must run one at a time
        if (groupedTests.NotInParallel.Count > 0)
        {
            var globalNotInParallelTask = ExecuteNotInParallelTestsAsync(
                plan,
                groupedTests.NotInParallel,
                executor,
                runningTasks,
                completedTests,
                cancellationToken);
            allTestTasks.Add(globalNotInParallelTask);
        }

        // 2. Keyed NotInParallel tests - can run in parallel with other keys
        foreach (var kvp in groupedTests.KeyedNotInParallel)
        {
            var keyedTask = ExecuteKeyedNotInParallelTestsAsync(
                plan,
                kvp.Value,
                executor,
                runningTasks,
                completedTests,
                cancellationToken);
            allTestTasks.Add(keyedTask);
        }

        // 3. Parallel groups - can run in parallel within constraints
        foreach (var group in groupedTests.ParallelGroups)
        {
            var groupTask = ExecuteParallelGroupAsync(group.Value,
                executor,
                runningTasks,
                completedTests,
                semaphore,
                cancellationToken);
            allTestTasks.Add(groupTask);
        }

        // 4. Fully parallel tests
        var parallelTask = ExecuteParallelTestsAsync(groupedTests.Parallel,
            executor,
            runningTasks,
            completedTests,
            semaphore,
            cancellationToken);
        allTestTasks.Add(parallelTask);

        // Wait for all tests to complete
        await Task.WhenAll(allTestTasks);
    }

    private async Task ExecuteNotInParallelTestsAsync(
        ExecutionPlan plan,
        PriorityQueue<AbstractExecutableTest, TestPriority> queue,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        CancellationToken cancellationToken)
    {
        var tests = new List<AbstractExecutableTest>();
        while (queue.TryDequeue(out var test, out _))
        {
            tests.Add(test);
        }

        // Sort by execution order from the plan
        tests.Sort((a, b) =>
        {
            var aOrder = plan.ExecutionOrder.TryGetValue(a, out var ao) ? ao : int.MaxValue;
            var bOrder = plan.ExecutionOrder.TryGetValue(b, out var bo) ? bo : int.MaxValue;
            return aOrder.CompareTo(bOrder);
        });

        // Execute sequentially
        foreach (var test in tests)
        {
            await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken);
        }
    }

    private async Task ExecuteKeyedNotInParallelTestsAsync(
        ExecutionPlan plan,
        PriorityQueue<AbstractExecutableTest, TestPriority> queue,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        CancellationToken cancellationToken)
    {
        var tests = new List<AbstractExecutableTest>();
        while (queue.TryDequeue(out var test, out _))
        {
            tests.Add(test);
        }

        // Sort by execution order
        tests.Sort((a, b) =>
        {
            var aOrder = plan.ExecutionOrder.TryGetValue(a, out var ao) ? ao : int.MaxValue;
            var bOrder = plan.ExecutionOrder.TryGetValue(b, out var bo) ? bo : int.MaxValue;
            return aOrder.CompareTo(bOrder);
        });

        // Execute sequentially within this key
        foreach (var test in tests)
        {
            await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken);
        }
    }

    private async Task ExecuteParallelGroupAsync(SortedDictionary<int, List<AbstractExecutableTest>> orderGroups,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        object semaphore,
        CancellationToken cancellationToken)
    {
        // Execute order groups sequentially
        foreach (var orderGroup in orderGroups.OrderBy(og => og.Key))
        {
            var tasks = new List<Task>();

            foreach (var test in orderGroup.Value)
            {
                if (semaphore is AdaptiveSemaphore adaptive)
                    await adaptive.WaitAsync(cancellationToken);
                else
                    await ((SemaphoreSlim)semaphore).WaitAsync(cancellationToken);

                // Create a task that releases semaphore on completion without Task.Run overhead
                async Task ExecuteWithSemaphoreRelease()
                {
                    try
                    {
                        await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken);
                    }
                    finally
                    {
                        if (semaphore is AdaptiveSemaphore adaptive)
                            adaptive.Release();
                        else
                            ((SemaphoreSlim)semaphore).Release();
                    }
                }

                tasks.Add(ExecuteWithSemaphoreRelease());
            }

            // Wait for all tests in this order group to complete
            await Task.WhenAll(tasks);
        }
    }

    private async Task ExecuteParallelTestsAsync(IEnumerable<AbstractExecutableTest> tests,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        object semaphore,
        CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        foreach (var test in tests)
        {
            if (semaphore is AdaptiveSemaphore adaptive)
                await adaptive.WaitAsync(cancellationToken);
            else
                await ((SemaphoreSlim)semaphore).WaitAsync(cancellationToken);

            // Create a task that releases semaphore on completion without Task.Run overhead
            async Task ExecuteWithSemaphoreRelease()
            {
                try
                {
                    await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken);
                }
                finally
                {
                    if (semaphore is AdaptiveSemaphore adaptive)
                        adaptive.Release();
                    else
                        ((SemaphoreSlim)semaphore).Release();
                }
            }

            tasks.Add(ExecuteWithSemaphoreRelease());
        }

        await Task.WhenAll(tasks);
    }

    private async Task ExecuteTestWhenReadyAsync(AbstractExecutableTest test,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        CancellationToken cancellationToken)
    {
        // If test is already failed (e.g., due to circular dependencies), report the pre-failure
        if (test.State == TestState.Failed)
        {
            await _messageBus.Failed(test.Context,
                    test.Result?.Exception ?? new InvalidOperationException("Test was marked as failed before execution"),
                    test.StartTime ?? DateTimeOffset.UtcNow);

            return;
        }

        if (test.State == TestState.Skipped)
        {
            await _messageBus.Skipped(test.Context, "Test was skipped");
            return;
        }

        await Task.WhenAll(test.Dependencies.Select(x => x.Test.CompletionTask));

        // Execute the test directly without Task.Run wrapper
        var executionTask = ExecuteTestDirectlyAsync(test, executor, completedTests, cancellationToken);
        runningTasks[test] = executionTask;
        await executionTask;
        runningTasks.TryRemove(test, out _);
    }

    private async Task ExecuteTestDirectlyAsync(AbstractExecutableTest test, ITestExecutor executor, ConcurrentDictionary<AbstractExecutableTest, bool> completedTests, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            await executor.ExecuteTestAsync(test, cancellationToken);
        }
        finally
        {
            completedTests[test] = true;

            // Record completion for adaptive metrics
            if (_adaptiveController != null)
            {
                var executionTime = DateTime.UtcNow - startTime;
                _adaptiveController.RecordTestCompletion(executionTime);
            }
        }
    }

    public void Dispose()
    {
        _adaptiveController?.Dispose();
        _semaphore?.Dispose();
    }
}
