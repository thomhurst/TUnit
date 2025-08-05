using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Exceptions;

namespace TUnit.Engine.Services;

/// <summary>
/// Resolves test dependencies on-demand during streaming
/// </summary>
internal sealed class TestDependencyResolver
{
    private readonly ConcurrentDictionary<string, AbstractExecutableTest> _testsByName = new();
    private readonly ConcurrentDictionary<string, List<string>> _pendingDependents = new();
    private readonly List<AbstractExecutableTest> _allTests = new();
    private readonly ConcurrentDictionary<string, List<TestDetails>> _cachedTransitiveDependencies = new();

    private readonly ConcurrentDictionary<string, List<AbstractExecutableTest>> _testsByClassName = new();

    private readonly ConcurrentDictionary<string, bool> _testsBeingResolved = new();

    public void RegisterTest(AbstractExecutableTest test)
    {
        _testsByName[test.TestId] = test;
        _allTests.Add(test);

        var className = test.Metadata.TestClassType.FullName ?? test.Metadata.TestClassType.Name;
        _testsByClassName.AddOrUpdate(className,
            _ => [test],
            (_, list) => { list.Add(test); return list; });

        if (_pendingDependents.TryRemove(test.TestId, out var dependents))
        {
            foreach (var dependentId in dependents)
            {
                if (_testsByName.TryGetValue(dependentId, out var dependent))
                {
                    ResolveDependenciesForTest(dependent);
                }
            }
        }

        var testClassName = test.Metadata.TestClassType.FullName ?? test.Metadata.TestClassType.Name;
        foreach (var kvp in _pendingDependents.ToList())
        {
            var depKey = kvp.Key;

            if (depKey.Contains($"Class={test.Metadata.TestClassType.Name}") &&
                depKey.Contains("Method=") == false)
            {
                if (_pendingDependents.TryRemove(depKey, out var waitingTests))
                {
                    foreach (var dependentId in waitingTests)
                    {
                        if (_testsByName.TryGetValue(dependentId, out var dependent))
                        {
                            ResolveDependenciesForTest(dependent);
                        }
                    }
                }
            }
        }
    }

    public bool TryResolveDependencies(AbstractExecutableTest test)
    {
        if (test.Dependencies.Length > 0)
        {
            return true;
        }

        return ResolveDependenciesForTest(test);
    }

    private bool ResolveDependenciesForTest(AbstractExecutableTest test)
    {
        if (!_testsBeingResolved.TryAdd(test.TestId, true))
        {
            return false;
        }

        try
        {

            var resolvedDependencies = new List<ResolvedDependency>();
            var allResolved = true;

            foreach (var dependency in test.Metadata.Dependencies)
            {
                List<AbstractExecutableTest> matchingTests;

                if (dependency.ClassType != null && string.IsNullOrEmpty(dependency.MethodName))
                {
                    var className = dependency.ClassType.FullName ?? dependency.ClassType.Name;
                    if (_testsByClassName.TryGetValue(className, out var testsInClass))
                    {
                        matchingTests = testsInClass
                            .Where(t => dependency.Matches(t.Metadata, test.Metadata))
                            .ToList();
                    }
                    else
                    {
                        matchingTests = new List<AbstractExecutableTest>();
                    }
                }
                else
                {
                    matchingTests = _testsByName.Values
                        .Where(t => dependency.Matches(t.Metadata, test.Metadata))
                        .ToList();
                }

                if (matchingTests.Count == 0)
                {
                    var depKey = dependency.ToString();
                    _pendingDependents.AddOrUpdate(depKey,
                        _ =>
                        [
                            test.TestId
                        ],
                        (_, list) => { list.Add(test.TestId); return list; });
                    allResolved = false;
                }
                else
                {
                    foreach (var matchingTest in matchingTests)
                    {
                        resolvedDependencies.Add(new ResolvedDependency
                        {
                            Test = matchingTest,
                            Metadata = dependency
                        });
                    }
                }
            }

            if (allResolved)
            {
                var distinctDeps = resolvedDependencies
                    .GroupBy(d => d.Test.TestId)
                    .Select(g => g.First())
                    .Where(d => d.Test.TestId != test.TestId)
                    .ToList();

                const int MaxDirectDependencies = 1000;
                if (distinctDeps.Count > MaxDirectDependencies)
                {
                    distinctDeps = distinctDeps.Take(MaxDirectDependencies).ToList();
                }

                test.Dependencies = distinctDeps.ToArray();

            }

            return allResolved;
        }
        finally
        {
            _testsBeingResolved.TryRemove(test.TestId, out _);
        }
    }


