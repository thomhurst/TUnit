using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Enums;
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
    
    // Index for efficient dependency matching
    private readonly ConcurrentDictionary<string, List<AbstractExecutableTest>> _testsByClassName = new();
    
    // Track tests currently being resolved to prevent infinite loops
    private readonly ConcurrentDictionary<string, bool> _testsBeingResolved = new();

    public void RegisterTest(AbstractExecutableTest test)
    {
        _testsByName[test.TestId] = test;
        _allTests.Add(test);
        
        // Add to class name index for efficient matching
        var className = test.Metadata.TestClassType.FullName ?? test.Metadata.TestClassType.Name;
        _testsByClassName.AddOrUpdate(className,
            _ => [test],
            (_, list) => { list.Add(test); return list; });

        // Check if any tests are waiting for this specific test
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
        
        // Also check if any tests are waiting for this test's class
        // This handles class-level dependencies
        var testClassName = test.Metadata.TestClassType.FullName ?? test.Metadata.TestClassType.Name;
        foreach (var kvp in _pendingDependents.ToList())
        {
            var depKey = kvp.Key;
            
            // Simple check if this is a class-level dependency for this test's class
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
            return true; // Already resolved
        }

        return ResolveDependenciesForTest(test);
    }

    private bool ResolveDependenciesForTest(AbstractExecutableTest test)
    {
        // Prevent recursive resolution
        if (!_testsBeingResolved.TryAdd(test.TestId, true))
        {
            return false; // Already being resolved
        }
        
        try
        {
            
            var dependencies = new List<AbstractExecutableTest>();
            var allResolved = true;

            foreach (var dependency in test.Metadata.Dependencies)
            {
                List<AbstractExecutableTest> matchingTests;
                
                // Optimize class-level dependency matching
                if (dependency.ClassType != null && string.IsNullOrEmpty(dependency.MethodName))
                {
                    // For class-level dependencies, use the class name index
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
                    // For other dependencies, fall back to full search
                    // but limit the search space if possible
                    matchingTests = _testsByName.Values
                        .Where(t => dependency.Matches(t.Metadata, test.Metadata))
                        .ToList();
                }

                if (matchingTests.Count == 0)
                {
                    // Dependency not yet discovered, register for notification
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
                    dependencies.AddRange(matchingTests);
                }
            }

            if (allResolved)
            {
                var distinctDeps = dependencies
                    .Distinct()
                    .Where(d => d.TestId != test.TestId)
                    .ToList();
                
                // Safeguard: Limit the number of direct dependencies to prevent memory issues
                const int MaxDirectDependencies = 1000;
                if (distinctDeps.Count > MaxDirectDependencies)
                {
                    distinctDeps = distinctDeps.Take(MaxDirectDependencies).ToList();
                }
                
                test.Dependencies = distinctDeps.ToArray();

                // Skip updating TestContext.Dependencies for now - will be done after circular dependency check
                // to avoid infinite loops
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
        
        // Use a more efficient algorithm for large graphs
        // Instead of checking every test, we'll use a topological sort approach
        var inDegree = new Dictionary<string, int>();
        var adjacencyList = new Dictionary<string, List<string>>();
        
        // Build the graph
        foreach (var test in _allTests)
        {
            inDegree[test.TestId] = 0;
            adjacencyList[test.TestId] = new List<string>();
        }
        
        foreach (var test in _allTests)
        {
            foreach (var dep in test.Dependencies)
            {
                adjacencyList[dep.TestId].Add(test.TestId);
                inDegree[test.TestId]++;
            }
        }
        
        // Perform topological sort using Kahn's algorithm
        var queue = new Queue<string>();
        var sortedCount = 0;
        
        // Find all nodes with no incoming edges
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
            
            // Process all neighbors
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
        
        // If we couldn't sort all tests, there's a cycle
        if (sortedCount < _allTests.Count)
        {
            // Find tests that are part of cycles (those with non-zero in-degree)
            var testsInCycles = _allTests.Where(t => inDegree[t.TestId] > 0).ToList();
            
            // Only check these specific tests for circular dependencies
            foreach (var test in testsInCycles)
            {
                try
                {
                    CheckForCircularDependency(test, new HashSet<string>(), new Stack<AbstractExecutableTest>());
                }
                catch (DependencyConflictException ex)
                {
                    // Mark this test as failed due to circular dependency
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
                    
                    // Log the circular dependency for debugging
                    Console.WriteLine($"[DEBUG] Circular dependency detected for test: {test.Context.GetDisplayName()} - {ex.Message}");
                }
            }
        }

        // Second pass: Build transitive dependencies for non-failed tests
        // We need to populate TestContext.Dependencies with all transitive dependencies
        // for backward compatibility. Use an efficient approach with memoization.
        
        foreach (var test in _allTests.Where(t => t.State != TestState.Failed))
        {
            test.Context.Dependencies.Clear();
            
            // Check if we already have cached dependencies
            if (_cachedTransitiveDependencies.TryGetValue(test.TestId, out var cachedDeps))
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
            foreach (var dep in test.Dependencies)
            {
                depQueue.Enqueue(dep);
            }
            
            // Process queue to get all transitive dependencies
            // Limit iterations to prevent infinite loops
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
                foreach (var subDep in dep.Dependencies)
                {
                    depQueue.Enqueue(subDep);
                }
            }
            
            // Cache the result
            var depsList = allDeps.ToList();
            _cachedTransitiveDependencies[test.TestId] = depsList;
            
            // Add to context
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
        // Add current test to the path
        path.Push(test);

        // Add the current test to the visited set to detect cycles
        if (!visited.Add(test.TestId))
        {
            // We've encountered this test before in our current path - circular dependency
            // Build the dependency chain for the error message
            var pathList = path.Reverse().ToList();
            var cycleStartIndex = pathList.FindIndex(t => t.TestId == test.TestId);
            var cycleTests = pathList.Skip(cycleStartIndex).ToList();

            // Convert to TestDetails for the exception
            var testDetailsChain = cycleTests.Select(t => t.Context.TestDetails).ToList();
            throw new DependencyConflictException(testDetailsChain);
        }

        try
        {
            // Check each dependency
            foreach (var dep in test.Dependencies)
            {
                CheckForCircularDependency(dep, visited, path);
            }
        }
        finally
        {
            // Remove from visited set and path when we're done with this test
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
