using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Logging;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;
using LoggingExtensions = TUnit.Core.Logging.LoggingExtensions;

namespace TUnit.Engine.Scheduling;

internal sealed class ProducerConsumerTestScheduler : ITestScheduler
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestGroupingService _groupingService;
    private readonly SchedulerConfiguration _configuration;
    private readonly TestChannelRouter _channelRouter;
    private readonly ChannelConsumerManager _consumerManager;
    private readonly ExecutionContextManager _executionContextManager;
    private readonly ConcurrentDictionary<string, int> _runningConstraintKeys = new();

    public ProducerConsumerTestScheduler(
        TUnitFrameworkLogger logger,
        ITestGroupingService groupingService,
        SchedulerConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _groupingService = groupingService ?? throw new ArgumentNullException(nameof(groupingService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        _channelRouter = new TestChannelRouter(logger);
        _consumerManager = new ChannelConsumerManager(logger, configuration);
        _executionContextManager = new ExecutionContextManager(logger);
    }

    public async Task ScheduleAndExecuteAsync(
        IEnumerable<ExecutableTest> tests,
        ITestExecutor executor,
        CancellationToken cancellationToken)
    {
        if (tests == null) throw new ArgumentNullException(nameof(tests));
        if (executor == null) throw new ArgumentNullException(nameof(executor));

        var testList = tests.ToList();
        if (testList.Count == 0)
        {
            await _logger.LogDebugAsync("No tests to execute");
            return;
        }

        await _logger.LogInformationAsync($"Scheduling {testList.Count} tests for execution");

        // Group tests by constraints
        var groupedTests = await _groupingService.GroupTestsByConstraintsAsync(testList);
        
        // Convert to execution states
        var executionStates = new Dictionary<string, TestExecutionState>();
        
        // Process parallel tests
        foreach (var test in groupedTests.Parallel)
        {
            var state = new TestExecutionState(test)
            {
                Priority = test.Context.ExecutionPriority
            };
            executionStates[test.TestId] = state;
        }
        
        // Process not-in-parallel tests
        while (groupedTests.NotInParallel.TryDequeue(out var test, out _))
        {
            var state = new TestExecutionState(test)
            {
                Constraint = test.Context.ParallelConstraint,
                Priority = test.Context.ExecutionPriority
            };
            executionStates[test.TestId] = state;
        }
        
        // Process keyed not-in-parallel tests
        foreach (var kvp in groupedTests.KeyedNotInParallel)
        {
            while (kvp.Value.TryDequeue(out var test, out _))
            {
                var state = new TestExecutionState(test)
                {
                    Constraint = test.Context.ParallelConstraint,
                    ConstraintKey = kvp.Key,
                    Priority = test.Context.ExecutionPriority
                };
                executionStates[test.TestId] = state;
            }
        }
        
        // Process parallel groups
        foreach (var group in groupedTests.ParallelGroups)
        {
            foreach (var orderGroup in group.Value)
            {
                foreach (var test in orderGroup.Value)
                {
                    var state = new TestExecutionState(test)
                    {
                        Constraint = test.Context.ParallelConstraint,
                        ConstraintKey = group.Key,
                        Priority = test.Context.ExecutionPriority
                    };
                    executionStates[test.TestId] = state;
                }
            }
        }
        
        // Set up dependency graph
        await SetupDependencyGraphAsync(executionStates, cancellationToken);

        // Pre-create channels for all constraint keys to avoid dynamic creation during execution
        await PreCreateChannelsForConstraintsAsync(executionStates.Values, cancellationToken);
        
        // Route all ready tests
        var readyTests = executionStates.Values.Where(s => s.RemainingDependencies == 0).ToList();
        
        // Route all tests to channels in batches
        const int batchSize = 100;
        for (int i = 0; i < readyTests.Count; i += batchSize)
        {
            var batch = readyTests.Skip(i).Take(batchSize);
            await Task.WhenAll(batch.Select(state => RouteTestAsync(state, cancellationToken)));
        }
        
        // Signal completion - no more tests will be added
        _channelRouter.SignalCompletion();
        
        // Start consumers - they will consume until channels are empty and completed
        await _consumerManager.StartConsumersAsync(
            executor,
            _runningConstraintKeys,
            async (test, token) => await ExecuteTestWithContextAsync(test, executor, executionStates, token),
            _channelRouter.GetMultiplexer(),
            cancellationToken);
        
        await _logger.LogInformationAsync("Test execution completed");
    }

    private async Task RouteTestAsync(TestExecutionState state, CancellationToken cancellationToken)
    {
        // Capture execution context at routing time
        var testData = new TestExecutionData
        {
            Test = state.Test,
            ExecutionContext = ExecutionContext.Capture(),
            Constraints = GetConstraintKeys(state),
            Priority = state.Priority,
            State = state
        };

        // Route test to appropriate channel
        await _channelRouter.RouteTestAsync(testData, cancellationToken);
    }

    private async Task ExecuteTestWithContextAsync(
        TestExecutionData testData,
        ITestExecutor executor,
        Dictionary<string, TestExecutionState> executionStates,
        CancellationToken cancellationToken)
    {
        var state = testData.State;
        
        try
        {
            // Set test state
            state.State = TestState.Running;
            
            // Create timeout cancellation
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (_configuration.TestTimeout != TimeSpan.Zero)
            {
                timeoutCts.CancelAfter(_configuration.TestTimeout);
            }

            // Execute test with restored context
            await _executionContextManager.ExecuteWithRestoredContextAsync(
                testData,
                async () => await executor.ExecuteTestAsync(state.Test, timeoutCts.Token),
                timeoutCts.Token);

            // State will be set by the executor based on test result
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            state.State = TestState.Cancelled;
        }
        catch (Exception ex)
        {
            await LoggingExtensions.LogErrorAsync(_logger, $"Test {state.Test.Context.TestName} failed: {ex.Message}");
        }
        finally
        {
            // Release constraint keys
            foreach (var key in testData.Constraints)
            {
                _runningConstraintKeys.AddOrUpdate(key, 0, (k, v) => Math.Max(0, v - 1));
            }
            
            // Process dependents and route newly ready tests
            await ProcessTestCompletionAsync(state, executionStates, cancellationToken);
        }
    }

    private List<string> GetConstraintKeys(TestExecutionState state)
    {
        var keys = new List<string>();
        
        if (state.Constraint is NotInParallelConstraint notInParallel)
        {
            if (notInParallel.NotInParallelConstraintKeys.Count == 0)
            {
                keys.Add("__global_not_in_parallel__");
            }
            else
            {
                keys.AddRange(notInParallel.NotInParallelConstraintKeys);
            }
        }
        else if (state.Constraint is ParallelGroupConstraint parallelGroup)
        {
            // Fix: Add support for ParallelGroup constraints
            keys.Add($"__parallel_group_{parallelGroup.Group}__");
        }
        
        return keys;
    }
    
    private Task PreCreateChannelsForConstraintsAsync(IEnumerable<TestExecutionState> states, CancellationToken cancellationToken)
    {
        var allConstraintKeys = new HashSet<string>();
        
        foreach (var state in states)
        {
            var keys = GetConstraintKeys(state);
            foreach (var key in keys)
            {
                allConstraintKeys.Add(key);
            }
        }
        
        // Pre-create channels for all constraint keys
        foreach (var key in allConstraintKeys)
        {
            if (key.StartsWith("__parallel_group_"))
            {
                _channelRouter.GetMultiplexer().GetOrCreateParallelGroupChannel(key);
            }
            else if (key != "__global_not_in_parallel__") // Global channel is already created
            {
                _channelRouter.GetMultiplexer().GetOrCreateKeyedNotInParallelChannel(key);
            }
        }
        
        // Pre-created channels for constraint keys
        return Task.CompletedTask;
    }
    
    private async Task SetupDependencyGraphAsync(Dictionary<string, TestExecutionState> executionStates, CancellationToken cancellationToken)
    {
        // Create a lookup for fast test resolution
        var testLookup = executionStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        // Set up dependency relationships
        foreach (var kvp in executionStates)
        {
            var testId = kvp.Key;
            var state = kvp.Value;
            var test = state.Test;
            
            // Process dependencies for this test
            foreach (var dependency in test.Dependencies)
            {
                if (testLookup.TryGetValue(dependency.TestId, out var dependencyState))
                {
                    // Add this test as a dependent of the dependency
                    dependencyState.Dependents.Add(testId);
                    // Dependency relationship established
                }
                else
                {
                    await LoggingExtensions.LogErrorAsync(_logger, $"Dependency not found: {testId} depends on {dependency.TestId}");
                }
            }
        }
        
        // Log dependency status
        var testsWithDependencies = executionStates.Values.Where(s => s.RemainingDependencies > 0).ToList();
        var readyTests = executionStates.Values.Where(s => s.RemainingDependencies == 0).ToList();
        
        await _logger.LogInformationAsync($"Dependency setup: {testsWithDependencies.Count} tests with dependencies, {readyTests.Count} ready tests");
    }
    
    private async Task ProcessTestCompletionAsync(TestExecutionState completedTest, Dictionary<string, TestExecutionState> executionStates, CancellationToken cancellationToken)
    {
        // Process dependents of the completed test
        var newlyReadyTests = new List<TestExecutionState>();
        
        foreach (var dependentId in completedTest.Dependents)
        {
            if (executionStates.TryGetValue(dependentId, out var dependentState))
            {
                var remaining = dependentState.DecrementRemainingDependencies();
                // Dependency count decremented
                
                if (remaining == 0 && dependentState.State == TestState.NotStarted)
                {
                    // Test is now ready for execution
                    newlyReadyTests.Add(dependentState);
                }
            }
        }
        
        // Route all newly ready tests
        foreach (var readyTest in newlyReadyTests)
        {
            await RouteTestAsync(readyTest, cancellationToken);
        }
    }
    
}

internal class TestExecutionData
{
    public required ExecutableTest Test { get; init; }
    public required ExecutionContext? ExecutionContext { get; init; }
    public required List<string> Constraints { get; init; }
    public required Priority Priority { get; init; }
    public required TestExecutionState State { get; init; }
}