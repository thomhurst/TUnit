using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Logging;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;
using TUnit.Engine.Services;
using LoggingExtensions = TUnit.Core.Logging.LoggingExtensions;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// A clean, simplified test scheduler that uses an execution plan
/// </summary>
internal sealed class TestScheduler : ITestScheduler
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestGroupingService _groupingService;
    private readonly int _maxParallelism;

    public TestScheduler(
        TUnitFrameworkLogger logger,
        ITestGroupingService groupingService,
        int maxParallelism)
    {
        _logger = logger;
        _groupingService = groupingService;
        _maxParallelism = maxParallelism > 0 ? maxParallelism : ParallelismDetector.DetectOptimalParallelism();
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

        await _logger.LogInformationAsync($"Execution plan created: {plan.ExecutableTests.Count} executable tests out of {plan.AllTests.Count} total (max parallelism: {_maxParallelism})");

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
        var semaphore = new SemaphoreSlim(_maxParallelism, _maxParallelism);

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
                kvp.Key,
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
            var groupTask = ExecuteParallelGroupAsync(
                plan,
                group.Key,
                group.Value,
                executor,
                runningTasks,
                completedTests,
                semaphore,
                cancellationToken);
            allTestTasks.Add(groupTask);
        }

        // 4. Fully parallel tests
        var parallelTask = ExecuteParallelTestsAsync(
            plan,
            groupedTests.Parallel,
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
            await ExecuteTestWhenReadyAsync(plan, test, executor, runningTasks, completedTests, cancellationToken);
        }
    }

    private async Task ExecuteKeyedNotInParallelTestsAsync(
        ExecutionPlan plan,
        string key,
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
            await ExecuteTestWhenReadyAsync(plan, test, executor, runningTasks, completedTests, cancellationToken);
        }
    }

    private async Task ExecuteParallelGroupAsync(
        ExecutionPlan plan,
        string groupKey,
        SortedDictionary<int, List<AbstractExecutableTest>> orderGroups,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        // Execute order groups sequentially
        foreach (var orderGroup in orderGroups.OrderBy(og => og.Key))
        {
            var tasks = new List<Task>();

            foreach (var test in orderGroup.Value)
            {
                await semaphore.WaitAsync(cancellationToken);

                var task = ExecuteTestWhenReadyAsync(plan, test, executor, runningTasks, completedTests, cancellationToken)
                    .ContinueWith(_ => semaphore.Release(), cancellationToken);

                tasks.Add(task);
            }

            // Wait for all tests in this order group to complete
            await Task.WhenAll(tasks);
        }
    }

    private async Task ExecuteParallelTestsAsync(
        ExecutionPlan plan,
        IEnumerable<AbstractExecutableTest> tests,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        foreach (var test in tests)
        {
            await semaphore.WaitAsync(cancellationToken);

            var task = ExecuteTestWhenReadyAsync(plan, test, executor, runningTasks, completedTests, cancellationToken)
                .ContinueWith(_ => semaphore.Release(), cancellationToken);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    private async Task ExecuteTestWhenReadyAsync(
        ExecutionPlan plan,
        AbstractExecutableTest test,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        CancellationToken cancellationToken)
    {
        await Task.WhenAll(test.Dependencies.Select(x => x.Test.CompletionTask));

        // Execute the test
        var executionTask = Task.Run(async () =>
        {
            try
            {
                await executor.ExecuteTestAsync(test, cancellationToken);
            }
            finally
            {
                completedTests[test] = true;
            }
        }, cancellationToken);

        runningTasks[test] = executionTask;
        await executionTask;
        runningTasks.TryRemove(test, out _);
    }
}
