using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Logging;
using TUnit.Engine.CommandLineProviders;
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
    private readonly TestRunner _testRunner;
    private readonly CircularDependencyDetector _circularDependencyDetector;
    private readonly IConstraintKeyScheduler _constraintKeyScheduler;
    private readonly HookExecutor _hookExecutor;
    private readonly StaticPropertyHandler _staticPropertyHandler;

    public TestScheduler(
        TUnitFrameworkLogger logger,
        ITestGroupingService groupingService,
        ITUnitMessageBus messageBus,
        ICommandLineOptions commandLineOptions,
        ParallelLimitLockProvider parallelLimitLockProvider,
        TestStateManager testStateManager,
        TestRunner testRunner,
        CircularDependencyDetector circularDependencyDetector,
        IConstraintKeyScheduler constraintKeyScheduler,
        HookExecutor hookExecutor,
        StaticPropertyHandler staticPropertyHandler)
    {
        _logger = logger;
        _groupingService = groupingService;
        _messageBus = messageBus;
        _commandLineOptions = commandLineOptions;
        _parallelLimitLockProvider = parallelLimitLockProvider;
        _testStateManager = testStateManager;
        _testRunner = testRunner;
        _circularDependencyDetector = circularDependencyDetector;
        _constraintKeyScheduler = constraintKeyScheduler;
        _hookExecutor = hookExecutor;
        _staticPropertyHandler = staticPropertyHandler;
    }

    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    public async Task<bool> ScheduleAndExecuteAsync(
        List<AbstractExecutableTest> testList,
        CancellationToken cancellationToken)
    {
        if (testList == null)
        {
            throw new ArgumentNullException(nameof(testList));
        }

        if (testList.Count == 0)
        {
            await _logger.LogDebugAsync("No executable tests found").ConfigureAwait(false);
            return true;
        }

        await _logger.LogDebugAsync($"Scheduling execution of {testList.Count} tests").ConfigureAwait(false);

        var circularDependencies = _circularDependencyDetector.DetectCircularDependencies(testList);

        var testsInCircularDependencies = new HashSet<AbstractExecutableTest>();

        foreach (var (test, dependencyChain) in circularDependencies)
        {
            // Format the error message to match the expected format
            var simpleNames = dependencyChain.Select(t =>
            {
                var className = t.Metadata.TestClassType.Name;
                var testName = t.Metadata.TestMethodName;
                return $"{className}.{testName}";
            }).ToList();

            var errorMessage = $"DependsOn Conflict: {string.Join(" > ", simpleNames)}";
            var exception = new CircularDependencyException(errorMessage);

            // Mark all tests in the dependency chain as failed
            foreach (var chainTest in dependencyChain)
            {
                if (testsInCircularDependencies.Add(chainTest))
                {
                    await _testStateManager.MarkCircularDependencyFailedAsync(chainTest, exception).ConfigureAwait(false);
                    await _messageBus.Failed(chainTest.Context, exception, DateTimeOffset.UtcNow).ConfigureAwait(false);
                }
            }
        }

        var executableTests = testList.Where(t => !testsInCircularDependencies.Contains(t)).ToArray();
        if (executableTests.Length == 0)
        {
            await _logger.LogDebugAsync("No executable tests found after removing circular dependencies").ConfigureAwait(false);
            return true;
        }

        // Initialize static properties before tests run
        await _staticPropertyHandler.InitializeStaticPropertiesAsync(cancellationToken).ConfigureAwait(false);

        // Track static properties for disposal at session end
        _staticPropertyHandler.TrackStaticProperties();

        // Group tests by their parallel constraints
        var groupedTests = await _groupingService.GroupTestsByConstraintsAsync(executableTests).ConfigureAwait(false);

        // Execute tests according to their grouping
        await ExecuteGroupedTestsAsync(groupedTests, cancellationToken).ConfigureAwait(false);

        var sessionHookExceptions = await _hookExecutor.ExecuteAfterTestSessionHooksAsync(cancellationToken).ConfigureAwait(false) ?? [];

        await _staticPropertyHandler.DisposeStaticPropertiesAsync(sessionHookExceptions).ConfigureAwait(false);

        if (sessionHookExceptions.Count > 0)
        {
            foreach (var ex in sessionHookExceptions)
            {
                await _logger.LogErrorAsync($"Error executing After(TestSession) hook: {ex}").ConfigureAwait(false);
            }
            return false;
        }

        return true;
    }

    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteGroupedTestsAsync(
        GroupedTests groupedTests,
        CancellationToken cancellationToken)
    {
        // Check if maximum parallel tests limit is specified
        int? maxParallelism = null;
        if (_commandLineOptions.TryGetOptionArgumentList(
                MaximumParallelTestsCommandProvider.MaximumParallelTests,
                out var args) && args.Length > 0)
        {
            if (int.TryParse(args[0], out var maxParallelTests) && maxParallelTests > 0)
            {
                maxParallelism = maxParallelTests;
                await _logger.LogDebugAsync($"Maximum parallel tests limit set to {maxParallelTests}").ConfigureAwait(false);
            }
        }
        // Execute all test groups with proper isolation to prevent race conditions between class-level hooks

        // 1. Execute parallel tests (no constraints, can run freely in parallel)
        if (groupedTests.Parallel.Length > 0)
        {
            await _logger.LogDebugAsync($"Starting {groupedTests.Parallel.Length} parallel tests").ConfigureAwait(false);

            if (maxParallelism is > 0)
            {
                // Use worker pool pattern to respect maximum parallel tests limit
                await ExecuteParallelTestsWithLimitAsync(groupedTests.Parallel, maxParallelism.Value, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // No limit - start all tests at once
                // Use Task.Run to ensure tasks start immediately without blocking on logging in ExecuteTestWithParallelLimitAsync
                var parallelTasks = groupedTests.Parallel.Select(test =>
                {
                    return test.ExecutionTask = Task.Run(async () => await ExecuteTestWithParallelLimitAsync(test, cancellationToken), CancellationToken.None);
                }).ToArray();

                await WaitForTasksWithFailFastHandling(parallelTasks, cancellationToken).ConfigureAwait(false);
            }
        }

        // 2. Execute parallel groups SEQUENTIALLY to prevent race conditions between class-level hooks
        // Each group completes entirely (including After(Class)) before the next group starts (including Before(Class))
        foreach (var group in groupedTests.ParallelGroups)
        {
            var groupName = group.Key;
            var orderedTests = group.Value
                .OrderBy(t => t.Key)
                .SelectMany(x => x.Value)
                .ToArray();

            await _logger.LogDebugAsync($"Starting parallel group '{groupName}' with {orderedTests.Length} orders").ConfigureAwait(false);

            await ExecuteParallelGroupAsync(groupName, orderedTests, maxParallelism, cancellationToken).ConfigureAwait(false);
        }

        // 2b. Execute constrained parallel groups (groups with both ParallelGroup and NotInParallel)
        foreach (var kvp in groupedTests.ConstrainedParallelGroups)
        {
            var groupName = kvp.Key;
            var constrainedTests = kvp.Value;

            await _logger.LogDebugAsync($"Starting constrained parallel group '{groupName}' with {constrainedTests.UnconstrainedTests.Length} unconstrained and {constrainedTests.KeyedTests.Length} keyed tests").ConfigureAwait(false);

            await ExecuteConstrainedParallelGroupAsync(groupName, constrainedTests, maxParallelism, cancellationToken).ConfigureAwait(false);
        }

        // 3. Execute keyed NotInParallel tests using ConstraintKeyScheduler for proper coordination
        if (groupedTests.KeyedNotInParallel.Length > 0)
        {
            await _logger.LogDebugAsync($"Starting {groupedTests.KeyedNotInParallel.Length} keyed NotInParallel tests").ConfigureAwait(false);
            await _constraintKeyScheduler.ExecuteTestsWithConstraintsAsync(groupedTests.KeyedNotInParallel, cancellationToken).ConfigureAwait(false);
        }

        // 4. Execute global NotInParallel tests (completely sequential, after everything else)
        if (groupedTests.NotInParallel.Length > 0)
        {
            await _logger.LogDebugAsync($"Starting {groupedTests.NotInParallel.Length} global NotInParallel tests").ConfigureAwait(false);

            await ExecuteSequentiallyAsync("Global", groupedTests.NotInParallel, cancellationToken).ConfigureAwait(false);
        }
    }

    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteTestWithParallelLimitAsync(
        AbstractExecutableTest test,
        CancellationToken cancellationToken)
    {
        var taskStartTime = DateTime.UtcNow;
        await _logger.LogDebugAsync($"[TASK START] Test '{test.TestId}' task started at {taskStartTime:HH:mm:ss.fff}").ConfigureAwait(false);

        // Check if test has parallel limit constraint
        if (test.Context.ParallelLimiter != null)
        {
            var limiterType = test.Context.ParallelLimiter.GetType().Name;
            var semaphore = _parallelLimitLockProvider.GetLock(test.Context.ParallelLimiter);

            var waitStartTime = DateTime.UtcNow;
            var availableBeforeWait = semaphore.CurrentCount;
            await _logger.LogDebugAsync($"[SEMAPHORE WAIT START] Test '{test.TestId}' waiting for ParallelLimiter '{limiterType}' at {waitStartTime:HH:mm:ss.fff} (available: {availableBeforeWait}/{test.Context.ParallelLimiter.Limit})").ConfigureAwait(false);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            var acquiredTime = DateTime.UtcNow;
            var waitDuration = (acquiredTime - waitStartTime).TotalMilliseconds;
            var remainingAfterAcquire = semaphore.CurrentCount;
            await _logger.LogDebugAsync($"[SEMAPHORE ACQUIRED] Test '{test.TestId}' acquired ParallelLimiter '{limiterType}' at {acquiredTime:HH:mm:ss.fff} after {waitDuration:F0}ms wait (remaining: {remainingAfterAcquire}/{test.Context.ParallelLimiter.Limit})").ConfigureAwait(false);

            try
            {
                var execStartTime = DateTime.UtcNow;
                await _logger.LogDebugAsync($"[TEST EXECUTION START] Test '{test.TestId}' starting execution at {execStartTime:HH:mm:ss.fff}").ConfigureAwait(false);

                await _testRunner.ExecuteTestAsync(test, cancellationToken).ConfigureAwait(false);

                var execEndTime = DateTime.UtcNow;
                var execDuration = (execEndTime - execStartTime).TotalMilliseconds;
                await _logger.LogDebugAsync($"[TEST EXECUTION END] Test '{test.TestId}' completed execution at {execEndTime:HH:mm:ss.fff} (duration: {execDuration:F0}ms)").ConfigureAwait(false);
            }
            finally
            {
                var availableBeforeRelease = semaphore.CurrentCount;
                semaphore.Release();
                var availableAfterRelease = semaphore.CurrentCount;
                var releaseTime = DateTime.UtcNow;
                var totalDuration = (releaseTime - taskStartTime).TotalMilliseconds;
                await _logger.LogDebugAsync($"[SEMAPHORE RELEASED] Test '{test.TestId}' released ParallelLimiter '{limiterType}' at {releaseTime:HH:mm:ss.fff} (total task duration: {totalDuration:F0}ms, available: {availableBeforeRelease}/{test.Context.ParallelLimiter.Limit} → {availableAfterRelease}/{test.Context.ParallelLimiter.Limit})").ConfigureAwait(false);
            }
        }
        else
        {
            await _logger.LogDebugAsync($"Test '{test.TestId}': No ParallelLimiter, executing directly").ConfigureAwait(false);
            await _testRunner.ExecuteTestAsync(test, cancellationToken).ConfigureAwait(false);
        }
    }

    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteParallelGroupAsync(
        string groupName,
        AbstractExecutableTest[] orderedTests,
        int? maxParallelism,
        CancellationToken cancellationToken)
    {
        await _logger.LogDebugAsync($"Executing parallel group '{groupName}' with {orderedTests.Length} tests").ConfigureAwait(false);

        if (maxParallelism is > 0)
        {
            // Use worker pool pattern to respect maximum parallel tests limit
            await ExecuteParallelTestsWithLimitAsync(orderedTests, maxParallelism.Value, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // No limit - start all tests at once
            var orderTasks = orderedTests.Select(test =>
            {
                var task = ExecuteTestWithParallelLimitAsync(test, cancellationToken);
                test.ExecutionTask = task;
                return task;
            }).ToArray();

            await WaitForTasksWithFailFastHandling(orderTasks, cancellationToken).ConfigureAwait(false);
        }
    }

    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteConstrainedParallelGroupAsync(
        string groupName,
        GroupedConstrainedTests constrainedTests,
        int? maxParallelism,
        CancellationToken cancellationToken)
    {
        await _logger.LogDebugAsync($"Executing constrained parallel group '{groupName}'").ConfigureAwait(false);

        // Start unconstrained tests (can run in parallel)
        var unconstrainedTasks = new List<Task>();
        if (constrainedTests.UnconstrainedTests.Length > 0)
        {
            if (maxParallelism is > 0)
            {
                // Respect maximum parallel tests limit for unconstrained tests
                var unconstrainedTask = ExecuteParallelTestsWithLimitAsync(
                    constrainedTests.UnconstrainedTests,
                    maxParallelism.Value,
                    cancellationToken);
                unconstrainedTasks.Add(unconstrainedTask);
            }
            else
            {
                // No limit - start all unconstrained tests at once
                foreach (var test in constrainedTests.UnconstrainedTests)
                {
                    var task = ExecuteTestWithParallelLimitAsync(test, cancellationToken);
                    test.ExecutionTask = task;
                    unconstrainedTasks.Add(task);
                }
            }
        }

        // Execute keyed tests using the constraint key scheduler
        Task? keyedTask = null;
        if (constrainedTests.KeyedTests.Length > 0)
        {
            keyedTask = _constraintKeyScheduler.ExecuteTestsWithConstraintsAsync(
                constrainedTests.KeyedTests,
                cancellationToken).AsTask();
        }

        // Wait for both unconstrained and keyed tests to complete
        var allTasks = unconstrainedTasks.ToList();
        if (keyedTask != null)
        {
            allTasks.Add(keyedTask);
        }

        if (allTasks.Count > 0)
        {
            await WaitForTasksWithFailFastHandling(allTasks.ToArray(), cancellationToken).ConfigureAwait(false);
        }
    }

    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteSequentiallyAsync(
        string groupName,
        AbstractExecutableTest[] tests,
        CancellationToken cancellationToken)
    {
        foreach (var test in tests)
        {
            await _logger.LogDebugAsync($"Executing sequential test in group '{groupName}': {test.TestId}").ConfigureAwait(false);

            var task = ExecuteTestWithParallelLimitAsync(test, cancellationToken);
            test.ExecutionTask = task;
            await task.ConfigureAwait(false);
        }
    }

    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteParallelTestsWithLimitAsync(
        AbstractExecutableTest[] tests,
        int maxParallelism,
        CancellationToken cancellationToken)
    {
        await _logger.LogDebugAsync($"Starting {tests.Length} tests with global max parallelism: {maxParallelism}").ConfigureAwait(false);

        // Global semaphore limits total concurrent test execution
        var globalSemaphore = new SemaphoreSlim(maxParallelism, maxParallelism);

        // Start all tests concurrently using two-phase acquisition pattern:
        // Phase 1: Acquire ParallelLimiter (if test has one) - wait for constrained resource
        // Phase 2: Acquire global semaphore - claim execution slot
        //
        // This ordering prevents resource underutilization: tests wait for constrained
        // resources BEFORE claiming global slots, so global slots are only held during
        // actual test execution, not during waiting for constrained resources.
        //
        // This is deadlock-free because:
        // - All tests acquire ParallelLimiter BEFORE global semaphore
        // - No test ever holds global while waiting for ParallelLimiter
        // - Therefore, no circular wait can occur
        var tasks = tests.Select(async test =>
        {
            SemaphoreSlim? parallelLimiterSemaphore = null;

            // Phase 1: Acquire ParallelLimiter first (if test has one)
            if (test.Context.ParallelLimiter != null)
            {
                var limiterName = test.Context.ParallelLimiter.GetType().Name;
                await _logger.LogDebugAsync($"Test '{test.TestId}': [Phase 1] Acquiring ParallelLimiter '{limiterName}' (limit: {test.Context.ParallelLimiter.Limit})").ConfigureAwait(false);

                parallelLimiterSemaphore = _parallelLimitLockProvider.GetLock(test.Context.ParallelLimiter);
                await parallelLimiterSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                await _logger.LogDebugAsync($"Test '{test.TestId}': [Phase 1] Acquired ParallelLimiter '{limiterName}'").ConfigureAwait(false);
            }

            try
            {
                // Phase 2: Acquire global semaphore
                // At this point, we have the constrained resource (if needed),
                // so we can immediately use the global slot for execution
                await _logger.LogDebugAsync($"Test '{test.TestId}': [Phase 2] Acquiring global semaphore (available: {globalSemaphore.CurrentCount}/{maxParallelism})").ConfigureAwait(false);

                await globalSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                var slotsUsed = maxParallelism - globalSemaphore.CurrentCount;
                await _logger.LogDebugAsync($"Test '{test.TestId}': [Phase 2] Acquired global semaphore - executing (global slots used: {slotsUsed}/{maxParallelism})").ConfigureAwait(false);

                try
                {
                    // Execute the test
                    var task = _testRunner.ExecuteTestAsync(test, cancellationToken);
                    test.ExecutionTask = task;
                    await task.ConfigureAwait(false);
                }
                finally
                {
                    // Always release global semaphore after execution
                    globalSemaphore.Release();
                    await _logger.LogDebugAsync($"Test '{test.TestId}': [Phase 2] Released global semaphore (available: {globalSemaphore.CurrentCount}/{maxParallelism})").ConfigureAwait(false);
                }
            }
            finally
            {
                // Always release ParallelLimiter semaphore (if we acquired one)
                if (parallelLimiterSemaphore != null)
                {
                    parallelLimiterSemaphore.Release();
                    await _logger.LogDebugAsync($"Test '{test.TestId}': [Phase 1] Released ParallelLimiter").ConfigureAwait(false);
                }
            }
        }).ToArray();

        // Wait for all tests to complete, handling fail-fast correctly
        await WaitForTasksWithFailFastHandling(tasks, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for multiple tasks to complete, handling fail-fast cancellation properly.
    /// When fail-fast is triggered, we only want to bubble up the first real failure,
    /// not the cancellation exceptions from other tests that were cancelled as a result.
    /// </summary>
    private async Task WaitForTasksWithFailFastHandling(Task[] tasks, CancellationToken cancellationToken)
    {
        try
        {
            // Wait for all tasks to complete, even if some fail
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Check if this is a fail-fast scenario
            if (cancellationToken.IsCancellationRequested)
            {
                // Get the first failure that triggered fail-fast
                var firstFailure = _testRunner.GetFirstFailFastException();

                // If we have a stored first failure, throw that instead of the aggregated exceptions
                if (firstFailure != null)
                {
                    throw firstFailure;
                }

                // If no stored failure, this was a user-initiated cancellation
                // Let the original exception bubble up
            }

            // Re-throw the original exception (either cancellation or non-fail-fast failure)
            throw;
        }
    }
}
