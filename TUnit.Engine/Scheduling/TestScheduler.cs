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

        var testList = tests as IList<AbstractExecutableTest> ?? tests.ToList();
        if (testList.Count == 0)
        {
            await _logger.LogDebugAsync("No executable tests found");
            return;
        }

        // Group tests by constraints
        var groupedTests = await _groupingService.GroupTestsByConstraintsAsync(testList);

        // Execute tests
        await ExecuteGroupedTestsAsync(groupedTests, executor, cancellationToken);
    }

    private async Task ExecuteGroupedTestsAsync(
        GroupedTests groupedTests,
        ITestExecutor executor,
        CancellationToken cancellationToken)
    {
        var runningTasks = new ConcurrentDictionary<AbstractExecutableTest, Task>();
        var completedTests = new ConcurrentDictionary<AbstractExecutableTest, bool>();

        // Determine parallelism level
        int? maxParallelism;
        if (_configuration.Strategy == ParallelismStrategy.Adaptive)
        {
            // For adaptive strategy, use a sensible default based on processor count
            // This prevents unlimited concurrency which causes thread pool exhaustion
            // Use the configured AdaptiveMaxParallelism value
            maxParallelism = _configuration.AdaptiveMaxParallelism;

            // Ensure we respect the minimum
            maxParallelism = Math.Max(maxParallelism.Value, _configuration.AdaptiveMinParallelism);
        }
        else
        {
            // Fixed strategy uses the configured max parallelism
            maxParallelism = _configuration.MaxParallelism > 0 ? _configuration.MaxParallelism : Environment.ProcessorCount * 4;
        }

        // Process all test groups
        var allTestTasks = new List<Task>();

        // 1. NotInParallel tests (global) - must run one at a time
        if (groupedTests.NotInParallel.Length > 0)
        {
            var globalNotInParallelTask = ExecuteNotInParallelTestsAsync(
                groupedTests.NotInParallel,
                executor,
                runningTasks,
                completedTests,
                cancellationToken);
            allTestTasks.Add(globalNotInParallelTask);
        }

        // 2. Keyed NotInParallel tests - can run in parallel with other keys
        foreach (var (key, tests) in groupedTests.KeyedNotInParallel)
        {
            var keyedTask = ExecuteKeyedNotInParallelTestsAsync(
                tests,
                executor,
                runningTasks,
                completedTests,
                cancellationToken);
            allTestTasks.Add(keyedTask);
        }

        // 3. Parallel groups - can run in parallel within constraints
        foreach (var (groupName, orderedTests) in groupedTests.ParallelGroups)
        {
            var groupTask = ExecuteParallelGroupAsync(orderedTests,
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
        AbstractExecutableTest[] tests,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        CancellationToken cancellationToken)
    {
        // Tests are already sorted by priority from TestGroupingService
        // Group tests by class for execution
        var testsByClass = tests
            .GroupBy(t => t.Context.TestDetails.ClassType)
            .ToList();

        // Execute class by class
        foreach (var classGroup in testsByClass)
        {
            // Tests are already in priority order, just execute them
            var classTests = classGroup.ToList();

            // Execute all tests from this class sequentially
            foreach (var test in classTests)
            {
                await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken);
            }
        }
    }

    private async Task ExecuteKeyedNotInParallelTestsAsync(
        AbstractExecutableTest[] tests,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        CancellationToken cancellationToken)
    {
        // Tests are already sorted by priority from TestGroupingService
        // Group tests by class for execution
        var testsByClass = tests
            .GroupBy(t => t.Context.TestDetails.ClassType)
            .ToList();

        // Execute class by class within this key
        foreach (var classGroup in testsByClass)
        {
            // Tests are already in priority order, just execute them
            var classTests = classGroup.ToList();

            // Execute all tests from this class sequentially
            foreach (var test in classTests)
            {
                await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken);
            }
        }
    }

    private async Task ExecuteParallelGroupAsync((int Order, AbstractExecutableTest[] Tests)[] orderedTests,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        int? maxParallelism,
        CancellationToken cancellationToken)
    {
        // Execute order groups sequentially (already sorted by order)
        foreach (var (order, tests) in orderedTests)
        {
            var processor = tests
                .ForEachAsync(async test => await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken));

            if (maxParallelism is > 0)
            {
                await processor.ProcessInParallel(maxParallelism.Value);
            }
            else
            {
                await processor.ProcessInParallelUnbounded();
            }
        }
    }

    private async Task ExecuteParallelTestsAsync(AbstractExecutableTest[] tests,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        int? maxParallelism,
        CancellationToken cancellationToken)
    {
        var processor = tests
            .ForEachAsync(async test => await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken));

        if (maxParallelism is > 0)
        {
            await processor.ProcessInParallel(maxParallelism.Value);
        }
        else
        {
            await processor.ProcessInParallelUnbounded();
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