    public void CheckForCircularDependencies()
    {

        var inDegree = new Dictionary<string, int>();
        var adjacencyList = new Dictionary<string, List<string>>();

        foreach (var test in _allTests)
        {
            inDegree[test.TestId] = 0;
            adjacencyList[test.TestId] = new List<string>();
        }

        foreach (var test in _allTests)
        {
            foreach (var resolvedDep in test.Dependencies)
            {
                var dep = resolvedDep.Test;
                adjacencyList[dep.TestId].Add(test.TestId);
                inDegree[test.TestId]++;
            }
        }

        var queue = new Queue<string>();
        var sortedCount = 0;

        foreach (var kvp in inDegree)
        {
            if (kvp.Value == 0)
            {
                queue.Enqueue(kvp.Key);
            }
        }

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            sortedCount++;

            if (adjacencyList.TryGetValue(currentId, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    inDegree[neighbor]--;
                    if (inDegree[neighbor] == 0)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        if (sortedCount < _allTests.Count)
        {
            var testsInCycles = _allTests.Where(t => inDegree[t.TestId] > 0).ToList();

            foreach (var test in testsInCycles)
            {
                try
                {
                    CheckForCircularDependency(test, new HashSet<string>(), new Stack<AbstractExecutableTest>());
                }
                catch (DependencyConflictException ex)
                {
                    test.Result = new TestResult
                    {
                        State = TestState.Failed,
                        Start = DateTimeOffset.UtcNow,
                        End = DateTimeOffset.UtcNow,
                        Duration = TimeSpan.Zero,
                        Exception = ex,
                        ComputerName = Environment.MachineName,
                        TestContext = test.Context
                    };
                    test.State = TestState.Failed;
                }
            }
        }

        foreach (var test in _allTests.Where(t => t.State != TestState.Failed))
        {
            test.Context.Dependencies.Clear();

            if (_cachedTransitiveDependencies.TryGetValue(test.TestId, out var cachedDeps))
            {
                foreach (var dep in cachedDeps)
                {
                    test.Context.Dependencies.Add(dep);
                }
                continue;
            }

            var allDeps = new HashSet<TestDetails>();
            var depQueue = new Queue<AbstractExecutableTest>();
            var visited = new HashSet<string>();

            foreach (var resolvedDep in test.Dependencies)
            {
                var dep = resolvedDep.Test;
                depQueue.Enqueue(dep);
            }

            const int maxIterations = 10000;
            var iterations = 0;

            while (depQueue.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                var dep = depQueue.Dequeue();

                if (!visited.Add(dep.TestId))
                {
                    continue;
                }

                allDeps.Add(dep.Context.TestDetails);

                foreach (var resolvedSubDep in dep.Dependencies)
                {
                    var subDep = resolvedSubDep.Test;
                    depQueue.Enqueue(subDep);
                }
            }

            var depsList = allDeps.ToList();
            _cachedTransitiveDependencies[test.TestId] = depsList;

            foreach (var dep in depsList)
            {
                test.Context.Dependencies.Add(dep);
            }
        }
    }

    private void CheckForCircularDependency(
        AbstractExecutableTest test,
        HashSet<string> visited,
        Stack<AbstractExecutableTest> path)
    {
        path.Push(test);

        if (!visited.Add(test.TestId))
        {
            var pathList = path.Reverse().ToList();
            var cycleStartIndex = pathList.FindIndex(t => t.TestId == test.TestId);
            var cycleTests = pathList.Skip(cycleStartIndex).ToList();

            var testDetailsChain = cycleTests.Select(t => t.Context.TestDetails).ToList();
            throw new DependencyConflictException(testDetailsChain);
        }

        try
        {
            foreach (var resolvedDep in test.Dependencies)
            {
                var dep = resolvedDep.Test;
                CheckForCircularDependency(dep, visited, path);
            }
        }
        finally
        {
            visited.Remove(test.TestId);
            path.Pop();
        }
    }


    private List<TestDetails> BuildTestChain(List<string> cycle)
    {
        var testChain = new List<TestDetails>();
        foreach (var testId in cycle)
        {
            if (_testsByName.TryGetValue(testId, out var test))
            {
                testChain.Add(test.Context.TestDetails);
            }
        }
        return testChain;
    }
}
