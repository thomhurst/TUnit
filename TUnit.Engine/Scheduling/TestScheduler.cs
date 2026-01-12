using System.Buffers;
using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Logging;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Interfaces;
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
    private readonly IDynamicTestQueue _dynamicTestQueue;
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
        StaticPropertyHandler staticPropertyHandler,
        IDynamicTestQueue dynamicTestQueue)
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
        _dynamicTestQueue = dynamicTestQueue;

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
            var simpleNames = new List<string>(dependencyChain.Count);
            foreach (var t in dependencyChain)
            {
                simpleNames.Add($"{t.Metadata.TestClassType.Name}.{t.Metadata.TestMethodName}");
            }

            var errorMessage = $"DependsOn Conflict: {string.Join(" > ", simpleNames)}";
            var exception = new CircularDependencyException(errorMessage);

            // Mark all tests in the dependency chain as failed
            foreach (var chainTest in dependencyChain)
            {
                if (testsInCircularDependencies.Add(chainTest))
                {
                    _testStateManager.MarkCircularDependencyFailed(chainTest, exception);
                    await _messageBus.Failed(chainTest.Context, exception, DateTimeOffset.UtcNow).ConfigureAwait(false);
                }
            }
        }

        var executableTests = new List<AbstractExecutableTest>(testList.Count);
        foreach (var test in testList)
        {
            if (!testsInCircularDependencies.Contains(test))
            {
                executableTests.Add(test);
            }
        }

        var executableTestsArray = executableTests.ToArray();
        if (executableTestsArray.Length == 0)
        {
            await _logger.LogDebugAsync("No executable tests found after removing circular dependencies").ConfigureAwait(false);
            return true;
        }

        // Initialize static properties before tests run
        await _staticPropertyHandler.InitializeStaticPropertiesAsync(cancellationToken).ConfigureAwait(false);

        // Track static properties for disposal at session end
        _staticPropertyHandler.TrackStaticProperties();

        // Group tests by their parallel constraints
        var groupedTests = await _groupingService.GroupTestsByConstraintsAsync(executableTestsArray).ConfigureAwait(false);

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
        // Start dynamic test queue processing in background
        var dynamicTestProcessingTask = ProcessDynamicTestQueueAsync(cancellationToken);

        if (groupedTests.Parallel.Length > 0)
        {
            await _logger.LogDebugAsync($"Starting {groupedTests.Parallel.Length} parallel tests").ConfigureAwait(false);
            await ExecuteTestsAsync(groupedTests.Parallel, cancellationToken).ConfigureAwait(false);
        }

        foreach (var group in groupedTests.ParallelGroups)
        {
            var orderedTests = new List<AbstractExecutableTest>();
            foreach (var kvp in group.Value.OrderBy(t => t.Key))
            {
                orderedTests.AddRange(kvp.Value);
            }
            var orderedTestsArray = orderedTests.ToArray();

            await _logger.LogDebugAsync($"Starting parallel group '{group.Key}' with {orderedTestsArray.Length} orders").ConfigureAwait(false);
            await ExecuteTestsAsync(orderedTestsArray, cancellationToken).ConfigureAwait(false);
        }

        foreach (var kvp in groupedTests.ConstrainedParallelGroups)
        {
            var constrainedTests = kvp.Value;
            await _logger.LogDebugAsync($"Starting constrained parallel group '{kvp.Key}' with {constrainedTests.UnconstrainedTests.Length} unconstrained and {constrainedTests.KeyedTests.Length} keyed tests").ConfigureAwait(false);

            var tasks = new List<Task>();
            if (constrainedTests.UnconstrainedTests.Length > 0)
            {
                tasks.Add(ExecuteTestsAsync(constrainedTests.UnconstrainedTests, cancellationToken));
            }
            if (constrainedTests.KeyedTests.Length > 0)
            {
                tasks.Add(_constraintKeyScheduler.ExecuteTestsWithConstraintsAsync(constrainedTests.KeyedTests, cancellationToken).AsTask());
            }
            if (tasks.Count > 0)
            {
                await WaitForTasksWithFailFastHandling(tasks.ToArray(), cancellationToken).ConfigureAwait(false);
            }
        }

        if (groupedTests.KeyedNotInParallel.Length > 0)
        {
            await _logger.LogDebugAsync($"Starting {groupedTests.KeyedNotInParallel.Length} keyed NotInParallel tests").ConfigureAwait(false);
            await _constraintKeyScheduler.ExecuteTestsWithConstraintsAsync(groupedTests.KeyedNotInParallel, cancellationToken).ConfigureAwait(false);
        }

        if (groupedTests.NotInParallel.Length > 0)
        {
            await _logger.LogDebugAsync($"Starting {groupedTests.NotInParallel.Length} global NotInParallel tests").ConfigureAwait(false);
            await ExecuteSequentiallyAsync(groupedTests.NotInParallel, cancellationToken).ConfigureAwait(false);
        }

        // Mark the queue as complete and wait for remaining dynamic tests to finish
        _dynamicTestQueue.Complete();
        await dynamicTestProcessingTask.ConfigureAwait(false);
    }

    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ProcessDynamicTestQueueAsync(CancellationToken cancellationToken)
    {
        var dynamicTests = new List<AbstractExecutableTest>();

        // Use async signaling instead of polling to eliminate IOCP overhead
        while (await _dynamicTestQueue.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            // Dequeue all currently available tests (batch processing)
            while (_dynamicTestQueue.TryDequeue(out var test))
            {
                if (test != null)
                {
                    dynamicTests.Add(test);
                }
            }

            // Execute the batch of dynamic tests if any were found
            if (dynamicTests.Count > 0)
            {
                await _logger.LogDebugAsync($"Executing {dynamicTests.Count} dynamic test(s)").ConfigureAwait(false);

                // Group and execute just like regular tests
                var dynamicTestsArray = dynamicTests.ToArray();
                var groupedDynamicTests = await _groupingService.GroupTestsByConstraintsAsync(dynamicTestsArray).ConfigureAwait(false);

                // Execute the grouped dynamic tests (recursive call handles sub-dynamics)
                if (groupedDynamicTests.Parallel.Length > 0)
                {
                    await ExecuteTestsAsync(groupedDynamicTests.Parallel, cancellationToken).ConfigureAwait(false);
                }

                if (groupedDynamicTests.NotInParallel.Length > 0)
                {
                    await ExecuteSequentiallyAsync(groupedDynamicTests.NotInParallel, cancellationToken).ConfigureAwait(false);
                }

                dynamicTests.Clear();
            }
        }

        // Process any remaining tests after queue completion
        while (_dynamicTestQueue.TryDequeue(out var test))
        {
            if (test != null)
            {
                dynamicTests.Add(test);
            }
        }

        if (dynamicTests.Count > 0)
        {
            await _logger.LogDebugAsync($"Executing {dynamicTests.Count} remaining dynamic test(s)").ConfigureAwait(false);

            var dynamicTestsArray = dynamicTests.ToArray();
            var groupedDynamicTests = await _groupingService.GroupTestsByConstraintsAsync(dynamicTestsArray).ConfigureAwait(false);

            if (groupedDynamicTests.Parallel.Length > 0)
            {
                await ExecuteTestsAsync(groupedDynamicTests.Parallel, cancellationToken).ConfigureAwait(false);
            }

            if (groupedDynamicTests.NotInParallel.Length > 0)
            {
                await ExecuteSequentiallyAsync(groupedDynamicTests.NotInParallel, cancellationToken).ConfigureAwait(false);
            }
        }
    }

