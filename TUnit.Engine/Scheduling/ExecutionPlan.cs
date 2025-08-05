using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Exceptions;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Represents a complete execution plan for tests with all dependencies resolved upfront
/// </summary>
internal sealed class ExecutionPlan
{
    private readonly List<AbstractExecutableTest> _allTests;
    private readonly HashSet<AbstractExecutableTest> _executableTests;
    private readonly Dictionary<AbstractExecutableTest, List<AbstractExecutableTest>> _dependencyGraph;
    private readonly Dictionary<AbstractExecutableTest, List<AbstractExecutableTest>> _dependentGraph;
    private readonly Dictionary<AbstractExecutableTest, int> _executionOrder;
    
    public IReadOnlyList<AbstractExecutableTest> AllTests => _allTests;
    public IReadOnlyCollection<AbstractExecutableTest> ExecutableTests => _executableTests;
    public IReadOnlyDictionary<AbstractExecutableTest, int> ExecutionOrder => _executionOrder;

    private ExecutionPlan(
        List<AbstractExecutableTest> allTests,
        HashSet<AbstractExecutableTest> executableTests,
        Dictionary<AbstractExecutableTest, List<AbstractExecutableTest>> dependencyGraph,
        Dictionary<AbstractExecutableTest, List<AbstractExecutableTest>> dependentGraph,
        Dictionary<AbstractExecutableTest, int> executionOrder)
    {
        _allTests = allTests;
        _executableTests = executableTests;
        _dependencyGraph = dependencyGraph;
        _dependentGraph = dependentGraph;
        _executionOrder = executionOrder;
    }

    /// <summary>
    /// Creates an execution plan from a list of tests, resolving all dependencies upfront
    /// </summary>
    public static ExecutionPlan Create(IEnumerable<AbstractExecutableTest> tests)
    {
        var allTests = tests.ToList();
        var executableTests = new HashSet<AbstractExecutableTest>();
        var dependencyGraph = new Dictionary<AbstractExecutableTest, List<AbstractExecutableTest>>();
        var dependentGraph = new Dictionary<AbstractExecutableTest, List<AbstractExecutableTest>>();
        var executionOrder = new Dictionary<AbstractExecutableTest, int>();
        
        // Build dependency graphs
        foreach (var test in allTests)
        {
            dependencyGraph[test] = new List<AbstractExecutableTest>(test.Dependencies);
            
            foreach (var dependency in test.Dependencies)
            {
                if (!dependentGraph.TryGetValue(dependency, out var dependents))
                {
                    dependents = new List<AbstractExecutableTest>();
                    dependentGraph[dependency] = dependents;
                }
                dependents.Add(test);
            }
        }
        
        // Detect circular dependencies using topological sort
        var visited = new HashSet<AbstractExecutableTest>();
        var recursionStack = new HashSet<AbstractExecutableTest>();
        var topologicalOrder = new List<AbstractExecutableTest>();
        
        foreach (var test in allTests)
        {
            if (!visited.Contains(test))
            {
                if (!TopologicalSort(test, visited, recursionStack, topologicalOrder, dependencyGraph))
                {
                    // Circular dependency detected - mark all tests in the cycle as failed
                    MarkCircularDependencyChain(test, recursionStack, dependencyGraph);
                }
            }
        }
        
        // Assign execution order based on topological sort
        for (int i = 0; i < topologicalOrder.Count; i++)
        {
            executionOrder[topologicalOrder[i]] = i;
        }
        
        // Determine which tests are executable
        foreach (var test in allTests)
        {
            if (test.State != TestState.Failed && test.State != TestState.Skipped)
            {
                executableTests.Add(test);
            }
        }
        
        return new ExecutionPlan(allTests, executableTests, dependencyGraph, dependentGraph, executionOrder);
    }

