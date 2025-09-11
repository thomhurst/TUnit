using EnumerableAsyncProcessor.Extensions;
using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Logging;
using TUnit.Core.Models;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;
using TUnit.Engine.Services;

namespace TUnit.Engine.Scheduling;

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
        TestRunner runner,
        CancellationToken cancellationToken)
    {
        if (tests == null) throw new ArgumentNullException(nameof(tests));
        if (runner == null) throw new ArgumentNullException(nameof(runner));

        var testList = tests as IList<AbstractExecutableTest> ?? tests.ToList();
        if (testList.Count == 0)
        {
            await _logger.LogDebugAsync("No executable tests found").ConfigureAwait(false);
            return;
        }

        await _logger.LogDebugAsync($"Scheduling execution of {testList.Count} tests").ConfigureAwait(false);

        var circularDependencies = await DetectCircularDependenciesAsync(testList).ConfigureAwait(false);

        foreach (var (test, dependencies) in circularDependencies)
        {
            test.State = TestState.Failed;
            test.Result = new TestResult
            {
                State = TestState.Failed,
                Exception = new CircularDependencyException($"Circular dependency detected: {string.Join(" -> ", dependencies.Select(d => d.TestId))}"),
                ComputerName = Environment.MachineName,
                Start = DateTimeOffset.UtcNow,
                End = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero
            };

            await _messageBus.Failed(test.Context, test.Result.Exception, DateTimeOffset.UtcNow).ConfigureAwait(false);
        }

        var executableTests = testList.Where(t => !circularDependencies.Any(cd => cd.test == t)).ToList();
        if (executableTests.Count == 0)
        {
            await _logger.LogDebugAsync("No executable tests found after removing circular dependencies").ConfigureAwait(false);
            return;
        }

        // Simply execute all tests - TestRunner handles dependencies and prevents double execution
        var testTasks = executableTests.Select(test =>
        {
            // Set ExecutionTask for dependency tracking
            test.ExecutionTask = runner.ExecuteTestAsync(test, cancellationToken);
            return test.ExecutionTask;
        }).ToArray();

        await Task.WhenAll(testTasks).ConfigureAwait(false);
    }

    private async Task<List<(AbstractExecutableTest test, List<AbstractExecutableTest> dependencies)>> DetectCircularDependenciesAsync(
        IEnumerable<AbstractExecutableTest> tests)
    {
        var testList = tests.ToList();
        var circularDependencies = new List<(AbstractExecutableTest test, List<AbstractExecutableTest> dependencies)>();
        
        // Use DFS to detect cycles in the dependency graph
        var visitedStates = new Dictionary<string, VisitState>();
        
        foreach (var test in testList)
        {
            if (visitedStates.ContainsKey(test.TestId))
                continue;
                
            var path = new List<AbstractExecutableTest>();
            if (HasCycleDfs(test, testList, visitedStates, path))
            {
                // Found a cycle - add all tests in the cycle to circular dependencies
                var cycle = new List<AbstractExecutableTest>(path);
                circularDependencies.Add((test, cycle));
            }
        }
        
        return await Task.FromResult(circularDependencies).ConfigureAwait(false);
    }
    
    private enum VisitState
    {
        Unvisited,
        Visiting,
        Visited
    }
    
    private bool HasCycleDfs(AbstractExecutableTest test, List<AbstractExecutableTest> allTests, 
        Dictionary<string, VisitState> visitedStates, List<AbstractExecutableTest> currentPath)
    {
        if (visitedStates.TryGetValue(test.TestId, out var state))
        {
            if (state == VisitState.Visiting)
            {
                // Found a cycle - the current path contains the cycle
                return true;
            }
            if (state == VisitState.Visited)
            {
                // Already processed, no cycle through this path
                return false;
            }
        }
        
        // Mark as visiting and add to current path
        visitedStates[test.TestId] = VisitState.Visiting;
        currentPath.Add(test);
        
        // Check all dependencies
        foreach (var dependency in test.Dependencies)
        {
            if (HasCycleDfs(dependency.Test, allTests, visitedStates, currentPath))
            {
                return true;
            }
        }
        
        // Mark as visited and remove from current path
        visitedStates[test.TestId] = VisitState.Visited;
        currentPath.RemoveAt(currentPath.Count - 1);
        
        return false;
    }
}