#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteTestsAsync(
        AbstractExecutableTest[] tests,
        CancellationToken cancellationToken)
    {
        if (_maxParallelismSemaphore != null)
        {
            await ExecuteWithGlobalLimitAsync(tests, cancellationToken).ConfigureAwait(false);
        }
        else
        {
#if NET6_0_OR_GREATER
            // Use Parallel.ForEachAsync for bounded concurrency (eliminates unbounded Task.Run queue depth)
            // This dramatically reduces ThreadPool contention and GetQueuedCompletionStatus waits
            await Parallel.ForEachAsync(
                tests,
                new ParallelOptions { CancellationToken = cancellationToken },
                async (test, ct) =>
                {
                    test.ExecutionTask ??= ExecuteSingleTestAsync(test, ct);
                    await test.ExecutionTask.ConfigureAwait(false);
                }
            ).ConfigureAwait(false);
#else
            // Fallback for netstandard2.0: use Task.WhenAll (still better than unbounded Task.Run)
            var tasks = new Task[tests.Length];
            for (var i = 0; i < tests.Length; i++)
            {
                var test = tests[i];
                tasks[i] = test.ExecutionTask ??= ExecuteSingleTestAsync(test, cancellationToken);
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
#endif
        }
    }

    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteSingleTestAsync(
        AbstractExecutableTest test,
        CancellationToken cancellationToken)
    {
        if (test.Context.ParallelLimiter != null)
        {
            var semaphore = _parallelLimitLockProvider.GetLock(test.Context.ParallelLimiter);
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _testRunner.ExecuteTestAsync(test, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }
        else
        {
            await _testRunner.ExecuteTestAsync(test, cancellationToken).ConfigureAwait(false);
        }
    }

#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteSequentiallyAsync(
        AbstractExecutableTest[] tests,
        CancellationToken cancellationToken)
    {
        foreach (var test in tests)
        {
            test.ExecutionTask ??= ExecuteSingleTestAsync(test, cancellationToken);
            await test.ExecutionTask.ConfigureAwait(false);
        }
    }

    private async Task ExecuteWithGlobalLimitAsync(
        AbstractExecutableTest[] tests,
        CancellationToken cancellationToken)
    {
#if NET6_0_OR_GREATER
        // PERFORMANCE OPTIMIZATION: Partition tests by whether they have parallel limiters
        // Tests without limiters can run with unlimited parallelism (avoiding global semaphore overhead)
        var testsWithLimiters = new List<AbstractExecutableTest>();
        var testsWithoutLimiters = new List<AbstractExecutableTest>();

        foreach (var test in tests)
        {
            if (test.Context.ParallelLimiter != null)
            {
                testsWithLimiters.Add(test);
            }
            else
            {
                testsWithoutLimiters.Add(test);
            }
        }

        // Execute both groups concurrently
        var limitedTask = testsWithLimiters.Count > 0
            ? ExecuteWithLimitAsync(testsWithLimiters, cancellationToken)
            : Task.CompletedTask;

        var unlimitedTask = testsWithoutLimiters.Count > 0
            ? ExecuteUnlimitedAsync(testsWithoutLimiters, cancellationToken)
            : Task.CompletedTask;

        await Task.WhenAll(limitedTask, unlimitedTask).ConfigureAwait(false);
#else
        // Fallback for netstandard2.0: Manual bounded concurrency using existing semaphore
        var tasks = new Task[tests.Length];
        for (var i = 0; i < tests.Length; i++)
        {
            var test = tests[i];
            tasks[i] = Task.Run(async () =>
            {
                SemaphoreSlim? parallelLimiterSemaphore = null;

                await _maxParallelismSemaphore!.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (test.Context.ParallelLimiter != null)
                    {
                        parallelLimiterSemaphore = _parallelLimitLockProvider.GetLock(test.Context.ParallelLimiter);
                        await parallelLimiterSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }

                    try
                    {
                        test.ExecutionTask ??= _testRunner.ExecuteTestAsync(test, cancellationToken).AsTask();
                        await test.ExecutionTask.ConfigureAwait(false);
                    }
                    finally
                    {
                        parallelLimiterSemaphore?.Release();
                    }
                }
                finally
                {
                    _maxParallelismSemaphore.Release();
                }
            }, CancellationToken.None);
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
#endif
    }

#if NET6_0_OR_GREATER
    private async Task ExecuteWithLimitAsync(
        List<AbstractExecutableTest> tests,
        CancellationToken cancellationToken)
    {
        // Execute tests with parallel limiters using the global limit
        await Parallel.ForEachAsync(
            tests,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _maxParallelism,
                CancellationToken = cancellationToken
            },
            async (test, ct) =>
            {
                var parallelLimiterSemaphore = _parallelLimitLockProvider.GetLock(test.Context.ParallelLimiter!);
                await parallelLimiterSemaphore.WaitAsync(ct).ConfigureAwait(false);

                try
                {
                    test.ExecutionTask ??= _testRunner.ExecuteTestAsync(test, ct).AsTask();
                    await test.ExecutionTask.ConfigureAwait(false);
                }
                finally
                {
                    parallelLimiterSemaphore.Release();
                }
            }
        ).ConfigureAwait(false);
    }

    private async Task ExecuteUnlimitedAsync(
        List<AbstractExecutableTest> tests,
        CancellationToken cancellationToken)
    {
        // Execute tests without per-test limiters, but still apply global parallelism limit
        await Parallel.ForEachAsync(
            tests,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _maxParallelism,
                CancellationToken = cancellationToken
            },
            async (test, ct) =>
            {
                test.ExecutionTask ??= _testRunner.ExecuteTestAsync(test, ct).AsTask();
                await test.ExecutionTask.ConfigureAwait(false);
            }
        ).ConfigureAwait(false);
    }