    /// <summary>
    /// Checks if a test can be executed based on its dependencies
    /// </summary>
    public bool CanExecute(AbstractExecutableTest test)
    {
        if (!_executableTests.Contains(test))
        {
            return false;
        }
        
        // Check if all dependencies have completed
        foreach (var dependency in test.Dependencies)
        {
            if (dependency.State != TestState.Passed && 
                dependency.State != TestState.Failed && 
                dependency.State != TestState.Skipped)
            {
                return false;
            }
            
            // Check if dependency failed and we should skip
            if (dependency.State == TestState.Failed)
            {
                var dependencyMeta = FindDependencyMetadata(test, dependency);
                if (dependencyMeta?.ProceedOnFailure != true)
                {
                    // Mark this test as skipped
                    SkipTestDueToDependency(test, dependency);
                    return false;
                }
            }
        }
        
        return true;
    }

    /// <summary>
    /// Gets all tests that depend on the given test
    /// </summary>
    public IEnumerable<AbstractExecutableTest> GetDependents(AbstractExecutableTest test)
    {
        return _dependentGraph.TryGetValue(test, out var dependents) 
            ? dependents 
            : Enumerable.Empty<AbstractExecutableTest>();
    }

    private static bool TopologicalSort(
        AbstractExecutableTest test,
        HashSet<AbstractExecutableTest> visited,
        HashSet<AbstractExecutableTest> recursionStack,
        List<AbstractExecutableTest> topologicalOrder,
        Dictionary<AbstractExecutableTest, List<AbstractExecutableTest>> dependencyGraph)
    {
        visited.Add(test);
        recursionStack.Add(test);
        
        if (dependencyGraph.TryGetValue(test, out var dependencies))
        {
            foreach (var dependency in dependencies)
            {
                if (!visited.Contains(dependency))
                {
                    if (!TopologicalSort(dependency, visited, recursionStack, topologicalOrder, dependencyGraph))
                    {
                        return false;
                    }
                }
                else if (recursionStack.Contains(dependency))
                {
                    // Circular dependency detected
                    return false;
                }
            }
        }
        
        recursionStack.Remove(test);
        topologicalOrder.Add(test);
        return true;
    }

    private static void MarkCircularDependencyChain(
        AbstractExecutableTest test,
        HashSet<AbstractExecutableTest> recursionStack,
        Dictionary<AbstractExecutableTest, List<AbstractExecutableTest>> dependencyGraph)
    {
        var testsInCycle = new List<AbstractExecutableTest>();
        var current = test;
        var visited = new HashSet<AbstractExecutableTest>();
        
        // Find all tests in the circular dependency
        while (current != null && visited.Add(current))
        {
            testsInCycle.Add(current);
            
            if (dependencyGraph.TryGetValue(current, out var dependencies))
            {
                current = dependencies.FirstOrDefault(d => recursionStack.Contains(d));
            }
            else
            {
                current = null;
            }
        }
        
        // Mark all tests in the cycle as failed
        var exception = new DependencyConflictException(testsInCycle.Select(t => t.Context.TestDetails).ToList());
        
        foreach (var testInCycle in testsInCycle)
        {
            testInCycle.State = TestState.Failed;
            testInCycle.Result = new TestResult
            {
                State = TestState.Failed,
                Start = DateTimeOffset.UtcNow,
                End = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero,
                Exception = exception,
                ComputerName = Environment.MachineName,
                TestContext = testInCycle.Context
            };
        }
    }

    private static void SkipTestDueToDependency(AbstractExecutableTest test, AbstractExecutableTest failedDependency)
    {
        test.State = TestState.Skipped;
        test.Context.SkipReason = $"Dependency '{failedDependency.Context.GetDisplayName()}' failed";
        test.Result = new TestResult
        {
            State = TestState.Skipped,
            Start = DateTimeOffset.UtcNow,
            End = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            OverrideReason = test.Context.SkipReason,
            Exception = null,
            ComputerName = Environment.MachineName,
            TestContext = test.Context
        };
    }

    private static TestDependency? FindDependencyMetadata(AbstractExecutableTest test, AbstractExecutableTest dependency)
    {
        foreach (var meta in test.Metadata.Dependencies)
        {
            if (meta.Matches(dependency.Metadata, test.Metadata))
            {
                return meta;
            }
        }
        return null;
    }
}