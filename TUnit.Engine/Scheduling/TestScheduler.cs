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
    private readonly ParallelLimitLockProvider _parallelLimitLockProvider;
    private readonly TestStateManager _testStateManager;
    private readonly TestRunner _testRunner;
    private readonly CircularDependencyDetector _circularDependencyDetector;
    private readonly IConstraintKeyScheduler _constraintKeyScheduler;
    private readonly HookExecutor _hookExecutor;
    private readonly StaticPropertyHandler _staticPropertyHandler;
    private readonly int _maxParallelism;
    private readonly SemaphoreSlim? _maxParallelismSemaphore;

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
        _parallelLimitLockProvider = parallelLimitLockProvider;
        _testStateManager = testStateManager;
        _testRunner = testRunner;
        _circularDependencyDetector = circularDependencyDetector;
        _constraintKeyScheduler = constraintKeyScheduler;
        _hookExecutor = hookExecutor;
        _staticPropertyHandler = staticPropertyHandler;

        _maxParallelism = GetMaxParallelism(logger, commandLineOptions);

        _maxParallelismSemaphore = _maxParallelism == int.MaxValue
            ? null
            : new SemaphoreSlim(_maxParallelism, _maxParallelism);
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
        if (groupedTests.Parallel.Length > 0)
        {
            await _logger.LogDebugAsync($"Starting {groupedTests.Parallel.Length} parallel tests").ConfigureAwait(false);

            if (_maxParallelism > 0)
            {
                await ExecuteParallelTestsWithLimitAsync(groupedTests.Parallel, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var parallelTasks = groupedTests.Parallel.Select(test =>
                {
                    return test.ExecutionTask ??= Task.Run(() => ExecuteTestWithParallelLimitAsync(test, cancellationToken), CancellationToken.None);
                }).ToArray();

                await WaitForTasksWithFailFastHandling(parallelTasks, cancellationToken).ConfigureAwait(false);
            }
        }

        foreach (var group in groupedTests.ParallelGroups)
        {
            var groupName = group.Key;
            var orderedTests = group.Value
                .OrderBy(t => t.Key)
                .SelectMany(x => x.Value)
                .ToArray();

            await _logger.LogDebugAsync($"Starting parallel group '{groupName}' with {orderedTests.Length} orders").ConfigureAwait(false);

            await ExecuteParallelGroupAsync(groupName, orderedTests, cancellationToken).ConfigureAwait(false);
        }

        foreach (var kvp in groupedTests.ConstrainedParallelGroups)
        {
            var groupName = kvp.Key;
            var constrainedTests = kvp.Value;

            await _logger.LogDebugAsync($"Starting constrained parallel group '{groupName}' with {constrainedTests.UnconstrainedTests.Length} unconstrained and {constrainedTests.KeyedTests.Length} keyed tests").ConfigureAwait(false);

            await ExecuteConstrainedParallelGroupAsync(groupName, constrainedTests, cancellationToken).ConfigureAwait(false);
        }

        if (groupedTests.KeyedNotInParallel.Length > 0)
        {
            await _logger.LogDebugAsync($"Starting {groupedTests.KeyedNotInParallel.Length} keyed NotInParallel tests").ConfigureAwait(false);
            await _constraintKeyScheduler.ExecuteTestsWithConstraintsAsync(groupedTests.KeyedNotInParallel, cancellationToken).ConfigureAwait(false);
        }

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
        CancellationToken cancellationToken)
    {
        await _logger.LogDebugAsync($"Executing parallel group '{groupName}' with {orderedTests.Length} tests").ConfigureAwait(false);

        if (_maxParallelism is > 0)
        {
            await ExecuteParallelTestsWithLimitAsync(orderedTests, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var orderTasks = orderedTests.Select(test =>
            {
                return test.ExecutionTask ??= Task.Run(() => ExecuteTestWithParallelLimitAsync(test, cancellationToken), CancellationToken.None);
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
        CancellationToken cancellationToken)
    {
        await _logger.LogDebugAsync($"Executing constrained parallel group '{groupName}'").ConfigureAwait(false);

        var unconstrainedTasks = new List<Task>();
        if (constrainedTests.UnconstrainedTests.Length > 0)
        {
            if (_maxParallelism is > 0)
            {
                var unconstrainedTask = ExecuteParallelTestsWithLimitAsync(
                    constrainedTests.UnconstrainedTests,
                    cancellationToken);
                unconstrainedTasks.Add(unconstrainedTask);
            }
            else
            {
                foreach (var test in constrainedTests.UnconstrainedTests)
                {
                    test.ExecutionTask ??= Task.Run(() => ExecuteTestWithParallelLimitAsync(test, cancellationToken), CancellationToken.None);
                    unconstrainedTasks.Add(test.ExecutionTask);
                }
            }
        }

        Task? keyedTask = null;
        if (constrainedTests.KeyedTests.Length > 0)
        {
            keyedTask = _constraintKeyScheduler.ExecuteTestsWithConstraintsAsync(
                constrainedTests.KeyedTests,
                cancellationToken).AsTask();
        }

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

            test.ExecutionTask ??= ExecuteTestWithParallelLimitAsync(test, cancellationToken);
            await test.ExecutionTask.ConfigureAwait(false);
        }
    }

#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteParallelTestsWithLimitAsync(
        AbstractExecutableTest[] tests,
        CancellationToken cancellationToken)
    {
        await _logger.LogDebugAsync($"Starting {tests.Length} tests with global max parallelism: {_maxParallelism}").ConfigureAwait(false);

        var tasks = tests.Select(async test =>
        {
            SemaphoreSlim? parallelLimiterSemaphore = null;

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
                await _logger.LogDebugAsync($"Test '{test.TestId}': [Phase 2] Acquiring global semaphore (available: {_maxParallelismSemaphore?.CurrentCount}/{_maxParallelism})").ConfigureAwait(false);

                if (_maxParallelismSemaphore != null)
                {
                    await _maxParallelismSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                }

                var slotsUsed = _maxParallelism - _maxParallelismSemaphore?.CurrentCount;
                await _logger.LogDebugAsync($"Test '{test.TestId}': [Phase 2] Acquired global semaphore - executing (global slots used: {slotsUsed}/{_maxParallelism})").ConfigureAwait(false);

                try
                {
                    test.ExecutionTask ??= _testRunner.ExecuteTestAsync(test, cancellationToken);
                    await test.ExecutionTask.ConfigureAwait(false);
                }
                finally
                {
                    _maxParallelismSemaphore?.Release();
                    await _logger.LogDebugAsync($"Test '{test.TestId}': [Phase 2] Released global semaphore (available: {_maxParallelismSemaphore?.CurrentCount}/{_maxParallelism})").ConfigureAwait(false);
                }
            }
            finally
            {
                if (parallelLimiterSemaphore != null)
                {
                    parallelLimiterSemaphore.Release();
                    await _logger.LogDebugAsync($"Test '{test.TestId}': [Phase 1] Released ParallelLimiter").ConfigureAwait(false);
                }
            }
        }).ToArray();

        await WaitForTasksWithFailFastHandling(tasks, cancellationToken).ConfigureAwait(false);
    }

    private async Task WaitForTasksWithFailFastHandling(Task[] tasks, CancellationToken cancellationToken)
    {
        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                var firstFailure = _testRunner.GetFirstFailFastException();

                if (firstFailure != null)
                {
                    throw firstFailure;
                }
            }

            throw;
        }
    }

    private static int GetMaxParallelism(ILogger logger, ICommandLineOptions commandLineOptions)
    {
        if (!commandLineOptions.TryGetOptionArgumentList(
                MaximumParallelTestsCommandProvider.MaximumParallelTests,
                out var args) || args.Length <= 0 || !int.TryParse(args[0], out var maxParallelTests) || maxParallelTests <= 0)
        {
            return int.MaxValue;
        }

        logger.LogDebug($"Maximum parallel tests limit set to {maxParallelTests}");
        return maxParallelTests;
    }
}
