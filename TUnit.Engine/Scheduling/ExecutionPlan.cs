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

        // Create topological order for execution
        var visited = new HashSet<AbstractExecutableTest>();
        var topologicalOrder = new List<AbstractExecutableTest>(testCount);

        foreach (var test in allTests)
        {
            if (!visited.Contains(test))
            {
                TopologicalSort(test, visited, topologicalOrder, dependencyGraph);
            }
        }

        // Assign execution order
        for (int i = 0; i < topologicalOrder.Count; i++)
        {
            executionOrder[topologicalOrder[i]] = i;
        }

        // All tests are executable (pre-failed tests will be handled by scheduler)
        foreach (var test in allTests)
        {
            executableTests.Add(test);
        }

        return new ExecutionPlan(allTests, executableTests, dependencyGraph, dependentGraph, executionOrder);
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
