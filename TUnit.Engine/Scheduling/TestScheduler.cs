using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Logging;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Configuration;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;
using TUnit.Engine.Services;
using TUnit.Core.Settings;
using TUnit.Engine.Services.TestExecution;

namespace TUnit.Engine.Scheduling;

internal sealed class TestScheduler : ITestScheduler
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestGroupingService _groupingService;
    private readonly ITUnitMessageBus _messageBus;
    private readonly TestStateManager _testStateManager;
    private readonly TestRunner _testRunner;
    private readonly CircularDependencyDetector _circularDependencyDetector;
    private readonly IConstraintKeyScheduler _constraintKeyScheduler;
    private readonly HookExecutor _hookExecutor;
    private readonly AfterHookPairTracker _afterHookPairTracker;
    private readonly StaticPropertyHandler _staticPropertyHandler;
    private readonly IDynamicTestQueue _dynamicTestQueue;
    private readonly Lazy<int> _maxParallelism;
#if !NET8_0_OR_GREATER
    private readonly Lazy<SemaphoreSlim> _maxParallelismSemaphore;
#endif

    public TestScheduler(
        TUnitFrameworkLogger logger,
        ITestGroupingService groupingService,
        ITUnitMessageBus messageBus,
        ICommandLineOptions commandLineOptions,
        TestStateManager testStateManager,
        TestRunner testRunner,
        CircularDependencyDetector circularDependencyDetector,
        IConstraintKeyScheduler constraintKeyScheduler,
        HookExecutor hookExecutor,
        AfterHookPairTracker afterHookPairTracker,
        StaticPropertyHandler staticPropertyHandler,
        IDynamicTestQueue dynamicTestQueue)
    {
        _logger = logger;
        _groupingService = groupingService;
        _messageBus = messageBus;
        _testStateManager = testStateManager;
        _testRunner = testRunner;
        _circularDependencyDetector = circularDependencyDetector;
        _constraintKeyScheduler = constraintKeyScheduler;
        _hookExecutor = hookExecutor;
        _afterHookPairTracker = afterHookPairTracker;
        _staticPropertyHandler = staticPropertyHandler;
        _dynamicTestQueue = dynamicTestQueue;

        _maxParallelism = new Lazy<int>(() => GetMaxParallelism(logger, commandLineOptions));

#if !NET8_0_OR_GREATER
        // The .NET 8+ path uses Parallel.ForEachAsync which caps concurrency via
        // ParallelOptions.MaxDegreeOfParallelism — the semaphore is only needed
        // for the netstandard2.0 fallback path.
        _maxParallelismSemaphore = new Lazy<SemaphoreSlim>(() =>
            new SemaphoreSlim(_maxParallelism.Value, _maxParallelism.Value));
#endif
    }

    #if NET8_0_OR_GREATER
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

        if (_logger.IsDebugEnabled)
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
                    TestSessionContext.Current?.MarkFailure();
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

        if (executableTests.Count == 0)
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

        var sessionHookExceptions = await _afterHookPairTracker.GetOrCreateAfterTestSessionTask(
            () => _hookExecutor.ExecuteAfterTestSessionHooksAsync(cancellationToken)).ConfigureAwait(false) ?? [];

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

    #if NET8_0_OR_GREATER
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
            if (_logger.IsTraceEnabled)
                await _logger.LogTraceAsync($"Starting {groupedTests.Parallel.Length} parallel tests").ConfigureAwait(false);
            await ExecuteTestsAsync(groupedTests.Parallel, cancellationToken).ConfigureAwait(false);
        }

        foreach (var group in groupedTests.ParallelGroups)
        {
            var totalCount = 0;
            foreach (var list in group.Value.Values) totalCount += list.Count;
            var orderedTestsArray = new AbstractExecutableTest[totalCount];
            var idx = 0;
            foreach (var list in group.Value.Values)
            {
                foreach (var t in list)
                {
                    orderedTestsArray[idx++] = t;
                }
            }

            if (_logger.IsTraceEnabled)
                await _logger.LogTraceAsync($"Starting parallel group '{group.Key}' with {orderedTestsArray.Length} orders").ConfigureAwait(false);
            await ExecuteTestsAsync(orderedTestsArray, cancellationToken).ConfigureAwait(false);
        }

        foreach (var kvp in groupedTests.ConstrainedParallelGroups)
        {
            var constrainedTests = kvp.Value;
            if (_logger.IsTraceEnabled)
                await _logger.LogTraceAsync($"Starting constrained parallel group '{kvp.Key}' with {constrainedTests.UnconstrainedTests.Length} unconstrained and {constrainedTests.KeyedTests.Length} keyed tests").ConfigureAwait(false);

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
            if (_logger.IsTraceEnabled)
                await _logger.LogTraceAsync($"Starting {groupedTests.KeyedNotInParallel.Length} keyed NotInParallel tests").ConfigureAwait(false);
            await _constraintKeyScheduler.ExecuteTestsWithConstraintsAsync(groupedTests.KeyedNotInParallel, cancellationToken).ConfigureAwait(false);
        }

        if (groupedTests.NotInParallel.Length > 0)
        {
            if (_logger.IsTraceEnabled)
                await _logger.LogTraceAsync($"Starting {groupedTests.NotInParallel.Length} global NotInParallel tests").ConfigureAwait(false);
            await ExecuteSequentiallyAsync(groupedTests.NotInParallel, cancellationToken).ConfigureAwait(false);
        }

        // Mark the queue as complete and wait for remaining dynamic tests to finish
        _dynamicTestQueue.Complete();
        await dynamicTestProcessingTask.ConfigureAwait(false);
    }

    #if NET8_0_OR_GREATER
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
                if (_logger.IsTraceEnabled)
                    await _logger.LogTraceAsync($"Executing {dynamicTests.Count} dynamic test(s)").ConfigureAwait(false);

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
            if (_logger.IsTraceEnabled)
                await _logger.LogTraceAsync($"Executing {dynamicTests.Count} remaining dynamic test(s)").ConfigureAwait(false);

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

