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
    
    public void RegisterTest(AbstractExecutableTest test)
    {
        _testsByName[test.TestId] = test;
        _allTests.Add(test);
        
        // Process any tests waiting for this one
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
        var dependencies = new List<AbstractExecutableTest>();
        var allResolved = true;
        
        foreach (var dependency in test.Metadata.Dependencies)
        {
            var matchingTests = _testsByName.Values
                .Where(t => dependency.Matches(t.Metadata, test.Metadata))
                .ToList();
                
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
            test.Dependencies = dependencies
                .Distinct()
                .Where(d => d.TestId != test.TestId)
                .ToArray();
                
            // Update TestContext.Dependencies
            test.Context.Dependencies.Clear();
            foreach (var dep in GetAllDependencies(test, [
                     ]))
            {
                test.Context.Dependencies.Add(dep.Context.TestDetails);
            }
        }
        
        return allResolved;
    }
    
    private IEnumerable<AbstractExecutableTest> GetAllDependencies(
        AbstractExecutableTest test, 
        HashSet<string> visited)
    {
        foreach (var dep in test.Dependencies)
        {
            if (visited.Add(dep.TestId))
            {
                yield return dep;
                foreach (var transitive in GetAllDependencies(dep, visited))
                {
                    yield return transitive;
                }
            }
        }
    }
    
    public void CheckForCircularDependencies()
    {
        var circularDependencies = DetectCircularDependencies();
        if (circularDependencies.Count > 0)
        {
            // Mark all tests involved in circular dependencies as failed
            foreach (var cycle in circularDependencies)
            {
                Console.WriteLine($"[DEBUG] Found circular dependency cycle: {string.Join(" -> ", cycle)}");
                foreach (var testId in cycle)
                {
                    if (_testsByName.TryGetValue(testId, out var test))
                    {
                        var testChain = BuildTestChain(cycle);
                        var exception = new DependencyConflictException(testChain);
                        
                        // Create a failed test result
                        test.Result = new TestResult
                        {
                            State = TestState.Failed,
                            Start = DateTimeOffset.UtcNow,
                            End = DateTimeOffset.UtcNow,
                            Duration = TimeSpan.Zero,
                            Exception = exception,
                            ComputerName = Environment.MachineName,
                            TestContext = test.Context
                        };
                        
                        test.State = TestState.Failed;
                    }
                }
            }
        }
    }
    
    private List<List<string>> DetectCircularDependencies()
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var cycles = new List<List<string>>();
        var currentPath = new List<string>();
        
        foreach (var test in _allTests)
        {
            if (!visited.Contains(test.TestId))
            {
                DetectCyclesRecursive(test.TestId, visited, recursionStack, currentPath, cycles);
            }
        }
        
        return cycles;
    }
    
    private bool DetectCyclesRecursive(
        string testId,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        List<string> currentPath,
        List<List<string>> cycles)
    {
        visited.Add(testId);
        recursionStack.Add(testId);
        currentPath.Add(testId);
        
        if (_testsByName.TryGetValue(testId, out var test))
        {
            foreach (var dependency in test.Dependencies)
            {
                if (!visited.Contains(dependency.TestId))
                {
                    if (DetectCyclesRecursive(dependency.TestId, visited, recursionStack, currentPath, cycles))
                    {
                        return true;
                    }
                }
                else if (recursionStack.Contains(dependency.TestId))
                {
                    // Found a cycle - extract the cycle from currentPath
                    var cycleStartIndex = currentPath.IndexOf(dependency.TestId);
                    var cycle = new List<string>();
                    for (int i = cycleStartIndex; i < currentPath.Count; i++)
                    {
                        cycle.Add(currentPath[i]);
                    }
                    cycle.Add(dependency.TestId); // Complete the cycle
                    cycles.Add(cycle);
                }
            }
        }
        
        recursionStack.Remove(testId);
        currentPath.RemoveAt(currentPath.Count - 1);
        return false;
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