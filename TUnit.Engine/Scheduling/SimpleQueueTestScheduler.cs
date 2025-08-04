using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Logging;
using TUnit.Core.Models;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;
using LoggingExtensions = TUnit.Core.Logging.LoggingExtensions;

namespace TUnit.Engine.Scheduling;

internal sealed class SimpleQueueTestScheduler : ITestScheduler
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestGroupingService _groupingService;
    private readonly SchedulerConfiguration _configuration;
    private readonly SimpleTestQueues _queues;
    private readonly SimpleConsumerManager _consumerManager;
    private readonly ExecutionContextManager _executionContextManager;
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;
    private readonly HookOrchestrator _hookOrchestrator;
    private readonly ConcurrentDictionary<string, int> _runningConstraintKeys = new();

    public SimpleQueueTestScheduler(
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
        
        _queues = new SimpleTestQueues(logger);
        _consumerManager = new SimpleConsumerManager(logger, configuration);
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

        var groupedTests = await _groupingService.GroupTestsByConstraintsAsync(testList);
        
        // Process each constraint group separately with proper sorting
        
        // 1. Parallel tests - sort by priority only (can run concurrently)
        var parallelTests = groupedTests.Parallel.ToList();
        parallelTests.Sort((a, b) => b.Context.ExecutionPriority.CompareTo(a.Context.ExecutionPriority));
        
        foreach (var test in parallelTests)
        {
            var state = new TestExecutionState(test);
            var testData = new TestExecutionData
            {
                Test = state.Test,
                ExecutionContext = ExecutionContext.Capture(),
                Constraints = GetConstraintKeys(state),
                Priority = state.Priority,
                State = state
            };
            _queues.EnqueueTest(testData);
        }
        
        // 2. Global NotInParallel tests - sort by Order first, then Priority
        var globalNotInParallelTests = new List<AbstractExecutableTest>();
        while (groupedTests.NotInParallel.TryDequeue(out var test, out var testPriority))
        {
            globalNotInParallelTests.Add(test);
        }
        
        globalNotInParallelTests.Sort((a, b) =>
        {
            var aOrder = a.Context.ParallelConstraint is NotInParallelConstraint nip1 ? nip1.Order : int.MaxValue / 2;
            var bOrder = b.Context.ParallelConstraint is NotInParallelConstraint nip2 ? nip2.Order : int.MaxValue / 2;
            
            var orderComparison = aOrder.CompareTo(bOrder);
            return orderComparison != 0 ? orderComparison : b.Context.ExecutionPriority.CompareTo(a.Context.ExecutionPriority);
        });
        
        foreach (var test in globalNotInParallelTests)
        {
            var state = new TestExecutionState(test);
            var testData = new TestExecutionData
            {
                Test = state.Test,
                ExecutionContext = ExecutionContext.Capture(),
                Constraints = GetConstraintKeys(state),
                Priority = state.Priority,
                State = state
            };
            _queues.EnqueueTest(testData);
        }
        
        // 3. Keyed NotInParallel tests - sort by Order first, then Priority within each key
        foreach (var kvp in groupedTests.KeyedNotInParallel)
        {
            var keyedTests = new List<AbstractExecutableTest>();
            while (kvp.Value.TryDequeue(out var test, out var testPriority))
            {
                keyedTests.Add(test);
            }
            
            keyedTests.Sort((a, b) =>
            {
                var aOrder = a.Context.ParallelConstraint is NotInParallelConstraint nip1 ? nip1.Order : int.MaxValue / 2;
                var bOrder = b.Context.ParallelConstraint is NotInParallelConstraint nip2 ? nip2.Order : int.MaxValue / 2;
                
                var orderComparison = aOrder.CompareTo(bOrder);
                return orderComparison != 0 ? orderComparison : b.Context.ExecutionPriority.CompareTo(a.Context.ExecutionPriority);
            });
            
            foreach (var test in keyedTests)
            {
                var state = new TestExecutionState(test);
                var testData = new TestExecutionData
                {
                    Test = state.Test,
                    ExecutionContext = ExecutionContext.Capture(),
                    Constraints = GetConstraintKeys(state),
                    Priority = state.Priority,
                    State = state
                };
                _queues.EnqueueTest(testData);
            }
        }
        
        // 4. Parallel group tests - preserve existing order logic (already sorted by the grouping service)
        foreach (var group in groupedTests.ParallelGroups)
        {
            foreach (var orderGroup in group.Value)
            {
                foreach (var test in orderGroup.Value)
                {
                    var state = new TestExecutionState(test);
                    var testData = new TestExecutionData
                    {
                        Test = state.Test,
                        ExecutionContext = ExecutionContext.Capture(),
                        Constraints = GetConstraintKeys(state),
                        Priority = state.Priority,
                        State = state
                    };
                    _queues.EnqueueTest(testData);
                }
            }
        }
        
        await _logger.LogInformationAsync($"Enqueued {_queues.GetTotalQueuedCount()} tests in queues");
        
        // Start consumers to process tests from queues
        await _consumerManager.StartConsumersAsync(
            _queues,
            _runningConstraintKeys,
            async (test, token) => await ExecuteTestWithContextAsync(test, executor, token),
            cancellationToken);
        
        await _logger.LogInformationAsync("Test execution completed");
    }

    private async Task ExecuteTestWithContextAsync(
        TestExecutionData testData,
        ITestExecutor executor,
        CancellationToken cancellationToken)
    {
        var state = testData.State;
        
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
}