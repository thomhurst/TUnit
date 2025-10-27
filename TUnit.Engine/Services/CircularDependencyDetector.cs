using TUnit.Core;
using TUnit.Core.Exceptions;

namespace TUnit.Engine.Services;

/// <summary>
/// Detects circular dependencies in test execution chains using depth-first search
/// </summary>
internal sealed class CircularDependencyDetector
{
    /// <summary>
    /// Detects circular dependencies in the given collection of tests
    /// </summary>
    /// <param name="tests">Tests to analyze for circular dependencies</param>
    /// <returns>List of tests with circular dependencies and their dependency chains</returns>
    public List<(AbstractExecutableTest Test, List<AbstractExecutableTest> DependencyChain)> DetectCircularDependencies(
        IEnumerable<AbstractExecutableTest> tests)
    {
        var testList = tests as IList<AbstractExecutableTest> ?? tests.ToList();
        var circularDependencies = new List<(AbstractExecutableTest Test, List<AbstractExecutableTest> DependencyChain)>();
        var visitedStates = new Dictionary<string, VisitState>(capacity: testList.Count);

        foreach (var test in testList)
        {
            if (visitedStates.ContainsKey(test.TestId))
            {
                continue;
            }

            // Typical cycle depth is small (2-5 tests), pre-size to 4
            var path = new List<AbstractExecutableTest>(4);
            if (HasCycleDfs(test, testList, visitedStates, path))
            {
                // Found a cycle - add all tests in the cycle to circular dependencies
                var cycle = new List<AbstractExecutableTest>(path);
                circularDependencies.Add((test, cycle));
            }
        }

        return circularDependencies;
    }

    private enum VisitState
    {
        Unvisited,
        Visiting,
        Visited
    }

    private bool HasCycleDfs(
        AbstractExecutableTest test,
        IList<AbstractExecutableTest> allTests,
        Dictionary<string, VisitState> visitedStates,
        List<AbstractExecutableTest> currentPath)
    {
        if (visitedStates.TryGetValue(test.TestId, out var state))
        {
            if (state == VisitState.Visiting)
            {
                // Found a cycle - add the current test to complete the cycle
                currentPath.Add(test);
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