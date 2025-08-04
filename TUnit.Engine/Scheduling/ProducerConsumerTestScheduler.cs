using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Logging;
using TUnit.Core.Models;
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
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;
    private readonly HookOrchestrator _hookOrchestrator;
    private readonly ConcurrentDictionary<string, int> _runningConstraintKeys = new();
    private int _routedTestCount = 0;
    private int _completedTestCount = 0;

    public ProducerConsumerTestScheduler(
        TUnitFrameworkLogger logger,
        ITestGroupingService groupingService,
        SchedulerConfiguration configuration,
        EventReceiverOrchestrator eventReceiverOrchestrator,
        HookOrchestrator hookOrchestrator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _groupingService = groupingService ?? throw new ArgumentNullException(nameof(groupingService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _eventReceiverOrchestrator = eventReceiverOrchestrator ?? throw new ArgumentNullException(nameof(eventReceiverOrchestrator));
        _hookOrchestrator = hookOrchestrator ?? throw new ArgumentNullException(nameof(hookOrchestrator));
        
        _channelRouter = new TestChannelRouter(logger);
        _consumerManager = new ChannelConsumerManager(logger, configuration);
        _executionContextManager = new ExecutionContextManager(logger);
    }

    public async Task ScheduleAndExecuteAsync(
        IEnumerable<AbstractExecutableTest> tests,
        ITestExecutor executor,
        CancellationToken cancellationToken)
    {
        if (tests == null)
        {
            throw new ArgumentNullException(nameof(tests));
        }
        if (executor == null)
        {
            throw new ArgumentNullException(nameof(executor));
        }

        var testList = tests.ToList();
        if (testList.Count == 0)
        {
            await _logger.LogDebugAsync("No tests to execute");
            return;
        }

        await _logger.LogInformationAsync($"Scheduling {testList.Count} tests for execution");
        
        _routedTestCount = 0;

        var groupedTests = await _groupingService.GroupTestsByConstraintsAsync(testList);
        
        var executionStates = new Dictionary<string, TestExecutionState>();
        var orderedTests = new List<TestExecutionState>();
        
        foreach (var test in groupedTests.Parallel)
        {
            var state = new TestExecutionState(test);
            executionStates[test.TestId] = state;
            orderedTests.Add(state);
        }
        
        while (groupedTests.NotInParallel.TryDequeue(out var test, out var testPriority))
        {
            var state = new TestExecutionState(test);
            executionStates[test.TestId] = state;
            orderedTests.Add(state);
            
        }
        
        foreach (var kvp in groupedTests.KeyedNotInParallel)
        {
            var keyedOrderedTests = new List<TestExecutionState>();
            while (kvp.Value.TryDequeue(out var test, out var testPriority))
            {
                var state = new TestExecutionState(test);
                executionStates[test.TestId] = state;
                keyedOrderedTests.Add(state);
            }
            orderedTests.AddRange(keyedOrderedTests);
        }
        
        foreach (var group in groupedTests.ParallelGroups)
        {
            foreach (var orderGroup in group.Value)
            {
                foreach (var test in orderGroup.Value)
                {
                    var state = new TestExecutionState(test);
                    executionStates[test.TestId] = state;
                    orderedTests.Add(state);
                }
            }
        }
        
        await SetupDependencyGraphAsync(executionStates, cancellationToken);

        // Log failed tests
        var failedTests = executionStates.Values.Where(s => s.State == TestState.Failed).ToList();
        if (failedTests.Any())
        {
            await _logger.LogInformationAsync($"Found {failedTests.Count} failed tests before execution (likely circular dependencies)");
            foreach (var failed in failedTests)
            {
                await _logger.LogDebugAsync($"Failed test: {failed.Test.TestId} - {failed.Test.Context.TestName}");
            }
        }

        await PreCreateChannelsForConstraintsAsync(executionStates.Values, cancellationToken);
        
        var testsToRoute = new List<TestExecutionState>();
        var keyedTestsWithOrder = new Dictionary<string, List<TestExecutionState>>();
        
        foreach (var state in orderedTests)
        {
            // Route failed tests (e.g., circular dependencies) immediately regardless of dependencies
            // Also route tests with no remaining dependencies
            if (state.RemainingDependencies == 0 || state.State == TestState.Failed)
            {
                if (state.ConstraintKey != null && state.Order != int.MaxValue / 2)
                {
                    if (!keyedTestsWithOrder.ContainsKey(state.ConstraintKey))
                    {
                        keyedTestsWithOrder[state.ConstraintKey] = new List<TestExecutionState>();
                    }
                    keyedTestsWithOrder[state.ConstraintKey].Add(state);
                }
                else
                {
                    testsToRoute.Add(state);
                }
            }
        }
        
        foreach (var state in testsToRoute)
        {
            // Route all tests, including pre-failed ones (e.g., circular dependencies)
            // The executor will handle reporting for pre-failed tests
            await RouteTestAsync(state, cancellationToken);
        }
        
        foreach (var kvp in keyedTestsWithOrder)
        {
            var sortedTests = kvp.Value.OrderBy(s => s.Order).ToList();
            foreach (var state in sortedTests)
            {
                // Route all tests, including pre-failed ones (e.g., circular dependencies)
                // The executor will handle reporting for pre-failed tests
                await RouteTestAsync(state, cancellationToken);
            }
        }
        
        var totalTestCount = executionStates.Count;
        
        // Continue with execution even if all tests are pre-failed
        // The executor will handle reporting for pre-failed tests
        var nonFailedCount = executionStates.Values.Count(s => s.State != TestState.Failed);
        if (nonFailedCount == 0)
        {
            await _logger.LogInformationAsync("All tests are pre-failed. Will route them for proper reporting.");
        }
        
        var consumerTask = _consumerManager.StartConsumersAsync(
            executor,
            _runningConstraintKeys,
            async (test, token) => await ExecuteTestWithContextAsync(test, executor, executionStates, token),
            _channelRouter.GetMultiplexer(),
            cancellationToken);
        
        while (_completedTestCount < totalTestCount)
        {
            await Task.Delay(100, cancellationToken);
        }
        
        _channelRouter.SignalCompletion();
        
        await consumerTask;
        
        await _logger.LogInformationAsync("Test execution completed");
    }

    private async Task RouteTestAsync(TestExecutionState state, CancellationToken cancellationToken)
    {
        var testData = new TestExecutionData
        {
            Test = state.Test,
            ExecutionContext = ExecutionContext.Capture(),
            Constraints = GetConstraintKeys(state),
            Priority = state.Priority,
            State = state
        };

        await _channelRouter.RouteTestAsync(testData, cancellationToken);
        
        Interlocked.Increment(ref _routedTestCount);
    }

    private async Task ExecuteTestWithContextAsync(
        TestExecutionData testData,
        ITestExecutor executor,
        Dictionary<string, TestExecutionState> executionStates,
        CancellationToken cancellationToken)
    {
        var state = testData.State;
        
        // If test is already failed (e.g., due to circular dependencies), 
        // still execute it so the failure can be reported to the test platform
        if (state.State == TestState.Failed)
        {
            await executor.ExecuteTestAsync(state.Test, cancellationToken);
            await ProcessTestCompletionAsync(state, executionStates, cancellationToken);
            return;
        }
        
        try
        {
            state.State = TestState.Running;
            
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (_configuration.TestTimeout != TimeSpan.Zero)
            {
                timeoutCts.CancelAfter(_configuration.TestTimeout);
            }

            await _executionContextManager.ExecuteWithRestoredContextAsync(
                testData,
                async () => await executor.ExecuteTestAsync(state.Test, timeoutCts.Token),
                timeoutCts.Token);

            await ProcessTestCompletionAsync(state, executionStates, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            state.State = TestState.Cancelled;
            await ProcessTestCompletionAsync(state, executionStates, cancellationToken);
        }
        catch (Exception ex)
        {
            await LoggingExtensions.LogErrorAsync(_logger, $"Test {state.Test.Context.TestName} failed: {ex.Message}");
            await ProcessTestCompletionAsync(state, executionStates, cancellationToken);
        }
        finally
        {
            foreach (var key in testData.Constraints)
            {
                _runningConstraintKeys.AddOrUpdate(key, 0, (k, v) => Math.Max(0, v - 1));
            }
            
            try
            {
                using var cleanupCts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                await _hookOrchestrator.OnTestCompletedAsync(state.Test, cleanupCts.Token);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Hook orchestration failed for test {state.Test.Context.TestName}: {ex.Message}");
                
                if (state.Test.State == TestState.Passed)
                {
                    state.Test.State = TestState.Failed;
                    state.Test.Result = new TestResult
                    {
                        State = TestState.Failed,
                        Start = state.Test.StartTime,
                        End = state.Test.EndTime,
                        Duration = state.Test.EndTime.GetValueOrDefault() - state.Test.StartTime.GetValueOrDefault(),
                        Exception = ex,
                        ComputerName = Environment.MachineName
                    };
                }
            }
            
            Interlocked.Increment(ref _completedTestCount);
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
            keys.Add($"__parallel_group_{parallelGroup.Group}__");
        }
        
        return keys;
    }
    
    private Task PreCreateChannelsForConstraintsAsync(IEnumerable<TestExecutionState> states, CancellationToken cancellationToken)
    {
        var allConstraintKeys = new HashSet<string>();
        var constraintKeyHasExplicitOrder = new Dictionary<string, bool>();
        
        foreach (var state in states)
        {
            var keys = GetConstraintKeys(state);
            foreach (var key in keys)
            {
                allConstraintKeys.Add(key);
                
                if (!constraintKeyHasExplicitOrder.ContainsKey(key))
                {
                    constraintKeyHasExplicitOrder[key] = false;
                }
                
                if (state.Order != int.MaxValue / 2)
                {
                    constraintKeyHasExplicitOrder[key] = true;
                }
            }
        }
        
        foreach (var key in allConstraintKeys)
        {
            if (key.StartsWith("__parallel_group_"))
            {
                _channelRouter.GetMultiplexer().GetOrCreateParallelGroupChannel(key);
            }
            else if (key != "__global_not_in_parallel__")
            {
                var hasExplicitOrder = constraintKeyHasExplicitOrder.ContainsKey(key) ? constraintKeyHasExplicitOrder[key] : false;
                _channelRouter.GetMultiplexer().GetOrCreateKeyedNotInParallelChannel(key, hasExplicitOrder);
            }
        }
        
        return Task.CompletedTask;
    }
    
    private async Task SetupDependencyGraphAsync(Dictionary<string, TestExecutionState> executionStates, CancellationToken cancellationToken)
    {
        var testLookup = executionStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        foreach (var kvp in executionStates)
        {
            var testId = kvp.Key;
            var state = kvp.Value;
            var test = state.Test;
            
            // Skip if already marked as failed due to circular dependency
            if (state.State == TestState.Failed)
            {
                continue;
            }
            
            // Process dependencies for this test
            foreach (var dependency in test.Dependencies)
            {
                if (testLookup.TryGetValue(dependency.TestId, out var dependencyState))
                {
                    dependencyState.Dependents.Add(testId);
                }
                else
                {
                    await LoggingExtensions.LogErrorAsync(_logger, $"Dependency not found: {testId} depends on {dependency.TestId}");
                }
            }
        }
        
        var testsWithDependencies = executionStates.Values.Where(s => s.RemainingDependencies > 0).ToList();
        var readyTests = executionStates.Values.Where(s => s.RemainingDependencies == 0).ToList();
        
        await _logger.LogInformationAsync($"Dependency setup: {testsWithDependencies.Count} tests with dependencies, {readyTests.Count} ready tests");
        
    }
    
    private async Task ProcessTestCompletionAsync(TestExecutionState completedTest, Dictionary<string, TestExecutionState> executionStates, CancellationToken cancellationToken)
    {
        var newlyReadyTests = new List<TestExecutionState>();
        var keyedTestsWithOrder = new Dictionary<string, List<TestExecutionState>>();
        
        foreach (var dependentId in completedTest.Dependents)
        {
            if (executionStates.TryGetValue(dependentId, out var dependentState))
            {
                var remaining = dependentState.DecrementRemainingDependencies();
                
                if (remaining == 0 && dependentState.State == TestState.NotStarted)
                {
                    if (dependentState.ConstraintKey != null && dependentState.Order != int.MaxValue / 2)
                    {
                        if (!keyedTestsWithOrder.ContainsKey(dependentState.ConstraintKey))
                        {
                            keyedTestsWithOrder[dependentState.ConstraintKey] = new List<TestExecutionState>();
                        }
                        keyedTestsWithOrder[dependentState.ConstraintKey].Add(dependentState);
                    }
                    else
                    {
                        newlyReadyTests.Add(dependentState);
                    }
                }
            }
        }
        
        foreach (var readyTest in newlyReadyTests)
        {
            // Route all tests, including pre-failed ones (e.g., circular dependencies)
            // The executor will handle reporting for pre-failed tests
            await RouteTestAsync(readyTest, cancellationToken);
        }
        
        foreach (var kvp in keyedTestsWithOrder)
        {
            var sortedTests = kvp.Value.OrderBy(s => s.Order).ToList();
            foreach (var readyTest in sortedTests)
            {
                // Route all tests, including pre-failed ones (e.g., circular dependencies)
                // The executor will handle reporting for pre-failed tests
                await RouteTestAsync(readyTest, cancellationToken);
            }
        }
    }
    
    
}

internal class TestExecutionData
{
    public required AbstractExecutableTest Test { get; init; }
    public required ExecutionContext? ExecutionContext { get; init; }
    public required List<string> Constraints { get; init; }
    public required Priority Priority { get; init; }
    public required TestExecutionState State { get; init; }
}