#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private Task ExecuteTestsAsync(
        AbstractExecutableTest[] tests,
        CancellationToken cancellationToken)
    {
        // All paths run through the shared limiter so the DOP cap is unified in
        // GetMaxParallelism. "Unlimited" is resolved to the default cap
        // (ProcessorCount * 4) there rather than being a separate code path.
#if NET8_0_OR_GREATER
        return ExecuteWithGlobalLimitAsync(tests, cancellationToken);
#else
        return ExecuteWithGlobalLimitAsync(tests, _maxParallelismSemaphore.Value, cancellationToken);
#endif
    }

#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteSequentiallyAsync(
        AbstractExecutableTest[] tests,
        CancellationToken cancellationToken)
    {
        foreach (var test in tests)
        {
            test.ExecutionTask ??= _testRunner.ExecuteTestAsync(test, cancellationToken).AsTask();
            await test.ExecutionTask.ConfigureAwait(false);
        }
    }

#if NET8_0_OR_GREATER
    private Task ExecuteWithGlobalLimitAsync(
        AbstractExecutableTest[] tests,
        CancellationToken cancellationToken)
    {
        return Parallel.ForEachAsync(
            tests,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _maxParallelism.Value,
                CancellationToken = cancellationToken
            },
            async (test, ct) =>
            {
                test.ExecutionTask ??= _testRunner.ExecuteTestAsync(test, ct).AsTask();
                await test.ExecutionTask.ConfigureAwait(false);
            }
        );
    }
#else
    private async Task ExecuteWithGlobalLimitAsync(
        AbstractExecutableTest[] tests,
        SemaphoreSlim globalSemaphore,
        CancellationToken cancellationToken)
    {
        // Fallback for netstandard2.0: Manual bounded concurrency using existing semaphore
        var tasks = new Task[tests.Length];
        for (var i = 0; i < tests.Length; i++)
        {
            var test = tests[i];
            tasks[i] = Task.Run(async () =>
            {
                await globalSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    test.ExecutionTask ??= _testRunner.ExecuteTestAsync(test, cancellationToken).AsTask();
                    await test.ExecutionTask.ConfigureAwait(false);
                }
                finally
                {
                    globalSemaphore.Release();
                }
            }, CancellationToken.None);
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
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
        // "Unlimited" (caller passed 0) resolves to the same cap as the default path.
        // Parallel.ForEachAsync with MaxDegreeOfParallelism = -1 is truly unbounded —
        // with thousands of tests this saturates IOCP threads, so we always apply a cap.
        // ProcessorCount * 4 is empirically sized for async/IO-bound workloads.
        var defaultLimit = Environment.ProcessorCount * 4;

        // Check command line argument first (highest priority)
        if (commandLineOptions.TryGetOptionArgumentList(
                MaximumParallelTestsCommandProvider.MaximumParallelTests,
                out var args) && args.Length > 0 && int.TryParse(args[0], out var maxParallelTests))
        {
            if (maxParallelTests == 0)
            {
                // 0 historically meant "unlimited"; we now treat it the same as the
                // default cap so the unlimited path is not a regression against the
                // default path.
                logger.LogDebug($"Maximum parallel tests: unlimited requested from command line, capped to default {defaultLimit} to avoid IOCP saturation");
                return defaultLimit;
            }

            if (maxParallelTests > 0)
            {
                logger.LogDebug($"Maximum parallel tests limit set to {maxParallelTests} (from command line)");
                return maxParallelTests;
            }
        }

        // Check environment variable (second priority)
        if (Environment.GetEnvironmentVariable(EnvironmentConstants.MaxParallelTests) is string envVar
            && int.TryParse(envVar, out var envLimit))
        {
            if (envLimit == 0)
            {
                logger.LogDebug($"Maximum parallel tests: unlimited requested from TUNIT_MAX_PARALLEL_TESTS, capped to default {defaultLimit} to avoid IOCP saturation");
                return defaultLimit;
            }

            if (envLimit > 0)
            {
                logger.LogDebug($"Maximum parallel tests limit set to {envLimit} (from TUNIT_MAX_PARALLEL_TESTS environment variable)");
                return envLimit;
            }
        }

        // Check TUnitSettings (third priority — code-level project defaults)
        if (TUnitSettings.Default.Parallelism.MaximumParallelTests is { } codeLimit)
        {
            if (codeLimit == 0)
            {
                logger.LogDebug($"Maximum parallel tests: unlimited requested from TUnitSettings, capped to default {defaultLimit} to avoid IOCP saturation");
                return defaultLimit;
            }

            logger.LogDebug($"Maximum parallel tests limit set to {codeLimit} (from TUnitSettings)");
            return codeLimit;
        }

        // Default: 4x CPU cores (empirically optimized for async/IO-bound workloads)
        // Users can override via --maximum-parallel-tests or TUNIT_MAX_PARALLEL_TESTS
        logger.LogDebug($"Maximum parallel tests limit defaulting to {defaultLimit} ({Environment.ProcessorCount} processors * 4)");
        return defaultLimit;
    }
}