#endif

    private async Task WaitForTasksWithFailFastHandling(IEnumerable<Task> tasks, CancellationToken cancellationToken)
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
        // Check command line argument first (highest priority)
        if (commandLineOptions.TryGetOptionArgumentList(
                MaximumParallelTestsCommandProvider.MaximumParallelTests,
                out var args) && args.Length > 0 && int.TryParse(args[0], out var maxParallelTests))
        {
            if (maxParallelTests == 0)
            {
                // 0 means unlimited (backwards compat for advanced users)
                logger.LogDebug("Maximum parallel tests: unlimited (from command line)");
                return int.MaxValue;
            }

            if (maxParallelTests > 0)
            {
                logger.LogDebug($"Maximum parallel tests limit set to {maxParallelTests} (from command line)");
                return maxParallelTests;
            }
        }

        // Check environment variable (second priority)
        if (Environment.GetEnvironmentVariable("TUNIT_MAX_PARALLEL_TESTS") is string envVar
            && int.TryParse(envVar, out var envLimit))
        {
            if (envLimit == 0)
            {
                logger.LogDebug("Maximum parallel tests: unlimited (from TUNIT_MAX_PARALLEL_TESTS environment variable)");
                return int.MaxValue;
            }

            if (envLimit > 0)
            {
                logger.LogDebug($"Maximum parallel tests limit set to {envLimit} (from TUNIT_MAX_PARALLEL_TESTS environment variable)");
                return envLimit;
            }
        }

        // Default: 8x CPU cores (empirically optimized for async/IO-bound workloads)
        // Users can override via --maximum-parallel-tests or TUNIT_MAX_PARALLEL_TESTS
        var defaultLimit = Environment.ProcessorCount * 8;
        logger.LogDebug($"Maximum parallel tests limit defaulting to {defaultLimit} ({Environment.ProcessorCount} processors * 8)");
        return defaultLimit;
    }
}
