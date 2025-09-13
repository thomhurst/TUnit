using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Logging;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;
using TUnit.Engine.Services;
using TUnit.Engine.Services.TestExecution;

namespace TUnit.Engine.Scheduling;

internal sealed class TestScheduler : ITestScheduler
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestGroupingService _groupingService;
    private readonly ITUnitMessageBus _messageBus;
    private readonly ICommandLineOptions _commandLineOptions;
    private readonly ParallelLimitLockProvider _parallelLimitLockProvider;
    private readonly TestStateManager _testStateManager;
    private readonly CircularDependencyDetector _circularDependencyDetector;

    public TestScheduler(
        TUnitFrameworkLogger logger,
        ITestGroupingService groupingService,
        ITUnitMessageBus messageBus,
        ICommandLineOptions commandLineOptions,
        ParallelLimitLockProvider parallelLimitLockProvider,
        TestStateManager testStateManager,
        CircularDependencyDetector circularDependencyDetector)
    {
        _logger = logger;
        _groupingService = groupingService;
        _messageBus = messageBus;
        _commandLineOptions = commandLineOptions;
        _parallelLimitLockProvider = parallelLimitLockProvider;
        _testStateManager = testStateManager;
        _circularDependencyDetector = circularDependencyDetector;
    }

    public async Task ScheduleAndExecuteAsync(
        IEnumerable<AbstractExecutableTest> tests,
        TestRunner runner,
        CancellationToken cancellationToken)
    {
        if (tests == null)
        {
            throw new ArgumentNullException(nameof(tests));
        }
        if (runner == null)
        {
            throw new ArgumentNullException(nameof(runner));
        }

        var testList = tests as IList<AbstractExecutableTest> ?? tests.ToList();
        if (testList.Count == 0)
        {
            await _logger.LogDebugAsync("No executable tests found").ConfigureAwait(false);
            return;
        }

        await _logger.LogDebugAsync($"Scheduling execution of {testList.Count} tests").ConfigureAwait(false);

        var circularDependencies = _circularDependencyDetector.DetectCircularDependencies(testList);

        foreach (var (test, dependencyChain) in circularDependencies)
        {
            var exception = new CircularDependencyException($"Circular dependency detected: {string.Join(" -> ", dependencyChain.Select(d => d.TestId))}");
            await _testStateManager.MarkCircularDependencyFailedAsync(test, exception).ConfigureAwait(false);
            await _messageBus.Failed(test.Context, exception, DateTimeOffset.UtcNow).ConfigureAwait(false);
        }

        var executableTests = testList.Where(t => !circularDependencies.Any(cd => cd.Test == t)).ToList();
        if (executableTests.Count == 0)
        {
            await _logger.LogDebugAsync("No executable tests found after removing circular dependencies").ConfigureAwait(false);
            return;
        }

        // Group tests by their parallel constraints
        var groupedTests = await _groupingService.GroupTestsByConstraintsAsync(executableTests).ConfigureAwait(false);

        // Execute tests according to their grouping
        await ExecuteGroupedTestsAsync(groupedTests, runner, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteGroupedTestsAsync(
        GroupedTests groupedTests,
        TestRunner runner,
        CancellationToken cancellationToken)
    {
        // Execute all test groups with proper isolation to prevent race conditions between class-level hooks
        
        // 1. Execute parallel tests (no constraints, can run freely in parallel)
        if (groupedTests.Parallel.Length > 0)
        {
            await _logger.LogDebugAsync($"Starting {groupedTests.Parallel.Length} parallel tests").ConfigureAwait(false);
            
            var parallelTasks = groupedTests.Parallel.Select(test =>
            {
                var task = ExecuteTestWithParallelLimitAsync(test, runner, cancellationToken);
                test.ExecutionTask = task;
                return task;
            }).ToArray();

            await Task.WhenAll(parallelTasks).ConfigureAwait(false);
        }

        // 2. Execute parallel groups SEQUENTIALLY to prevent race conditions between class-level hooks
        // Each group completes entirely (including After(Class)) before the next group starts (including Before(Class))
        foreach (var (groupName, orderedTests) in groupedTests.ParallelGroups)
        {
            await _logger.LogDebugAsync($"Starting parallel group '{groupName}' with {orderedTests.Length} orders").ConfigureAwait(false);
            
            await ExecuteParallelGroupAsync(groupName, orderedTests, runner, cancellationToken).ConfigureAwait(false);
        }

        // 3. Execute keyed NotInParallel tests (each key runs sequentially, but keys can run in parallel with each other)
        if (groupedTests.KeyedNotInParallel.Length > 0)
        {
            var keyedTasks = groupedTests.KeyedNotInParallel.Select(async keyGroup =>
            {
                var (key, tests) = keyGroup;
                await _logger.LogDebugAsync($"Starting keyed NotInParallel group '{key}' with {tests.Length} tests").ConfigureAwait(false);
                await ExecuteSequentiallyAsync($"Key-{key}", tests, runner, cancellationToken).ConfigureAwait(false);
            }).ToArray();

            await Task.WhenAll(keyedTasks).ConfigureAwait(false);
        }

        // 4. Execute global NotInParallel tests (completely sequential, after everything else)
        if (groupedTests.NotInParallel.Length > 0)
        {
            await _logger.LogDebugAsync($"Starting {groupedTests.NotInParallel.Length} global NotInParallel tests").ConfigureAwait(false);
            
            // Execute each test individually in sequence to ensure no concurrency
            foreach (var test in groupedTests.NotInParallel)
            {
                await _logger.LogDebugAsync($"Executing global NotInParallel test: {test.TestId}").ConfigureAwait(false);
                
                var task = ExecuteTestWithParallelLimitAsync(test, runner, cancellationToken);
                test.ExecutionTask = task;
                await task.ConfigureAwait(false);
            }
        }
    }

    private async Task ExecuteTestWithParallelLimitAsync(
        AbstractExecutableTest test,
        TestRunner runner,
        CancellationToken cancellationToken)
    {
        // Check if test has parallel limit constraint
        if (test.Context.ParallelLimiter != null)
        {
            var semaphore = _parallelLimitLockProvider.GetLock(test.Context.ParallelLimiter);
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await runner.ExecuteTestAsync(test, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }
        else
        {
            await runner.ExecuteTestAsync(test, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ExecuteParallelGroupAsync(
        string groupName,
        (int Order, AbstractExecutableTest[] Tests)[] orderedTests,
        TestRunner runner,
        CancellationToken cancellationToken)
    {
        // Execute each order sequentially, but tests within each order can run in parallel
        foreach (var (order, tests) in orderedTests)
        {
            await _logger.LogDebugAsync($"Executing parallel group '{groupName}' order {order} with {tests.Length} tests").ConfigureAwait(false);
            
            var orderTasks = tests.Select(test =>
            {
                var task = ExecuteTestWithParallelLimitAsync(test, runner, cancellationToken);
                test.ExecutionTask = task;
                return task;
            }).ToArray();

            await Task.WhenAll(orderTasks).ConfigureAwait(false);
        }
    }

    private async Task ExecuteSequentiallyAsync(
        string groupName,
        AbstractExecutableTest[] tests,
        TestRunner runner,
        CancellationToken cancellationToken)
    {
        foreach (var test in tests)
        {
            await _logger.LogDebugAsync($"Executing sequential test in group '{groupName}': {test.TestId}").ConfigureAwait(false);
            
            var task = ExecuteTestWithParallelLimitAsync(test, runner, cancellationToken);
            test.ExecutionTask = task;
            await task.ConfigureAwait(false);
        }
    }
}