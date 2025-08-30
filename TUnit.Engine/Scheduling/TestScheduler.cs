using EnumerableAsyncProcessor.Extensions;
using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Logging;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;
using TUnit.Engine.Services;

namespace TUnit.Engine.Scheduling;

internal static class WorkerPoolCalculator
{
    private static readonly object _lock = new object();
    private static DateTime _lastCalculation = DateTime.MinValue;
    private static int _cachedOptimalWorkerCount = Environment.ProcessorCount;
    private static readonly TimeSpan CalculationCacheTime = TimeSpan.FromSeconds(5);

    public static int CalculateWorkerCount(int testCount, int? requestedMax)
    {
        lock (_lock)
        {
            // Use cached value if recent
            if (DateTime.UtcNow - _lastCalculation < CalculationCacheTime)
            {
                return Math.Min(requestedMax ?? _cachedOptimalWorkerCount, _cachedOptimalWorkerCount);
            }

            // Calculate based on system characteristics
            var processorCount = Environment.ProcessorCount;
            var availableMemory = GC.GetTotalMemory(false);
            
            // Base worker count on processor count
            var baseWorkerCount = processorCount;
            
            // Adjust based on test count - fewer workers for small test sets
            if (testCount < processorCount)
            {
                baseWorkerCount = Math.Max(1, testCount);
            }
            else if (testCount > processorCount * 4)
            {
                // Scale up for large test sets, but cap at reasonable limit
                baseWorkerCount = Math.Min(processorCount * 2, testCount / 4);
            }
            
            // Apply memory pressure adjustment
            if (availableMemory > 500_000_000) // > 500MB available
            {
                baseWorkerCount = Math.Min(baseWorkerCount * 2, processorCount * 3);
            }
            else if (availableMemory < 100_000_000) // < 100MB available
            {
                baseWorkerCount = Math.Max(1, baseWorkerCount / 2);
            }

            // Respect requested maximum
            var finalWorkerCount = requestedMax.HasValue 
                ? Math.Min(requestedMax.Value, baseWorkerCount)
                : baseWorkerCount;

            _cachedOptimalWorkerCount = finalWorkerCount;
            _lastCalculation = DateTime.UtcNow;
            
            return finalWorkerCount;
        }
    }
}

internal static class EfficientWorkQueue<T>
{
    private readonly T[] _items;
    private readonly int _mask;
    private volatile int _headIndex;
    private volatile int _tailIndex;
    private readonly object _lock = new object();

    public EfficientWorkQueue(int capacity = 1024)
    {
        // Round up to next power of 2 for efficient masking
        var actualCapacity = 1;
        while (actualCapacity < capacity)
            actualCapacity <<= 1;
            
        _items = new T[actualCapacity];
        _mask = actualCapacity - 1;
        _headIndex = 0;
        _tailIndex = 0;
    }

    public bool TryEnqueue(T item)
    {
        lock (_lock)
        {
            var tail = _tailIndex;
            var nextTail = (tail + 1) & _mask;
            
            if (nextTail == _headIndex)
                return false; // Queue is full
                
            _items[tail] = item;
            _tailIndex = nextTail;
            return true;
        }
    }

    public bool TryDequeue(out T item)
    {
        item = default!;
        
        var head = _headIndex;
        if (head == _tailIndex)
            return false; // Queue is empty
            
        lock (_lock)
        {
            // Double-check under lock
            head = _headIndex;
            if (head == _tailIndex)
                return false;
                
            item = _items[head];
            _items[head] = default!; // Help GC
            _headIndex = (head + 1) & _mask;
            return true;
        }
    }

    public int Count
    {
        get
        {
            var tail = _tailIndex;
            var head = _headIndex;
            return (tail - head) & _mask;
        }
    }
}

