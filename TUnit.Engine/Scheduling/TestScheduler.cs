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
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _constraintSemaphores = new();


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

        // Report any tests that were marked as failed during planning
        foreach (var test in plan.AllTests)
        {
            if (test.State == TestState.Failed && test.Result != null)
            {
                await _messageBus.Failed(test.Context, test.Result.Exception ?? new InvalidOperationException("Test failed during planning"), test.Result.Start ?? DateTimeOffset.UtcNow);
            }
        }

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

        int? maxParallelism = null;
        if (_configuration.Strategy != ParallelismStrategy.Adaptive)
        {
            maxParallelism = _configuration.MaxParallelism > 0 ? _configuration.MaxParallelism : Environment.ProcessorCount * 4;
        }

        var allTestTasks = new List<Task>();

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

        if (groupedTests.KeyedNotInParallel.Count > 0)
        {
            var keyedTask = ExecuteAllKeyedNotInParallelTestsAsync(
                plan,
                groupedTests.KeyedNotInParallel,
                executor,
                runningTasks,
                completedTests,
                maxParallelism,
                cancellationToken);
            allTestTasks.Add(keyedTask);
        }

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

        var testsByClass = testsWithPriority
            .GroupBy(t => t.Test.Context.TestDetails.ClassType)
            .ToList();

        testsByClass.Sort((a, b) =>
        {
            var aMinOrder = a.Min(t => t.Priority.Order);
            var bMinOrder = b.Min(t => t.Priority.Order);
            return aMinOrder.CompareTo(bMinOrder);
        });

        foreach (var classGroup in testsByClass)
        {
            var classTests = classGroup.OrderBy(t => t.Priority.Order)
                .ThenBy(t => plan.ExecutionOrder.TryGetValue(t.Test, out var order) ? order : int.MaxValue)
                .Select(t => t.Test)
                .ToList();

            foreach (var test in classTests)
            {
                await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken);
            }
        }
    }

    private async Task ExecuteAllKeyedNotInParallelTestsAsync(
        ExecutionPlan plan,
        IDictionary<string, PriorityQueue<AbstractExecutableTest, TestPriority>> keyedQueues,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        int? maxParallelism,
        CancellationToken cancellationToken)
    {
        // Process each keyed queue sequentially within the key
        // Different keys can run in parallel
        var keyedTasks = new List<Task>();
        var processedTests = new ConcurrentDictionary<AbstractExecutableTest, bool>();
        
        foreach (var kvp in keyedQueues)
        {
            var key = kvp.Key;
            var queue = kvp.Value;
            
            var keyTask = Task.Run(async () =>
            {
                var testsWithPriority = new List<(AbstractExecutableTest Test, TestPriority Priority)>();
                while (queue.TryDequeue(out var test, out var priority))
                {
                    testsWithPriority.Add((test, priority));
                }
                
                // Sort tests within this key by priority and order
                testsWithPriority.Sort((a, b) =>
                {
                    var priorityComp = b.Priority.Priority.CompareTo(a.Priority.Priority);
                    if (priorityComp != 0) return priorityComp;
                    
                    var orderComp = a.Priority.Order.CompareTo(b.Priority.Order);
                    if (orderComp != 0) return orderComp;
                    
                    return plan.ExecutionOrder.TryGetValue(a.Test, out var aOrder) && 
                           plan.ExecutionOrder.TryGetValue(b.Test, out var bOrder) 
                           ? aOrder.CompareTo(bOrder) : 0;
                });
                
                // Execute tests for this key sequentially
                foreach (var (test, _) in testsWithPriority)
                {
                    // Only execute if not already processed by another key
                    // This handles tests with multiple constraint keys
                    if (!processedTests.TryAdd(test, true))
                    {
                        continue; // Already processed by another key
                    }
                    
                    // Get ALL constraint keys for this test (may be more than just this queue's key)
                    var constraintKeys = GetConstraintKeys(test);
                    var semaphores = new List<SemaphoreSlim>();
                    
                    // Collect all semaphores for this test's constraint keys (sorted to prevent deadlock)
                    foreach (var constraintKey in constraintKeys.OrderBy(k => k))
                    {
                        semaphores.Add(_constraintSemaphores.GetOrAdd(constraintKey, _ => new SemaphoreSlim(1, 1)));
                    }
                    
                    // Acquire all semaphores
                    foreach (var sem in semaphores)
                    {
                        await sem.WaitAsync(cancellationToken);
                    }
                    
                    try
                    {
                        await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken);
                    }
                    finally
                    {
                        // Release all semaphores
                        foreach (var sem in semaphores)
                        {
                            sem.Release();
                        }
                    }
                }
            }, cancellationToken);
            
            keyedTasks.Add(keyTask);
        }
        
        // Wait for all keyed queues to complete
        await Task.WhenAll(keyedTasks);
    }
    
    private List<string> GetConstraintKeys(AbstractExecutableTest test)
    {
        if (test.Context.ParallelConstraint is NotInParallelConstraint notInParallel)
        {
            return notInParallel.NotInParallelConstraintKeys.ToList();
        }
        return new List<string>();
    }


    private async Task ExecuteParallelGroupAsync(SortedDictionary<int, List<AbstractExecutableTest>> orderGroups,
        ITestExecutor executor,
        ConcurrentDictionary<AbstractExecutableTest, Task> runningTasks,
        ConcurrentDictionary<AbstractExecutableTest, bool> completedTests,
        int? maxParallelism,
        CancellationToken cancellationToken)
    {
        foreach (var orderGroup in orderGroups.OrderBy(og => og.Key))
        {
            if (maxParallelism.HasValue)
            {
                await orderGroup.Value
                    .ForEachAsync(async test => await ExecuteTestWhenReadyAsync(test, executor, runningTasks, completedTests, cancellationToken))
                    .ProcessInParallel(maxParallelism.Value);
            }
            else
            {
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
}
