using System.Collections.Concurrent;
using EnumerableAsyncProcessor.Extensions;
using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;
using TUnit.Engine.Services;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// A clean, simplified test scheduler that uses an execution plan
/// </summary>
internal sealed class TestScheduler : ITestScheduler
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestGroupingService _groupingService;
    private readonly ITUnitMessageBus _messageBus;
    private readonly SchedulerConfiguration _configuration;

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

        // Determine parallelism level
        int? maxParallelism = null;
        if (_configuration.Strategy != ParallelismStrategy.Adaptive)
        {
            maxParallelism = _configuration.MaxParallelism > 0 ? _configuration.MaxParallelism : Environment.ProcessorCount * 4;
        }
        // For adaptive, we pass null to let EnumerableAsyncProcessor manage concurrency

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
                maxParallelism,
                cancellationToken);
            allTestTasks.Add(groupTask);
        }

        // 4. Fully parallel tests
        var parallelTask = ExecuteParallelTestsAsync(groupedTests.Parallel,
            executor,
            runningTasks,
            completedTests,
            maxParallelism,
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
        var testsWithPriority = new List<(AbstractExecutableTest Test, TestPriority Priority)>();
        while (queue.TryDequeue(out var test, out var priority))
        {
            testsWithPriority.Add((test, priority));
        }

        // Group tests by class
        var testsByClass = testsWithPriority
            .GroupBy(t => t.Test.Context.TestDetails.ClassType)
            .ToList();

        // Sort classes by their minimum test Order
        testsByClass.Sort((a, b) =>
        {
            var aMinOrder = a.Min(t => t.Priority.Order);
            var bMinOrder = b.Min(t => t.Priority.Order);
            return aMinOrder.CompareTo(bMinOrder);
        });

        // Execute class by class
        foreach (var classGroup in testsByClass)
        {
            // Sort tests within the class by Order, then by execution plan order
            var classTests = classGroup.OrderBy(t => t.Priority.Order)
                .ThenBy(t => plan.ExecutionOrder.TryGetValue(t.Test, out var order) ? order : int.MaxValue)
                .Select(t => t.Test)
                .ToList();

            // Execute all tests from this class sequentially
            foreach (var test in classTests)
            {
                await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken);
            }
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
        var testsWithPriority = new List<(AbstractExecutableTest Test, TestPriority Priority)>();
        while (queue.TryDequeue(out var test, out var priority))
        {
            testsWithPriority.Add((test, priority));
        }

        // Group tests by class
        var testsByClass = testsWithPriority
            .GroupBy(t => t.Test.Context.TestDetails.ClassType)
            .ToList();

        // Sort classes by their minimum test Order
        testsByClass.Sort((a, b) =>
        {
            var aMinOrder = a.Min(t => t.Priority.Order);
            var bMinOrder = b.Min(t => t.Priority.Order);
            return aMinOrder.CompareTo(bMinOrder);
        });

        // Execute class by class within this key
        foreach (var classGroup in testsByClass)
        {
            // Sort tests within the class by Order, then by execution plan order
            var classTests = classGroup.OrderBy(t => t.Priority.Order)
                .ThenBy(t => plan.ExecutionOrder.TryGetValue(t.Test, out var order) ? order : int.MaxValue)
                .Select(t => t.Test)
                .ToList();

            // Execute all tests from this class sequentially
            foreach (var test in classTests)
            {
                await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken);
            }
        }
    }

    private async Task ExecuteParallelGroupAsync(SortedDictionary<int, List<AbstractExecutableTest>> orderGroups,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        int? maxParallelism,
        CancellationToken cancellationToken)
    {
        // Execute order groups sequentially
        foreach (var orderGroup in orderGroups.OrderBy(og => og.Key))
        {
            // Use EnumerableAsyncProcessor to execute tests in parallel
            if (maxParallelism.HasValue)
            {
                await orderGroup.Value
                    .ForEachAsync(async test => await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken))
                    .ProcessInParallel(maxParallelism.Value);
            }
            else
            {
                // Adaptive parallelism - no limit specified
                await orderGroup.Value
                    .ForEachAsync(async test => await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken))
                    .ProcessInParallel();
            }
        }
    }

    private async Task ExecuteParallelTestsAsync(IEnumerable<AbstractExecutableTest> tests,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        int? maxParallelism,
        CancellationToken cancellationToken)
    {
        // Use EnumerableAsyncProcessor to execute tests in parallel
        if (maxParallelism.HasValue)
        {
            await tests
                .ForEachAsync(async test => await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken))
                .ProcessInParallel(maxParallelism.Value);
        }
        else
        {
            // Adaptive parallelism - no limit specified
            await tests
                .ForEachAsync(async test => await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken))
                .ProcessInParallel();
        }
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
        try
        {
            await executor.ExecuteTestAsync(test, cancellationToken);
        }
        finally
        {
            completedTests[test] = true;
        }
    }
}