internal static class WorkStealingQueue
{
    public static async Task ProcessWorkItems<T>(
        IEnumerable<T> workItems,
        Func<T, Task> processor,
        int workerCount,
        CancellationToken cancellationToken)
    {
        if (workerCount <= 0)
            throw new ArgumentException("Worker count must be positive", nameof(workerCount));

        var workArray = workItems.ToArray();
        if (workArray.Length == 0)
            return;

        // Distribute work across multiple queues to reduce contention
        var queueCount = Math.Min(workerCount, 4); // Max 4 queues to avoid too much overhead
        var queues = new ConcurrentQueue<T>[queueCount];
        
        for (int i = 0; i < queueCount; i++)
        {
            queues[i] = new ConcurrentQueue<T>();
        }

        // Distribute work items across queues
        for (int i = 0; i < workArray.Length; i++)
        {
            queues[i % queueCount].Enqueue(workArray[i]);
        }

        // Create workers that can steal from other queues
        var workers = new Task[workerCount];
        
        for (int workerId = 0; workerId < workerCount; workerId++)
        {
            var primaryQueue = workerId % queueCount;
            workers[workerId] = ProcessWithWorkStealing(queues, primaryQueue, processor, cancellationToken);
        }

        await Task.WhenAll(workers).ConfigureAwait(false);
    }

