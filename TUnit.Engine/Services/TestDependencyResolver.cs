using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using TUnit.Core;

namespace TUnit.Engine.Services;

/// <summary>
/// Dependency resolver using integer IDs and pre-computed graphs for fast lookups
/// </summary>
internal sealed class TestDependencyResolver
{
    private readonly ConcurrentDictionary<string, int> _testIdToIndex = new();
    private readonly ConcurrentDictionary<int, AbstractExecutableTest> _indexToTest = new();
    private int _nextIndex;
    private readonly ConcurrentDictionary<int, HashSet<int>> _dependencyGraph = new();
    private readonly ConcurrentDictionary<int, List<ResolvedDependency>> _resolvedDependencies = new();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RegisterTest(AbstractExecutableTest test)
    {
        var index = Interlocked.Increment(ref _nextIndex);
        _testIdToIndex[test.TestId] = index;
        _indexToTest[index] = test;
        
        if (test.Metadata.Dependencies.Length > 0)
        {
#if NETSTANDARD2_0
            _dependencyGraph[index] = new HashSet<int>();
#else
            _dependencyGraph[index] = new HashSet<int>(test.Metadata.Dependencies.Length);
#endif
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryResolveDependencies(AbstractExecutableTest test)
    {
        if (!_testIdToIndex.TryGetValue(test.TestId, out var testIndex))
        {
            return false;
        }
        
        if (_resolvedDependencies.ContainsKey(testIndex))
        {
            var cached = _resolvedDependencies[testIndex];
            test.Dependencies = cached.ToArray();
            return true;
        }
        
        var dependencies = test.Metadata.Dependencies;
        if (dependencies.Length == 0)
        {
            test.Dependencies = [];
            return true;
        }
        
        var resolved = new List<ResolvedDependency>(dependencies.Length);
#if NETSTANDARD2_0
        var dependencyIndices = _dependencyGraph.GetOrAdd(testIndex, _ => new HashSet<int>());
#else
        var dependencyIndices = _dependencyGraph.GetOrAdd(testIndex, _ => new HashSet<int>(dependencies.Length));
#endif
        
        foreach (var dependency in dependencies)
        {
            if (TryResolveSingleDependency(dependency, out var resolvedDep, out var depIndex))
            {
                resolved.Add(resolvedDep);
                dependencyIndices.Add(depIndex);
            }
        }
        
        _resolvedDependencies[testIndex] = resolved;
        test.Dependencies = resolved.ToArray();
        
        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryResolveSingleDependency(TestDependency dependency, out ResolvedDependency resolvedDependency, out int dependencyIndex)
    {
        resolvedDependency = default!;
        dependencyIndex = -1;
        
        if (dependency.MethodName != null && dependency.ClassType != null)
        {
            foreach (var kvp in _indexToTest)
            {
                var candidateTest = kvp.Value;
                if (candidateTest.Metadata.TestMethodName == dependency.MethodName &&
                    candidateTest.Metadata.TestClassType == dependency.ClassType)
                {
                    dependencyIndex = kvp.Key;
                    resolvedDependency = new ResolvedDependency
                    {
                        Test = candidateTest,
                        Metadata = dependency
                    };
                    return true;
                }
            }
        }
        else if (dependency.MethodName != null)
        {
            foreach (var kvp in _indexToTest)
            {
                var candidateTest = kvp.Value;
                if (candidateTest.Metadata.TestMethodName == dependency.MethodName)
                {
                    dependencyIndex = kvp.Key;
                    resolvedDependency = new ResolvedDependency
                    {
                        Test = candidateTest,
                        Metadata = dependency
                    };
                    return true;
                }
            }
        }
        else if (dependency.ClassType != null)
        {
            foreach (var kvp in _indexToTest)
            {
                var candidateTest = kvp.Value;
                if (dependency.Matches(candidateTest.Metadata))
                {
                    dependencyIndex = kvp.Key;
                    resolvedDependency = new ResolvedDependency
                    {
                        Test = candidateTest,
                        Metadata = dependency
                    };
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Get the topological sort order for test execution
    /// </summary>
    public List<AbstractExecutableTest> GetTopologicalOrder(IEnumerable<AbstractExecutableTest> tests)
    {
        var testList = tests.ToList();
        var result = new List<AbstractExecutableTest>(testList.Count);
        
        // Build in-degree map
        var inDegree = new Dictionary<int, int>();
        var testIndices = new HashSet<int>();
        
        foreach (var test in testList)
        {
            if (_testIdToIndex.TryGetValue(test.TestId, out var index))
            {
                testIndices.Add(index);
                if (!inDegree.ContainsKey(index))
                {
                    inDegree[index] = 0;
                }
            }
        }
        
        // Calculate in-degrees
        foreach (var index in testIndices)
        {
            if (_dependencyGraph.TryGetValue(index, out var dependencies))
            {
                foreach (var depIndex in dependencies)
                {
                    if (testIndices.Contains(depIndex))
                    {
                        inDegree[depIndex] = inDegree.TryGetValue(depIndex, out var currentDegree) ? currentDegree + 1 : 1;
                    }
                }
            }
        }
        
        // Find all nodes with no incoming edges
        var queue = new Queue<int>();
        foreach (var kvp in inDegree)
        {
            if (kvp.Value == 0)
            {
                queue.Enqueue(kvp.Key);
            }
        }
        
        // Process the queue
        while (queue.Count > 0)
        {
            var index = queue.Dequeue();
            if (_indexToTest.TryGetValue(index, out var test))
            {
                result.Add(test);
                
                // Reduce in-degree for dependent tests
                if (_dependencyGraph.TryGetValue(index, out var dependencies))
                {
                    foreach (var depIndex in dependencies)
                    {
                        if (inDegree.ContainsKey(depIndex))
                        {
                            inDegree[depIndex]--;
                            if (inDegree[depIndex] == 0)
                            {
                                queue.Enqueue(depIndex);
                            }
                        }
                    }
                }
            }
        }
        
        // Add any remaining tests (in case of circular dependencies)
        foreach (var test in testList)
        {
            if (!result.Contains(test))
            {
                result.Add(test);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Detect circular dependencies efficiently
    /// </summary>
    public List<List<AbstractExecutableTest>> DetectCircularDependencies(IEnumerable<AbstractExecutableTest> tests)
    {
        var cycles = new List<List<AbstractExecutableTest>>();
        var visited = new HashSet<int>();
        var recursionStack = new HashSet<int>();
        var currentPath = new Stack<int>();
        
        foreach (var test in tests)
        {
            if (_testIdToIndex.TryGetValue(test.TestId, out var index))
            {
                if (!visited.Contains(index))
                {
                    DetectCyclesDFS(index, visited, recursionStack, currentPath, cycles);
                }
            }
        }
        
        return cycles;
    }
    
    private void DetectCyclesDFS(
        int index,
        HashSet<int> visited,
        HashSet<int> recursionStack,
        Stack<int> currentPath,
        List<List<AbstractExecutableTest>> cycles)
    {
        visited.Add(index);
        recursionStack.Add(index);
        currentPath.Push(index);
        
        if (_dependencyGraph.TryGetValue(index, out var dependencies))
        {
            foreach (var depIndex in dependencies)
            {
                if (!visited.Contains(depIndex))
                {
                    DetectCyclesDFS(depIndex, visited, recursionStack, currentPath, cycles);
                }
                else if (recursionStack.Contains(depIndex))
                {
                    // Found a cycle
                    var cycle = new List<AbstractExecutableTest>();
                    var foundStart = false;
                    
                    foreach (var pathIndex in currentPath.Reverse())
                    {
                        if (_indexToTest.TryGetValue(pathIndex, out var test))
                        {
                            cycle.Add(test);
                        }
                        
                        if (pathIndex == depIndex)
                        {
                            foundStart = true;
                        }
                        
                        if (foundStart && pathIndex == index)
                        {
                            break;
                        }
                    }
                    
                    if (cycle.Count > 0)
                    {
                        cycles.Add(cycle);
                    }
                }
            }
        }
        
        currentPath.Pop();
        recursionStack.Remove(index);
    }
}