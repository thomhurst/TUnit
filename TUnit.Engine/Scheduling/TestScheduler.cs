using System.Buffers;
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
                    await _testStateManager.MarkCircularDependencyFailedAsync(chainTest, exception).ConfigureAwait(false);
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
            var tasks = ArrayPool<Task>.Shared.Rent(tests.Length);
            try
            {
                for (var i = 0; i < tests.Length; i++)
                {
                    var test = tests[i];
                    tasks[i] = test.ExecutionTask ??= Task.Run(() => ExecuteSingleTestAsync(test, cancellationToken), CancellationToken.None);
                }

                await WaitForTasksWithFailFastHandling(new ArraySegment<Task>(tasks, 0, tests.Length), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<Task>.Shared.Return(tasks);
            }
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

    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteWithGlobalLimitAsync(
        AbstractExecutableTest[] tests,
        CancellationToken cancellationToken)
    {
        var tasks = ArrayPool<Task>.Shared.Rent(tests.Length);
        try
        {
            for (var i = 0; i < tests.Length; i++)
            {
                var test = tests[i];
                tasks[i] = Task.Run(async () =>
                {
                    SemaphoreSlim? parallelLimiterSemaphore = null;

                    if (test.Context.ParallelLimiter != null)
                    {
                        parallelLimiterSemaphore = _parallelLimitLockProvider.GetLock(test.Context.ParallelLimiter);
                        await parallelLimiterSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }

                    try
                    {
                        await _maxParallelismSemaphore!.WaitAsync(cancellationToken).ConfigureAwait(false);
                        try
                        {
                            test.ExecutionTask ??= _testRunner.ExecuteTestAsync(test, cancellationToken);
                            await test.ExecutionTask.ConfigureAwait(false);
                        }
                        finally
                        {
                            _maxParallelismSemaphore.Release();
                        }
                    }
                    finally
                    {
                        parallelLimiterSemaphore?.Release();
                    }
                }, CancellationToken.None);
            }

            await WaitForTasksWithFailFastHandling(new ArraySegment<Task>(tasks, 0, tests.Length), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<Task>.Shared.Return(tasks);
        }
    }

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