    private static async Task ProcessWithWorkStealing<T>(
        ConcurrentQueue<T>[] queues,
        int primaryQueueIndex,
        Func<T, Task> processor,
        CancellationToken cancellationToken)
    {
        var queueCount = queues.Length;
        var attempts = 0;
        const int maxEmptyAttempts = queueCount * 2;

        while (!cancellationToken.IsCancellationRequested)
        {
            T workItem = default!;
            bool foundWork = false;

            // Try primary queue first
            if (queues[primaryQueueIndex].TryDequeue(out workItem))
            {
                foundWork = true;
                attempts = 0;
            }
            else
            {
                // Work stealing: try other queues
                for (int i = 1; i < queueCount; i++)
                {
                    var queueIndex = (primaryQueueIndex + i) % queueCount;
                    if (queues[queueIndex].TryDequeue(out workItem))
                    {
                        foundWork = true;
                        attempts = 0;
                        break;
                    }
                }
            }

            if (foundWork)
            {
                try
                {
                    await processor(workItem).ConfigureAwait(false);
                }
                catch
                {
                    // Let the processor handle its own exceptions
                    throw;
                }
            }
            else
            {
                attempts++;
                if (attempts >= maxEmptyAttempts)
                {
                    break; // All queues appear empty
                }
                
                // Brief yield to avoid tight spinning
                await Task.Yield();
            }
        }
    }
}

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

        var circularDependencies = DetectCircularDependencies(testList);

        foreach (var (test, dependencyChain) in circularDependencies)
        {
            test.State = TestState.Failed;
            var exception = new DependencyConflictException(dependencyChain);
            test.Result = new TestResult
            {
                State = TestState.Failed,
                Exception = exception,
                ComputerName = Environment.MachineName,
                Start = DateTimeOffset.UtcNow,
                End = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero
            };

            await _messageBus.Failed(test.Context, exception, test.Result.Start ?? DateTimeOffset.UtcNow).ConfigureAwait(false);
        }

        var executableTests = testList.Where(t => !circularDependencies.Any(cd => cd.test == t)).ToList();
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
            // Batch dependency checking for better performance
            if (test.Dependencies.Count > 0)
            {
                var dependencyTasks = new Task[test.Dependencies.Count];
                for (int i = 0; i < test.Dependencies.Count; i++)
                {
                    dependencyTasks[i] = test.Dependencies[i].Test.ExecutionTask;
                }
                
                try
                {
                    // Wait for all dependencies at once instead of sequentially
                    await Task.WhenAll(dependencyTasks).ConfigureAwait(false);
                    
                    // Quick check for failed dependencies that should cause skipping
                    for (int i = 0; i < test.Dependencies.Count; i++)
                    {
                        var dependency = test.Dependencies[i];
                        if (dependency.Test.State == TestState.Failed && !dependency.ProceedOnFailure)
                        {
                            await SkipTestDueToDependency(test, dependency.Test.TestId).ConfigureAwait(false);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Find which dependency failed and handle accordingly
                    var shouldProceed = true;
                    for (int i = 0; i < test.Dependencies.Count; i++)
                    {
                        var dependency = test.Dependencies[i];
                        if (dependency.Test.ExecutionTask.IsFaulted && !dependency.ProceedOnFailure)
                        {
                            shouldProceed = false;
                            break;
                        }
                    }
                    
                    if (!shouldProceed)
                    {
                        await _logger.LogErrorAsync($"Dependency failed for test {test.TestId}: {ex}").ConfigureAwait(false);
                        await SkipTestDueToDependency(test, "failed dependency").ConfigureAwait(false);
                        return;
                    }
                }
            }

            // Acquire parallel limit semaphore if needed - optimize for common case of no limit
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

    private async Task SkipTestDueToDependency(AbstractExecutableTest test, string dependencyInfo)
    {
        test.State = TestState.Skipped;
        test.Result = new TestResult
        {
            State = TestState.Skipped,
            Exception = new InvalidOperationException($"Skipped due to failed dependency: {dependencyInfo}"),
            ComputerName = Environment.MachineName,
            Start = DateTimeOffset.UtcNow,
            End = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero
        };
        await _messageBus.Skipped(test.Context, "Skipped due to failed dependencies").ConfigureAwait(false);
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
        // Calculate total unique tests efficiently
        var testSet = new HashSet<AbstractExecutableTest>();
        var totalEstimate = 0;
        for (int i = 0; i < keyedTests.Length; i++)
        {
            totalEstimate += keyedTests[i].Tests.Length;
            for (int j = 0; j < keyedTests[i].Tests.Length; j++)
            {
                testSet.Add(keyedTests[i].Tests[j]);
            }
        }
        
        var uniqueTests = new AbstractExecutableTest[testSet.Count];
        testSet.CopyTo(uniqueTests);
        
        // Build test-to-keys mapping more efficiently using arrays instead of Lists
        var testKeyMap = new Dictionary<AbstractExecutableTest, string[]>(uniqueTests.Length);
        var keyCountMap = new Dictionary<AbstractExecutableTest, int>(uniqueTests.Length);
        
        // Count keys per test first
        for (int i = 0; i < keyedTests.Length; i++)
        {
            var key = keyedTests[i].Key;
            var tests = keyedTests[i].Tests;
            for (int j = 0; j < tests.Length; j++)
            {
                var test = tests[j];
                keyCountMap[test] = keyCountMap.GetValueOrDefault(test, 0) + 1;
            }
        }
        
        // Allocate arrays and populate
        foreach (var kvp in keyCountMap)
        {
            testKeyMap[kvp.Key] = new string[kvp.Value];
        }
        
        var keyIndex = new Dictionary<AbstractExecutableTest, int>(uniqueTests.Length);
        for (int i = 0; i < keyedTests.Length; i++)
        {
            var key = keyedTests[i].Key;
            var tests = keyedTests[i].Tests;
            for (int j = 0; j < tests.Length; j++)
            {
                var test = tests[j];
                var idx = keyIndex.GetValueOrDefault(test, 0);
                testKeyMap[test][idx] = key;
                keyIndex[test] = idx + 1;
            }
        }

        // Sort tests by priority without LINQ - use Array.Sort for better performance
        Array.Sort(uniqueTests, (t1, t2) =>
        {
            var priority1 = t1.Context.ExecutionPriority;
            var priority2 = t2.Context.ExecutionPriority;
            if (priority1 != priority2)
                return priority2.CompareTo(priority1); // Descending
                
            var order1 = (t1.Context.ParallelConstraint as NotInParallelConstraint)?.Order ?? int.MaxValue / 2;
            var order2 = (t2.Context.ParallelConstraint as NotInParallelConstraint)?.Order ?? int.MaxValue / 2;
            return order1.CompareTo(order2); // Ascending
        });

        // Track running tasks by key - use initial capacity for better performance
        var runningKeyedTasks = new Dictionary<string, Task>(keyedTests.Length);
        var conflictBuffer = new List<Task>(8); // Reuse buffer to avoid allocations

        for (int i = 0; i < uniqueTests.Length; i++)
        {
            var test = uniqueTests[i];
            var testKeys = testKeyMap[test];

            // Collect conflicting tasks efficiently
            conflictBuffer.Clear();
            for (int j = 0; j < testKeys.Length; j++)
            {
                if (runningKeyedTasks.TryGetValue(testKeys[j], out var runningTask))
                {
                    conflictBuffer.Add(runningTask);
                }
            }

            if (conflictBuffer.Count > 0)
            {
                if (conflictBuffer.Count == 1)
                {
                    await conflictBuffer[0].ConfigureAwait(false);
                }
                else
                {
                    await Task.WhenAll(conflictBuffer).ConfigureAwait(false);
                }
            }

            // Start the test execution
            var task = test.ExecutionTask;

            // Track this task for all its keys
            for (int j = 0; j < testKeys.Length; j++)
            {
                runningKeyedTasks[testKeys[j]] = task;
            }
        }

        // Wait for all tests to complete - use pre-allocated array
        var allTasks = new Task[uniqueTests.Length];
        for (int i = 0; i < uniqueTests.Length; i++)
        {
            allTasks[i] = uniqueTests[i].ExecutionTask;
        }
        await Task.WhenAll(allTasks).ConfigureAwait(false);
    }

    private async Task ExecuteParallelGroupAsync(
        (int Order, AbstractExecutableTest[] Tests)[] orderedTests,
        int? maxParallelism,
        CancellationToken cancellationToken)
    {
        // Execute order groups sequentially
        foreach (var (order, tests) in orderedTests)
        {
            // Calculate adaptive worker count for this group
            var adaptiveWorkerCount = WorkerPoolCalculator.CalculateWorkerCount(tests.Length, maxParallelism);
            
            if (adaptiveWorkerCount > 0 && tests.Length > adaptiveWorkerCount)
            {
                // Use work-stealing approach for larger groups
                await WorkStealingQueue.ProcessWorkItems(
                    tests,
                    test => test.ExecutionTask,
                    adaptiveWorkerCount,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            else if (tests.Length <= 4)
            {
                // Sequential for very small groups
                foreach (var test in tests)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    await test.ExecutionTask.ConfigureAwait(false);
                }
            }
            else
            {
                // Direct parallel execution for medium groups
                await Task.WhenAll(tests.Select(t => t.ExecutionTask)).ConfigureAwait(false);
            }
        }
    }

    private async Task ExecuteParallelTestsAsync(
        AbstractExecutableTest[] tests,
        int? maxParallelism,
        CancellationToken cancellationToken)
    {
        // Calculate adaptive worker count based on system load and test characteristics
        var adaptiveWorkerCount = WorkerPoolCalculator.CalculateWorkerCount(tests.Length, maxParallelism);
        
        if (adaptiveWorkerCount > 0 && tests.Length > adaptiveWorkerCount)
        {
            // Use work-stealing queue for better efficiency with many tests
            await WorkStealingQueue.ProcessWorkItems(
                tests,
                test => test.ExecutionTask,
                adaptiveWorkerCount,
                cancellationToken
            ).ConfigureAwait(false);
        }
        else
        {
            // For small test counts or single worker, just execute directly
            if (adaptiveWorkerCount == 1 || tests.Length <= 4)
            {
                // Sequential execution is more efficient for very small sets
                foreach (var test in tests)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    await test.ExecutionTask.ConfigureAwait(false);
                }
            }
            else
            {
                // Direct parallel execution for medium sets
                await Task.WhenAll(tests.Select(t => t.ExecutionTask)).ConfigureAwait(false);
            }
        }
    }

    private List<(AbstractExecutableTest test, List<TestDetails> dependencyChain)> DetectCircularDependencies(IList<AbstractExecutableTest> tests)
    {
        var testCount = tests.Count;
        if (testCount == 0)
            return new List<(AbstractExecutableTest, List<TestDetails>)>();

        var circularDependencies = new List<(AbstractExecutableTest, List<TestDetails>)>();
        var visitState = new Dictionary<string, VisitState>(testCount);
        var processedCycles = new HashSet<string>();

        // Pre-size and build test map efficiently
        var testMap = new Dictionary<string, AbstractExecutableTest>(testCount);
        for (int i = 0; i < testCount; i++)
        {
            var test = tests[i];
            testMap.TryAdd(test.TestId, test); // TryAdd is faster than ContainsKey + indexer
        }

        // Pre-allocate path to avoid repeated allocations
        var currentPath = new List<AbstractExecutableTest>(16);
        var cycleBuffer = new List<AbstractExecutableTest>(16);
        var dependencyChainBuffer = new List<TestDetails>(16);

        foreach (var test in tests)
        {
            if (!visitState.ContainsKey(test.TestId))
            {
                currentPath.Clear();
                if (HasCycle(test, testMap, visitState, currentPath))
                {
                    ProcessCycleEfficiently(currentPath, processedCycles, circularDependencies, 
                                          cycleBuffer, dependencyChainBuffer);
                }
            }
        }

        return circularDependencies;
    }

    private void ProcessCycleEfficiently(
        List<AbstractExecutableTest> currentPath,
        HashSet<string> processedCycles,
        List<(AbstractExecutableTest, List<TestDetails>)> circularDependencies,
        List<AbstractExecutableTest> cycleBuffer,
        List<TestDetails> dependencyChainBuffer)
    {
        if (currentPath.Count == 0) return;

        // Find cycle start more efficiently
        var lastTest = currentPath[currentPath.Count - 1];
        var cycleStartIndex = -1;
        
        for (int i = currentPath.Count - 2; i >= 0; i--)
        {
            if (currentPath[i].TestId == lastTest.TestId)
            {
                cycleStartIndex = i;
                break;
            }
        }

        if (cycleStartIndex < 0) return;

        // Build cycle and dependency chain efficiently without LINQ
        cycleBuffer.Clear();
        dependencyChainBuffer.Clear();
        
        for (int i = cycleStartIndex; i < currentPath.Count - 1; i++)
        {
            var test = currentPath[i];
            cycleBuffer.Add(test);
            dependencyChainBuffer.Add(test.Context.TestDetails);
        }

        // Create cycle key efficiently using StringBuilder-like approach
        if (cycleBuffer.Count > 0)
        {
            // Sort test IDs for consistent cycle key without LINQ
            var sortedIds = new string[cycleBuffer.Count];
            for (int i = 0; i < cycleBuffer.Count; i++)
            {
                sortedIds[i] = cycleBuffer[i].TestId;
            }
            Array.Sort(sortedIds);
            
            var cycleKey = string.Join("->", sortedIds);
            
            if (processedCycles.Add(cycleKey)) // Add returns false if already exists
            {
                // Add all cycle tests
                for (int i = 0; i < cycleBuffer.Count; i++)
                {
                    circularDependencies.Add((cycleBuffer[i], new List<TestDetails>(dependencyChainBuffer)));
                }
            }
        }
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
                // We found a cycle - add the dependency to complete the cycle
                currentPath.Add(testMap[depTestId]);
                return true;
            }
        }

        visitState[test.TestId] = VisitState.Visited;
        currentPath.RemoveAt(currentPath.Count - 1);
        return false;
    }

    private enum VisitState
    {
        Visiting,
        Visited
    }
}
