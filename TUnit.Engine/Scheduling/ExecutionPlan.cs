using System.Buffers;
using TUnit.Core;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Represents a complete execution plan for tests with execution order based on dependencies
/// </summary>
internal sealed class ExecutionPlan
{
    private readonly List<AbstractExecutableTest> _allTests;
    private readonly HashSet<AbstractExecutableTest> _executableTests;
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
        _dependentGraph = dependentGraph;
        _executionOrder = executionOrder;
    }

    public static ExecutionPlan Create(IEnumerable<AbstractExecutableTest> tests)
    {
        var allTests = tests.ToList();
        var testCount = allTests.Count;
        
        // Pre-size collections based on test count to reduce allocations
        var executableTests = new HashSet<AbstractExecutableTest>();
        var dependencyGraph = new Dictionary<AbstractExecutableTest, List<AbstractExecutableTest>>(testCount);
        var dependentGraph = new Dictionary<AbstractExecutableTest, List<AbstractExecutableTest>>(testCount);
        var executionOrder = new Dictionary<AbstractExecutableTest, int>(testCount);

        // Build dependency graphs
        foreach (var test in allTests)
        {
            // Pre-allocate list with known size to avoid resizing
            var dependencies = new List<AbstractExecutableTest>(test.Dependencies.Length);
            foreach (var resolvedDep in test.Dependencies)
            {
                dependencies.Add(resolvedDep.Test);
            }
            dependencyGraph[test] = dependencies;

            foreach (var resolvedDep in test.Dependencies)
            {
                var dependency = resolvedDep.Test;
                if (!dependentGraph.TryGetValue(dependency, out var dependents))
                {
                    dependents = new List<AbstractExecutableTest>();
                    dependentGraph[dependency] = dependents;
                }
                dependents.Add(test);
            }
        }

        // Detect circular dependencies first
        var circularDependencyTests = DetectCircularDependencies(allTests, dependencyGraph);
        
        // Mark tests with circular dependencies as failed
        foreach (var test in circularDependencyTests)
        {
            test.State = TestState.Failed;
            test.Result = new TestResult
            {
                State = TestState.Failed,
                Exception = new InvalidOperationException($"Test '{test.TestId}' has circular dependencies and cannot be executed"),
                ComputerName = Environment.MachineName,
                Start = DateTimeOffset.UtcNow,
                End = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero
            };
            
            // Complete the test's task to prevent waiting forever
            test.Context.InternalExecutableTest._taskCompletionSource?.TrySetResult();
        }

        // Create topological order for execution (only for non-circular tests)
        var visited = new HashSet<AbstractExecutableTest>();
        var topologicalOrder = new List<AbstractExecutableTest>(testCount);

        foreach (var test in allTests)
        {
            if (!visited.Contains(test) && !circularDependencyTests.Contains(test))
            {
                TopologicalSort(test, visited, topologicalOrder, dependencyGraph);
            }
        }

        // Assign execution order
        for (int i = 0; i < topologicalOrder.Count; i++)
        {
            executionOrder[topologicalOrder[i]] = i;
        }

        // Only non-circular tests are executable
        foreach (var test in allTests)
        {
            if (!circularDependencyTests.Contains(test))
            {
                executableTests.Add(test);
            }
        }

        return new ExecutionPlan(allTests, executableTests, dependencyGraph, dependentGraph, executionOrder);
    }

    private static HashSet<AbstractExecutableTest> DetectCircularDependencies(
        List<AbstractExecutableTest> allTests,
        Dictionary<AbstractExecutableTest, List<AbstractExecutableTest>> dependencyGraph)
    {
        var circularTests = new HashSet<AbstractExecutableTest>();
        var visitState = new Dictionary<AbstractExecutableTest, VisitState>();
        
        foreach (var test in allTests)
        {
            if (!visitState.ContainsKey(test))
            {
                var cycle = new List<AbstractExecutableTest>();
                if (HasCycle(test, dependencyGraph, visitState, cycle))
                {
                    // Add all tests in the cycle to the circular tests set
                    foreach (var cycleTest in cycle)
                    {
                        circularTests.Add(cycleTest);
                    }
                }
            }
        }
        
        return circularTests;
    }
    
    private static bool HasCycle(
        AbstractExecutableTest test,
        Dictionary<AbstractExecutableTest, List<AbstractExecutableTest>> dependencyGraph,
        Dictionary<AbstractExecutableTest, VisitState> visitState,
        List<AbstractExecutableTest> currentPath)
    {
        visitState[test] = VisitState.Visiting;
        currentPath.Add(test);
        
        if (dependencyGraph.TryGetValue(test, out var dependencies))
        {
            foreach (var dependency in dependencies)
            {
                if (!visitState.TryGetValue(dependency, out var state))
                {
                    // Not visited yet, recurse
                    if (HasCycle(dependency, dependencyGraph, visitState, currentPath))
                    {
                        return true;
                    }
                }
                else if (state == VisitState.Visiting)
                {
                    // Found a cycle - the dependency is in the current path
                    // Trim the path to only include the cycle
                    var cycleStartIndex = currentPath.IndexOf(dependency);
                    if (cycleStartIndex >= 0)
                    {
                        currentPath.RemoveRange(0, cycleStartIndex);
                    }
                    return true;
                }
                // If state == VisitState.Visited, this dependency was already fully processed
            }
        }
        
        visitState[test] = VisitState.Visited;
        currentPath.Remove(test);
        return false;
    }
    
    private enum VisitState
    {
        Visiting,
        Visited
    }

    private static void TopologicalSort(
        AbstractExecutableTest test,
        HashSet<AbstractExecutableTest> visited,
        List<AbstractExecutableTest> topologicalOrder,
        Dictionary<AbstractExecutableTest, List<AbstractExecutableTest>> dependencyGraph)
    {
        visited.Add(test);

        if (dependencyGraph.TryGetValue(test, out var dependencies))
        {
            foreach (var dependency in dependencies)
            {
                if (!visited.Contains(dependency))
                {
                    TopologicalSort(dependency, visited, topologicalOrder, dependencyGraph);
                }
            }
        }

        topologicalOrder.Add(test);
    }
}
