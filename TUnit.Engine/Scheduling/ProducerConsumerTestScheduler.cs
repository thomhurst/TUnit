using System.Collections.Concurrent;
using System.Threading.Channels;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;
using TUnit.Engine.Execution;
using TUnit.Engine.Extensions;
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

        // Create and initialize completion tracker
        var completionTracker = new ChannelBasedTestCompletionTracker(executionStates);
        
        // Route all initial tests in parallel for better performance
        var initialTests = executionStates.Values.Where(s => s.RemainingDependencies == 0).ToList();
        
        // Start routing task to handle test completions
        var routingTask = Task.Run(async () =>
        {
            // Route initial tests in parallel batches
            const int batchSize = 100;
            for (int i = 0; i < initialTests.Count; i += batchSize)
            {
                var batch = initialTests.Skip(i).Take(batchSize);
                await Task.WhenAll(batch.Select(state => RouteTestAsync(state, cancellationToken)));
            }
            
            // Process test completions and route newly ready tests
            await foreach (var completed in completionTracker.ReadyTests.ReadAllAsync(cancellationToken))
            {
                // Check if this is a newly ready test (not yet started)
                if (completed.State == TestState.NotStarted && completed.RemainingDependencies == 0)
                {
                    // Don't await here to avoid blocking other completions
                    _ = RouteTestAsync(completed, cancellationToken);
                }
            }
        }, cancellationToken);
        
        // Start consumers immediately for better parallelism
        var consumerTask = _consumerManager.StartConsumersAsync(
            executor,
            _runningConstraintKeys,
            async (test, token) => await ExecuteTestWithContextAsync(test, executor, completionTracker, token),
            _channelRouter.GetMultiplexer(),
            _channelRouter,
            cancellationToken);

        // Wait for both tasks
        await Task.WhenAll(routingTask, consumerTask);
        
        // Signal completion to consumers
        _channelRouter.SignalCompletion();
        
        await _logger.LogInformationAsync("Test execution completed");
    }

    private Task RouteTestAsync(TestExecutionState state, CancellationToken cancellationToken)
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

        // Don't await to avoid blocking - let the router handle backpressure
        return _channelRouter.RouteTestAsync(testData, cancellationToken);
    }

    private async Task ExecuteTestWithContextAsync(
        TestExecutionData testData,
        ITestExecutor executor,
        ChannelBasedTestCompletionTracker completionTracker,
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
            
            // Notify completion
            await completionTracker.OnTestCompletedAsync(state, cancellationToken);
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
}

internal class TestExecutionData
{
    public required ExecutableTest Test { get; init; }
    public required ExecutionContext? ExecutionContext { get; init; }
    public required List<string> Constraints { get; init; }
    public required Priority Priority { get; init; }
    public required TestExecutionState State { get; init; }
}