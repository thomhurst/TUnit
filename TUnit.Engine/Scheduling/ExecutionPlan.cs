using TUnit.Core;
using TUnit.Core.Exceptions;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Represents a complete execution plan for tests with all dependencies resolved upfront
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
            dependencyGraph[test] = test.Dependencies.Select(rd => rd.Test).ToList();

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

        // Populate TestContext.Dependencies with transitive dependencies
        PopulateTransitiveDependencies(allTests);

        return new ExecutionPlan(allTests, executableTests, dependencyGraph, dependentGraph, executionOrder);
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
                current = dependencies.FirstOrDefault(recursionStack.Contains);
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


    private static void PopulateTransitiveDependencies(List<AbstractExecutableTest> allTests)
    {
        var cachedTransitiveDependencies = new Dictionary<string, List<TestDetails>>();

        foreach (var test in allTests.Where(t => t.State != TestState.Failed))
        {
            test.Context.Dependencies.Clear();

            // Check if we already have cached dependencies
            if (cachedTransitiveDependencies.TryGetValue(test.TestId, out var cachedDeps))
            {
                foreach (var dep in cachedDeps)
                {
                    test.Context.Dependencies.Add(dep);
                }
                continue;
            }

            // Build transitive dependencies using BFS to avoid stack overflow
            var allDeps = new HashSet<TestDetails>();
            var depQueue = new Queue<AbstractExecutableTest>();
            var visited = new HashSet<string>();

            // Add direct dependencies to queue
            foreach (var resolvedDep in test.Dependencies)
            {
                var dep = resolvedDep.Test;
                depQueue.Enqueue(dep);
            }

            // Process queue to get all transitive dependencies
            const int maxIterations = 10000;
            var iterations = 0;

            while (depQueue.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                var dep = depQueue.Dequeue();

                // Skip if we've already processed this test
                if (!visited.Add(dep.TestId))
                {
                    continue;
                }

                allDeps.Add(dep.Context.TestDetails);

                // Add this test's dependencies to the queue
                foreach (var resolvedSubDep in dep.Dependencies)
                {
                    var subDep = resolvedSubDep.Test;
                    depQueue.Enqueue(subDep);
                }
            }

            // Cache the result
            var depsList = allDeps.ToList();
            cachedTransitiveDependencies[test.TestId] = depsList;

            // Add to context
            foreach (var dep in depsList)
            {
                test.Context.Dependencies.Add(dep);
            }
        }
    }
}
