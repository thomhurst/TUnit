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
            
            await _messageBus.Failed(test.Context, test.Result.Exception, test.Result.Start ?? DateTimeOffset.UtcNow).ConfigureAwait(false);
        }

        var executableTests = testList.Where(t => !circularTests.Contains(t)).ToList();
        if (executableTests.Count == 0)
        {
            await _logger.LogDebugAsync("No executable tests found after removing circular dependencies").ConfigureAwait(false);
            return;
        }

        foreach (var test in executableTests)
        {
            test.ExecutorDelegate = CreateTestExecutor(executor);
            test.ExecutionCancellationToken = cancellationToken;
        }

        var groupedTests = await _groupingService.GroupTestsByConstraintsAsync(executableTests).ConfigureAwait(false);

        await ExecuteGroupedTestsAsync(groupedTests, cancellationToken).ConfigureAwait(false);
    }

    private Func<AbstractExecutableTest, CancellationToken, Task> CreateTestExecutor(ITestExecutor executor)
    {
        return async (test, cancellationToken) =>
        {
            // First wait for all dependencies to complete
            foreach (var dependency in test.Dependencies)
            {
                try
                {
                    await dependency.Test.ExecutionTask.ConfigureAwait(false);
                    
                    // Check if dependency failed and we should skip
                    if (dependency.Test.State == TestState.Failed && !dependency.ProceedOnFailure)
                    {
                        test.State = TestState.Skipped;
                        test.Result = new TestResult
                        {
                            State = TestState.Skipped,
                            Exception = new InvalidOperationException($"Skipped due to failed dependency: {dependency.Test.TestId}"),
                            ComputerName = Environment.MachineName,
                            Start = DateTimeOffset.UtcNow,
                            End = DateTimeOffset.UtcNow,
                            Duration = TimeSpan.Zero
                        };
                        await _messageBus.Skipped(test.Context, "Skipped due to failed dependencies").ConfigureAwait(false);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync($"Error waiting for dependency {dependency.Test.TestId}: {ex}").ConfigureAwait(false);
                    
                    if (!dependency.ProceedOnFailure)
                    {
                        test.State = TestState.Skipped;
                        test.Result = new TestResult
                        {
                            State = TestState.Skipped,
                            Exception = ex,
                            ComputerName = Environment.MachineName,
                            Start = DateTimeOffset.UtcNow,
                            End = DateTimeOffset.UtcNow,
                            Duration = TimeSpan.Zero
                        };
                        await _messageBus.Skipped(test.Context, "Skipped due to failed dependencies").ConfigureAwait(false);
                        return;
                    }
                }
            }

            // Acquire parallel limit semaphore if needed
            SemaphoreSlim? parallelLimitSemaphore = null;
            if (test.Context.ParallelLimiter != null)
            {
                parallelLimitSemaphore = _parallelLimitLockProvider.GetLock(test.Context.ParallelLimiter);
                await parallelLimitSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            try
            {
                // Execute the actual test
                await executor.ExecuteTestAsync(test, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                parallelLimitSemaphore?.Release();
            }
        };
    }

    private async Task ExecuteGroupedTestsAsync(
        GroupedTests groupedTests,
        CancellationToken cancellationToken)
    {
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
        if (!maxParallelism.HasValue)
        {
            maxParallelism = Environment.ProcessorCount;
        }

        var allTestTasks = new List<Task>();

        // 1. NotInParallel tests (global) - must run one at a time
        if (groupedTests.NotInParallel.Length > 0)
        {
            var globalNotInParallelTask = ExecuteNotInParallelTestsAsync(
                groupedTests.NotInParallel,
                cancellationToken);
            allTestTasks.Add(globalNotInParallelTask);
        }

        // 2. Keyed NotInParallel tests
        if (groupedTests.KeyedNotInParallel.Length > 0)
        {
            var keyedTask = ExecuteKeyedNotInParallelTestsAsync(
                groupedTests.KeyedNotInParallel,
                cancellationToken);
            allTestTasks.Add(keyedTask);
        }

        // 3. Parallel groups
        foreach (var (groupName, orderedTests) in groupedTests.ParallelGroups)
        {
            var groupTask = ExecuteParallelGroupAsync(
                orderedTests,
                maxParallelism,
                cancellationToken);
            allTestTasks.Add(groupTask);
        }

        // 4. Parallel tests - can all run in parallel
        if (groupedTests.Parallel.Length > 0)
        {
            var parallelTask = ExecuteParallelTestsAsync(
                groupedTests.Parallel,
                maxParallelism,
                cancellationToken);
            allTestTasks.Add(parallelTask);
        }

        await Task.WhenAll(allTestTasks).ConfigureAwait(false);
    }

    private async Task ExecuteNotInParallelTestsAsync(
        AbstractExecutableTest[] tests,
        CancellationToken cancellationToken)
    {
        var testsByClass = tests
            .GroupBy(t => t.Context.TestDetails.ClassType)
            .ToList();

        foreach (var classGroup in testsByClass)
        {
            var classTests = classGroup
                .OrderBy(t => 
                {
                    var constraint = t.Context.ParallelConstraint as NotInParallelConstraint;
                    return constraint?.Order ?? int.MaxValue / 2;
                })
                .ToList();

            foreach (var test in classTests)
            {
                await test.ExecutionTask.ConfigureAwait(false);
            }
        }
    }

    private async Task ExecuteKeyedNotInParallelTestsAsync(
        (string Key, AbstractExecutableTest[] Tests)[] keyedTests,
        CancellationToken cancellationToken)
    {
        // Get all unique tests
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

        // Sort tests by priority
        var sortedTests = allTests
            .OrderByDescending(t => t.Context.ExecutionPriority)
            .ThenBy(t => {
                var constraint = t.Context.ParallelConstraint as NotInParallelConstraint;
                return constraint?.Order ?? int.MaxValue / 2;
            })
            .ToList();

        // Track running tasks by key
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

            // Start the test execution
            var task = test.ExecutionTask;
            
            // Track this task for all its keys
            foreach (var key in testKeys)
            {
                runningKeyedTasks[key] = task;
            }
        }

        // Wait for all tests to complete
        await Task.WhenAll(allTests.Select(t => t.ExecutionTask)).ConfigureAwait(false);
    }

    private async Task ExecuteParallelGroupAsync(
        (int Order, AbstractExecutableTest[] Tests)[] orderedTests,
        int? maxParallelism,
        CancellationToken cancellationToken)
    {
        // Execute order groups sequentially
        foreach (var (order, tests) in orderedTests)
        {
            if (maxParallelism.HasValue)
            {
                // Use semaphore to limit parallelism
                using var semaphore = new SemaphoreSlim(maxParallelism.Value, maxParallelism.Value);
                
                var tasks = tests.Select(async test =>
                {
                    await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        await test.ExecutionTask.ConfigureAwait(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            else
            {
                // No limit - start all and wait
                await Task.WhenAll(tests.Select(t => t.ExecutionTask)).ConfigureAwait(false);
            }
        }
    }

    private async Task ExecuteParallelTestsAsync(
        AbstractExecutableTest[] tests,
        int? maxParallelism,
        CancellationToken cancellationToken)
    {
        if (maxParallelism.HasValue)
        {
            // Use semaphore to limit parallelism
            using var semaphore = new SemaphoreSlim(maxParallelism.Value, maxParallelism.Value);
            
            var tasks = tests.Select(async test =>
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await test.ExecutionTask.ConfigureAwait(false);
                }
                finally
                {
                    semaphore.Release();
                }
            });
            
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        else
        {
            // No limit - just wait for all
            await Task.WhenAll(tests.Select(t => t.ExecutionTask)).ConfigureAwait(false);
        }
    }

    private HashSet<AbstractExecutableTest> DetectCircularDependencies(IList<AbstractExecutableTest> tests)
    {
        var circularTests = new HashSet<AbstractExecutableTest>();
        var visitState = new Dictionary<string, VisitState>();
        
        // Build test map
        var testMap = new Dictionary<string, AbstractExecutableTest>();
        foreach (var test in tests)
        {
            if (!testMap.ContainsKey(test.TestId))
            {
                testMap[test.TestId] = test;
            }
        }
        
        foreach (var test in tests)
        {
            if (!visitState.ContainsKey(test.TestId))
            {
                var cycle = new List<AbstractExecutableTest>();
                if (HasCycle(test, testMap, visitState, cycle))
                {
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
            
            if (!testMap.ContainsKey(depTestId))
                continue;
                
            if (!visitState.TryGetValue(depTestId, out var state))
            {
                if (HasCycle(testMap[depTestId], testMap, visitState, currentPath))
                {
                    return true;
                }
            }
            else if (state == VisitState.Visiting)
            {
                var cycleStartIndex = currentPath.FindIndex(t => t.TestId == depTestId);
                if (cycleStartIndex >= 0)
                {
                    currentPath.RemoveRange(0, cycleStartIndex);
                }
                return true;
            }
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