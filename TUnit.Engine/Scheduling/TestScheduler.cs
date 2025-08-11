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
    private readonly ParallelLimitLockProvider _parallelLimitLockProvider;

    public TestScheduler(
        TUnitFrameworkLogger logger,
        ITestGroupingService groupingService,
        ITUnitMessageBus messageBus,
        ICommandLineOptions commandLineOptions,
        ParallelLimitLockProvider parallelLimitLockProvider)
    {
        _logger = logger;
        _groupingService = groupingService;
        _messageBus = messageBus;
        _commandLineOptions = commandLineOptions;
        _parallelLimitLockProvider = parallelLimitLockProvider;
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
            await _logger.LogDebugAsync("No executable tests found").ConfigureAwait(false);
            return;
        }

        // Detect and handle circular dependencies
        var circularTests = DetectCircularDependencies(testList);
        foreach (var test in circularTests)
        {
            test.State = TestState.Failed;
            test.Result = new TestResult
            {
                State = TestState.Failed,
                Exception = new InvalidOperationException($"Test '{test.TestId}' has circular dependencies and cannot be executed"),
                ComputerName = Environment.MachineName,
                Start = DateTimeOffset.UtcNow,
                End = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero
            };
            
            // Complete the test's task to prevent waiting forever
            test.Context.InternalExecutableTest._taskCompletionSource?.TrySetResult();
            
            // Report the failure
            await _messageBus.Failed(test.Context, test.Result.Exception, test.Result.Start ?? DateTimeOffset.UtcNow).ConfigureAwait(false);
        }

        // Remove circular tests from the list
        var executableTests = testList.Where(t => !circularTests.Contains(t)).ToList();
        if (executableTests.Count == 0)
        {
            await _logger.LogDebugAsync("No executable tests found after removing circular dependencies").ConfigureAwait(false);
            return;
        }

        // Group tests by constraints
        var groupedTests = await _groupingService.GroupTestsByConstraintsAsync(executableTests).ConfigureAwait(false);

        // Execute tests
        await ExecuteGroupedTestsAsync(groupedTests, executor, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteGroupedTestsAsync(
        GroupedTests groupedTests,
        ITestExecutor executor,
        CancellationToken cancellationToken)
    {
        var runningTasks = new ConcurrentDictionary<AbstractExecutableTest, Task>();
        var completedTests = new ConcurrentDictionary<AbstractExecutableTest, bool>();
        // KeyedConstraintManager removed - using simpler task tracking approach

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
        
        // If no explicit limit is set, use a sensible default to prevent thread pool exhaustion
        // This matches Microsoft's Parallel.ForEachAsync approach
        if (!maxParallelism.HasValue)
        {
            maxParallelism = Environment.ProcessorCount;
        }

        // Collect all tests from all groups to ensure dependencies can be resolved
        var allTests = new HashSet<AbstractExecutableTest>();
        allTests.UnionWith(groupedTests.NotInParallel);
        allTests.UnionWith(groupedTests.KeyedNotInParallel.SelectMany(k => k.Tests));
        allTests.UnionWith(groupedTests.ParallelGroups.SelectMany(g => g.OrderedTests.SelectMany(o => o.Tests)));
        allTests.UnionWith(groupedTests.Parallel);

        // Start dependency resolution tasks for all tests upfront
        // This ensures dependencies are available when needed
        foreach (var test in allTests)
        {
            // Initialize the test's completion task if not already initialized
            // The field is readonly, so we need to check if it's already initialized
            // It's initialized when the test is created, so this is just a safety check
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

        // 4. Parallel tests - can all run in parallel
        if (groupedTests.Parallel.Length > 0)
        {
            var parallelTask = ExecuteParallelTestsAsync(groupedTests.Parallel,
                executor,
                runningTasks,
                completedTests,
                maxParallelism,
                cancellationToken);
            allTestTasks.Add(parallelTask);
        }

        // Log which task groups were created
        var taskGroupInfo = new List<string>();
        if (groupedTests.NotInParallel.Length > 0)
            taskGroupInfo.Add($"NotInParallel({groupedTests.NotInParallel.Length})");
        if (groupedTests.KeyedNotInParallel.Length > 0)
            taskGroupInfo.Add($"KeyedNotInParallel({groupedTests.KeyedNotInParallel.Length})");
        foreach (var (groupName, _) in groupedTests.ParallelGroups)
            taskGroupInfo.Add($"ParallelGroup({groupName})");
        if (groupedTests.Parallel.Length > 0)
            taskGroupInfo.Add($"Parallel({groupedTests.Parallel.Length})");

        await Task.WhenAll(allTestTasks).ConfigureAwait(false);
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
        int classIndex = 0;
        foreach (var classGroup in testsByClass)
        {
            var className = classGroup.Key.FullName ?? classGroup.Key.Name;
            var classTestsList = classGroup.ToList();

            // Sort tests within the class based on dependencies (topological sort)
            var classTests = SortTestsByDependencies(classTestsList);

            // Execute tests sequentially within each class for NotInParallel
            foreach (var test in classTests)
            {
                await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken).ConfigureAwait(false);
            }
            classIndex++;
        }
    }

    private async Task ExecuteAllKeyedNotInParallelTestsAsync(
        (string Key, AbstractExecutableTest[] Tests)[] keyedTests,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
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

        // Track all test execution tasks
        var testTasks = new List<Task>();

        // Execute tests with constraint checking
        // We need to start tests that can run in parallel, but wait for conflicting ones
        var runningKeyedTasks = new Dictionary<string, Task>();
        
        foreach (var test in sortedTests)
        {
            var testKeys = testToKeys[test];

            // Wait for any running tests that share any of our constraint keys
            var conflictingTasks = new List<Task>();
            foreach (var key in testKeys)
            {
                if (runningKeyedTasks.TryGetValue(key, out var runningTask))
                {
                    conflictingTasks.Add(runningTask);
                }
            }
            
            if (conflictingTasks.Count > 0)
            {
                await Task.WhenAll(conflictingTasks).ConfigureAwait(false);
            }

            // Execute the test
            var task = ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken);
            
            // Track this task for all its keys
            foreach (var key in testKeys)
            {
                runningKeyedTasks[key] = task;
            }
            
            testTasks.Add(task);
        }

        // Wait for all test execution tasks to complete
        await Task.WhenAll(testTasks).ConfigureAwait(false);
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
            // Collect all tests and their dependencies for this group
            var allTestsToSchedule = new HashSet<AbstractExecutableTest>();
            var queue = new Queue<AbstractExecutableTest>(tests);
            
            while (queue.Count > 0)
            {
                var test = queue.Dequeue();
                if (allTestsToSchedule.Add(test))
                {
                    // Add dependencies to queue if not already scheduled
                    foreach (var dep in test.Dependencies)
                    {
                        if (!allTestsToSchedule.Contains(dep.Test))
                        {
                            queue.Enqueue(dep.Test);
                        }
                    }
                }
            }
            
            // Start tests with parallelism control
            var groupTasks = new List<Task>();
            
            if (maxParallelism.HasValue)
            {
                // Use semaphore to limit parallelism
                using var semaphore = new SemaphoreSlim(maxParallelism.Value, maxParallelism.Value);
                foreach (var test in allTestsToSchedule)
                {
                    var task = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                        try
                        {
                            await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken);
                    groupTasks.Add(task);
                }
            }
            else
            {
                // No limit - start all immediately
                foreach (var test in allTestsToSchedule)
                {
                    var task = Task.Run(async () => await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken).ConfigureAwait(false), cancellationToken);
                    groupTasks.Add(task);
                }
            }
            
            // Wait for the original tests in this order group to complete
            var originalTestTasks = tests.Select(t => t.CompletionTask).ToArray();
            await Task.WhenAll(originalTestTasks).ConfigureAwait(false);
        }
    }

    private async Task ExecuteParallelTestsAsync(AbstractExecutableTest[] tests,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        int? maxParallelism,
        CancellationToken cancellationToken)
    {
        // For parallel tests, we need to ensure all tests (including dependencies) are scheduled
        // Collect all tests and their dependencies
        var allTestsToSchedule = new HashSet<AbstractExecutableTest>();
        var queue = new Queue<AbstractExecutableTest>(tests);
        
        while (queue.Count > 0)
        {
            var test = queue.Dequeue();
            if (allTestsToSchedule.Add(test))
            {
                // Add dependencies to queue if not already scheduled
                foreach (var dep in test.Dependencies)
                {
                    if (!allTestsToSchedule.Contains(dep.Test))
                    {
                        queue.Enqueue(dep.Test);
                    }
                }
            }
        }
        
        // Start tests with parallelism control
        var allTasks = new List<Task>();
        
        if (maxParallelism.HasValue)
        {
            // Use semaphore to limit parallelism
            var semaphore = new SemaphoreSlim(maxParallelism.Value, maxParallelism.Value);
            foreach (var test in allTestsToSchedule)
            {
                var task = Task.Run(async () =>
                {
                    await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);
                allTasks.Add(task);
            }
        }
        else
        {
            // No limit - start all immediately
            foreach (var test in allTestsToSchedule)
            {
                var task = Task.Run(async () => await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken).ConfigureAwait(false), cancellationToken);
                allTasks.Add(task);
            }
        }
        
        // Only wait for the originally requested tests (not dependencies from other groups)
        var originalTestTasks = tests.Select(t => t.CompletionTask).ToArray();
        await Task.WhenAll(originalTestTasks).ConfigureAwait(false);
    }

    private async Task ExecuteTestWhenReadyAsync(AbstractExecutableTest test,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        CancellationToken cancellationToken)
    {
        // Thread-safe check and state transition
        lock (test.Context.Lock)
        {
            if (test.State != TestState.NotStarted)
            {
                // Test was already started by another thread, just wait for completion
                // (Note: We'll wait for completion below after releasing the lock)
                goto WaitForCompletion;
            }
            test.State = TestState.WaitingForDependencies;
        }

        if (test.State == TestState.Failed)
        {
            await _messageBus.Failed(test.Context,
                    test.Result?.Exception ?? new InvalidOperationException("Test was marked as failed before execution"),
                    test.StartTime ?? DateTimeOffset.UtcNow).ConfigureAwait(false);

            return;
        }

        if (test.State == TestState.Skipped)
        {
            await _messageBus.Skipped(test.Context, "Test was skipped").ConfigureAwait(false);
            return;
        }

        // Wait for dependencies
        var dependencyTasks = new List<Task>();
        foreach (var dependency in test.Dependencies)
        {
            var depTest = dependency.Test;
            dependencyTasks.Add(depTest.CompletionTask);
        }

        // Wait for all dependencies to complete
        if (dependencyTasks.Count > 0)
        {
            await Task.WhenAll(dependencyTasks).ConfigureAwait(false);
        }

        var executionTask = ExecuteTestDirectlyAsync(test, executor, completedTests, cancellationToken);
        runningTasks[test] = executionTask;
        await executionTask.ConfigureAwait(false);
        runningTasks.TryRemove(test, out _);
        return;

    WaitForCompletion:
        // Test was already started by another thread, wait for it to complete
        await test.CompletionTask.ConfigureAwait(false);
    }

    private async Task ExecuteTestDirectlyAsync(AbstractExecutableTest test, ITestExecutor executor, ConcurrentDictionary<AbstractExecutableTest, bool> completedTests, CancellationToken cancellationToken)
    {
        // Acquire semaphore for parallel limit if configured
        SemaphoreSlim? parallelLimitSemaphore = null;
        if (test.Context.ParallelLimiter != null)
        {
            parallelLimitSemaphore = _parallelLimitLockProvider.GetLock(test.Context.ParallelLimiter);
            await parallelLimitSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            await executor.ExecuteTestAsync(test, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // Release semaphore if we acquired one
            parallelLimitSemaphore?.Release();
            
            completedTests[test] = true;

            // CRITICAL: Ensure TaskCompletionSource is set even if executor throws
            // This is a last-resort safety net
            if (!test.Context.InternalExecutableTest._taskCompletionSource.Task.IsCompleted)
            {
                test.Context.InternalExecutableTest._taskCompletionSource.TrySetResult();
            }
        }
    }

    /// <summary>
    /// Sorts tests based on their dependencies using topological sort
    /// </summary>
    private List<AbstractExecutableTest> SortTestsByDependencies(List<AbstractExecutableTest> tests)
    {
        // If no tests have dependencies, return original order
        if (!tests.Any(t => t.Dependencies.Length > 0))
        {
            return tests;
        }

        var sorted = new List<AbstractExecutableTest>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        // Build a map of test IDs to tests, handling potential duplicates gracefully
        var testMap = new Dictionary<string, AbstractExecutableTest>();
        var seenTests = new HashSet<string>();

        foreach (var test in tests)
        {
            if (!seenTests.Add(test.TestId))
            {
                // Skip duplicate - this shouldn't happen but let's be defensive
                continue;
            }
            testMap[test.TestId] = test;
        }

        void Visit(AbstractExecutableTest test)
        {
            if (visited.Contains(test.TestId))
            {
                return;
            }

            if (visiting.Contains(test.TestId))
            {
                // Circular dependency detected - just add it to avoid infinite loop
                return;
            }

            visiting.Add(test.TestId);

            // Visit dependencies first (only if they're in the same class group)
            foreach (var dependency in test.Dependencies)
            {
                if (testMap.TryGetValue(dependency.Test.TestId, out var depTest))
                {
                    Visit(depTest);
                }
            }

            visiting.Remove(test.TestId);
            visited.Add(test.TestId);
            sorted.Add(test);
        }

        // Visit all tests that are in our map (skipping any duplicates)
        foreach (var test in testMap.Values)
        {
            Visit(test);
        }

        return sorted;
    }

    private HashSet<AbstractExecutableTest> DetectCircularDependencies(IList<AbstractExecutableTest> tests)
    {
        var circularTests = new HashSet<AbstractExecutableTest>();
        var visitState = new Dictionary<string, VisitState>();
        
        // Build test map, handling potential duplicates by keeping the first occurrence
        var testMap = new Dictionary<string, AbstractExecutableTest>();
        foreach (var test in tests)
        {
            if (!testMap.ContainsKey(test.TestId))
            {
                testMap[test.TestId] = test;
            }
            else
            {
                // Log warning about duplicate test ID
                _logger.LogWarningAsync($"Duplicate test ID detected: {test.TestId}. This indicates a bug in test ID generation.").GetAwaiter().GetResult();
            }
        }
        
        foreach (var test in tests)
        {
            if (!visitState.ContainsKey(test.TestId))
            {
                var cycle = new List<AbstractExecutableTest>();
                if (HasCycle(test, testMap, visitState, cycle))
                {
                    // Add all tests in the cycle to the circular tests set
                    foreach (var cycleTest in cycle)
                    {
                        circularTests.Add(cycleTest);
                    }
                }
            }
        }
        
        return circularTests;
    }
    
    private bool HasCycle(
        AbstractExecutableTest test,
        Dictionary<string, AbstractExecutableTest> testMap,
        Dictionary<string, VisitState> visitState,
        List<AbstractExecutableTest> currentPath)
    {
        visitState[test.TestId] = VisitState.Visiting;
        currentPath.Add(test);
        
        foreach (var dependency in test.Dependencies)
        {
            var depTestId = dependency.Test.TestId;
            
            // Only check dependencies that are in our test set
            if (!testMap.ContainsKey(depTestId))
                continue;
                
            if (!visitState.TryGetValue(depTestId, out var state))
            {
                // Not visited yet, recurse
                if (HasCycle(testMap[depTestId], testMap, visitState, currentPath))
                {
                    return true;
                }
            }
            else if (state == VisitState.Visiting)
            {
                // Found a cycle - the dependency is in the current path
                // Trim the path to only include the cycle
                var cycleStartIndex = currentPath.FindIndex(t => t.TestId == depTestId);
                if (cycleStartIndex >= 0)
                {
                    currentPath.RemoveRange(0, cycleStartIndex);
                }
                return true;
            }
            // If state == VisitState.Visited, this dependency was already fully processed
        }
        
        visitState[test.TestId] = VisitState.Visited;
        currentPath.Remove(test);
        return false;
    }
    
    private enum VisitState
    {
        Visiting,
        Visited
    }

}
