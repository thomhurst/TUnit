using System.Collections.Concurrent;
using EnumerableAsyncProcessor.Extensions;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.CommandLine;
using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.CommandLineProviders;
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
    private readonly ICommandLineOptions _commandLineOptions;

    public TestScheduler(
        TUnitFrameworkLogger logger,
        ITestGroupingService groupingService,
        ITUnitMessageBus messageBus,
        ICommandLineOptions commandLineOptions)
    {
        _logger = logger;
        _groupingService = groupingService;
        _messageBus = messageBus;
        _commandLineOptions = commandLineOptions;
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
        var keyedConstraintManager = new KeyedConstraintManager();

        int? maxParallelism = null;
        if (_commandLineOptions.TryGetOptionArgumentList(
                MaximumParallelTestsCommandProvider.MaximumParallelTests,
                out var args) && args.Length > 0)
        {
            if (int.TryParse(args[0], out var maxParallelTests) && maxParallelTests > 0)
            {
                maxParallelism = maxParallelTests;
            }
        }

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

        // 2. Keyed NotInParallel tests - handle tests with overlapping constraint keys
        if (groupedTests.KeyedNotInParallel.Length > 0)
        {
            var keyedTask = ExecuteAllKeyedNotInParallelTestsAsync(
                groupedTests.KeyedNotInParallel,
                executor,
                runningTasks,
                completedTests,
                keyedConstraintManager,
                maxParallelism,
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

        var parallelTask = ExecuteParallelTestsAsync(groupedTests.Parallel,
            executor,
            runningTasks,
            completedTests,
            maxParallelism,
            cancellationToken);
        allTestTasks.Add(parallelTask);

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

            foreach (var test in classTests)
            {
                await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken);
            }
        }
    }

    private async Task ExecuteAllKeyedNotInParallelTestsAsync(
        (string Key, AbstractExecutableTest[] Tests)[] keyedTests,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        KeyedConstraintManager constraintManager,
        int? maxParallelism,
        CancellationToken cancellationToken)
    {
        // Get all unique tests (some tests may appear in multiple keys)
        var allTests = new HashSet<AbstractExecutableTest>();
        var testToKeys = new Dictionary<AbstractExecutableTest, List<string>>();
        
        foreach (var (key, tests) in keyedTests)
        {
            foreach (var test in tests)
            {
                allTests.Add(test);
                if (!testToKeys.TryGetValue(test, out var keys))
                {
                    keys = new List<string>();
                    testToKeys[test] = keys;
                }
                keys.Add(key);
            }
        }

        // Sort all unique tests by priority (Critical=5 comes first, Low=1 comes last)
        // Then by NotInParallel order attribute
        var sortedTests = allTests
            .OrderByDescending(t => t.Context.ExecutionPriority)
            .ThenBy(t => {
                var constraint = t.Context.ParallelConstraint as NotInParallelConstraint;
                return constraint?.Order ?? int.MaxValue / 2;
            })
            .ToList();

        // Execute tests with constraint checking
        foreach (var test in sortedTests)
        {
            var testKeys = testToKeys[test];
            
            // Wait for any running tests that share any of our constraint keys
            await constraintManager.WaitForKeysAsync(testKeys, cancellationToken);
            
            // Mark our keys as in use
            constraintManager.AcquireKeys(testKeys, test);
            
            // Execute the test
            var task = ExecuteTestAndReleaseKeysAsync(test, executor, runningTasks, completedTests, constraintManager, testKeys, cancellationToken);
            
            // Don't await the task - let it run in parallel with other non-conflicting tests
        }

        // Wait for all tests to complete
        await constraintManager.WaitForAllTestsAsync();
    }

    private async Task ExecuteTestAndReleaseKeysAsync(
        AbstractExecutableTest test,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        KeyedConstraintManager constraintManager,
        List<string> testKeys,
        CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken);
        }
        finally
        {
            constraintManager.ReleaseKeys(testKeys, test);
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

    /// <summary>
    /// Manages constraint keys to ensure tests with overlapping keys don't run in parallel
    /// </summary>
    private class KeyedConstraintManager
    {
        private readonly Dictionary<string, TaskCompletionSource<bool>> _keyLocks = new();
        private readonly List<Task> _allTestTasks = new();
        private readonly object _lockObject = new();

        public async Task WaitForKeysAsync(List<string> keys, CancellationToken cancellationToken)
        {
            List<Task> tasksToWait = new();
            
            lock (_lockObject)
            {
                foreach (var key in keys)
                {
                    if (_keyLocks.TryGetValue(key, out var tcs))
                    {
                        tasksToWait.Add(tcs.Task);
                    }
                }
            }

            if (tasksToWait.Count > 0)
            {
                await Task.WhenAll(tasksToWait).ConfigureAwait(false);
            }
        }

        public void AcquireKeys(List<string> keys, AbstractExecutableTest test)
        {
            lock (_lockObject)
            {
                var tcs = new TaskCompletionSource<bool>();
                _allTestTasks.Add(tcs.Task);
                
                foreach (var key in keys)
                {
                    _keyLocks[key] = tcs;
                }
            }
        }

        public void ReleaseKeys(List<string> keys, AbstractExecutableTest test)
        {
            lock (_lockObject)
            {
                foreach (var key in keys)
                {
                    if (_keyLocks.TryGetValue(key, out var tcs))
                    {
                        _keyLocks.Remove(key);
                        tcs.TrySetResult(true);
                    }
                }
            }
        }

        public async Task WaitForAllTestsAsync()
        {
            List<Task> tasks;
            lock (_lockObject)
            {
                tasks = new List<Task>(_allTestTasks);
            }
            
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }
    }
}
