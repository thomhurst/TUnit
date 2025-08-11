using System.Buffers;
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
    
    // Add indices for faster dependency lookups
    private readonly ConcurrentDictionary<string, HashSet<AbstractExecutableTest>> _testsByMethodName = new();
    private readonly ConcurrentDictionary<Type, HashSet<AbstractExecutableTest>> _testsByType = new();

    private readonly ConcurrentDictionary<string, bool> _testsBeingResolved = new();

    public void RegisterTest(AbstractExecutableTest test)
    {
        _testsByName[test.TestId] = test;
        _allTests.Add(test);

        var className = test.Metadata.TestClassType.FullName ?? test.Metadata.TestClassType.Name;
        _testsByClassName.AddOrUpdate(className,
            _ => [test],
            (_, list) => { list.Add(test); return list; });
            
        // Add to method name index
        _testsByMethodName.AddOrUpdate(test.Metadata.TestMethodName,
            _ => new HashSet<AbstractExecutableTest> { test },
            (_, set) => { set.Add(test); return set; });
            
        // Add to type index
        _testsByType.AddOrUpdate(test.Metadata.TestClassType,
            _ => new HashSet<AbstractExecutableTest> { test },
            (_, set) => { set.Add(test); return set; });

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
            Console.WriteLine($"[DEBUG RESOLVE] Resolving dependencies for {test.TestId} (has {test.Metadata.Dependencies.Length} metadata dependencies)");
            
            var resolvedDependencies = new List<ResolvedDependency>();
            var allResolved = true;

            foreach (var dependency in test.Metadata.Dependencies)
            {
                List<AbstractExecutableTest> matchingTests;

                // Use indices for O(1) lookup instead of O(n) scan
                if (dependency.ClassType != null && string.IsNullOrEmpty(dependency.MethodName))
                {
                    // Class-level dependency
                    if (_testsByType.TryGetValue(dependency.ClassType, out var testsInType))
                    {
                        // Optimize: Manual filtering instead of LINQ Where().ToList()
                        matchingTests = new List<AbstractExecutableTest>(testsInType.Count);
                        foreach (var t in testsInType)
                        {
                            if (dependency.Matches(t.Metadata, test.Metadata))
                            {
                                matchingTests.Add(t);
                            }
                        }
                    }
                    else
                    {
                        var className = dependency.ClassType.FullName ?? dependency.ClassType.Name;
                        if (_testsByClassName.TryGetValue(className, out var testsInClass))
                        {
                            // Optimize: Manual filtering instead of LINQ Where().ToList()
                            matchingTests = new List<AbstractExecutableTest>(testsInClass.Count);
                            foreach (var t in testsInClass)
                            {
                                if (dependency.Matches(t.Metadata, test.Metadata))
                                {
                                    matchingTests.Add(t);
                                }
                            }
                        }
                        else
                        {
                            matchingTests = new List<AbstractExecutableTest>();
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(dependency.MethodName))
                {
                    // Method-level dependency - use method name index
                    if (dependency.MethodName != null && _testsByMethodName.TryGetValue(dependency.MethodName, out var testsWithMethod))
                    {
                        // Optimize: Manual filtering instead of LINQ Where().ToList()
                        matchingTests = new List<AbstractExecutableTest>(testsWithMethod.Count);
                        foreach (var t in testsWithMethod)
                        {
                            if (dependency.Matches(t.Metadata, test.Metadata))
                            {
                                matchingTests.Add(t);
                            }
                        }
                    }
                    else
                    {
                        matchingTests = new List<AbstractExecutableTest>();
                    }
                }
                else
                {
                    // Fallback for complex dependencies
                    // Optimize: Manual filtering instead of LINQ Where().ToList()
                    matchingTests = new List<AbstractExecutableTest>(_testsByName.Count);
                    foreach (var t in _testsByName.Values)
                    {
                        if (dependency.Matches(t.Metadata, test.Metadata))
                        {
                            matchingTests.Add(t);
                        }
                    }
                }

                if (matchingTests.Count == 0)
                {
                    var depKey = dependency.ToString();
                    Console.WriteLine($"[DEBUG RESOLVE] No matches found for dependency {depKey} of test {test.TestId}");
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
                    Console.WriteLine($"[DEBUG RESOLVE] Found {matchingTests.Count} matches for dependency {dependency} of test {test.TestId}");
                    foreach (var matchingTest in matchingTests)
                    {
                        Console.WriteLine($"[DEBUG RESOLVE]   - Adding dependency: {matchingTest.TestId}");
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
                // Optimize: Use dictionary for deduplication instead of LINQ GroupBy().Select().Where().ToList()
                var seenIds = new HashSet<string>();
                var distinctDeps = new List<ResolvedDependency>(resolvedDependencies.Count);
                
                foreach (var dep in resolvedDependencies)
                {
                    if (dep.Test.TestId != test.TestId && seenIds.Add(dep.Test.TestId))
                    {
                        distinctDeps.Add(dep);
                    }
                }

                const int MaxDirectDependencies = 1000;
                if (distinctDeps.Count > MaxDirectDependencies)
                {
                    // Optimize: Manual truncation instead of LINQ Take().ToList()
                    var truncated = new List<ResolvedDependency>(MaxDirectDependencies);
                    for (int i = 0; i < MaxDirectDependencies && i < distinctDeps.Count; i++)
                    {
                        truncated.Add(distinctDeps[i]);
                    }
                    distinctDeps = truncated;
                }

                test.Dependencies = distinctDeps.ToArray();
                Console.WriteLine($"[DEBUG RESOLVE] Successfully resolved {test.Dependencies.Length} dependencies for {test.TestId}");
            }
            else
            {
                Console.WriteLine($"[DEBUG RESOLVE] Failed to resolve all dependencies for {test.TestId}");
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
            // Optimize: Manual filtering instead of LINQ Where().ToList()
            var testsInCycles = new List<AbstractExecutableTest>(_allTests.Count);
            foreach (var test in _allTests)
            {
                if (inDegree[test.TestId] > 0)
                {
                    testsInCycles.Add(test);
                }
            }

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
                    // IMPORTANT: Set the TaskCompletionSource immediately so dependent tests don't deadlock
                    // This must be done here because the test may be referenced as a dependency
                    // before it's processed by the scheduler
                    test._taskCompletionSource.TrySetResult();
                }
            }
        }

        // Optimize: Manual filtering instead of LINQ Where()
        foreach (var test in _allTests)
        {
            if (test.State == TestState.Failed) continue;
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
            // Optimize: Use ArrayPool for temporary storage to reduce allocations
            var pathCount = path.Count;
            var pathArray = ArrayPool<AbstractExecutableTest>.Shared.Rent(pathCount);
            try
            {
                var index = 0;
                foreach (var item in path)
                {
                    pathArray[index++] = item;
                }
                
                // Reverse the array
                Array.Reverse(pathArray, 0, pathCount);
                
                var cycleStartIndex = -1;
                for (int i = 0; i < pathCount; i++)
                {
                    if (pathArray[i].TestId == test.TestId)
                    {
                        cycleStartIndex = i;
                        break;
                    }
                }
                
                var cycleTests = new List<AbstractExecutableTest>(pathCount - cycleStartIndex);
                for (int i = cycleStartIndex; i < pathCount; i++)
                {
                    cycleTests.Add(pathArray[i]);
                }
                
                // Create test details chain from cycle tests
                var testDetailsChain = new List<TestDetails>(cycleTests.Count);
                foreach (var cycleTest in cycleTests)
                {
                    testDetailsChain.Add(cycleTest.Context.TestDetails);
                }
                
                throw new DependencyConflictException(testDetailsChain);
            }
            finally
            {
                ArrayPool<AbstractExecutableTest>.Shared.Return(pathArray);
            }
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
