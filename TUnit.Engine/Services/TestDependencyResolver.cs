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
                
            // Skip updating TestContext.Dependencies for now - will be done after circular dependency check
            // to avoid infinite loops
        }
        
        return allResolved;
    }
    
    private IEnumerable<AbstractExecutableTest> GetAllDependencies(
        AbstractExecutableTest test, 
        HashSet<string> visited)
    {
        // Add the current test to the visited set to detect cycles
        if (!visited.Add(test.TestId))
        {
            // We've encountered this test before in our current path - circular dependency
            throw new InvalidOperationException($"Circular dependency detected involving test {test.TestId}");
        }
        
        try
        {
            foreach (var dep in test.Dependencies)
            {
                yield return dep;
                
                // Recursively get dependencies of this dependency
                foreach (var transitive in GetAllDependencies(dep, visited))
                {
                    yield return transitive;
                }
            }
        }
        finally
        {
            // Remove from visited set when we're done with this path
            visited.Remove(test.TestId);
        }
    }
    
    public void CheckForCircularDependencies()
    {
        Console.WriteLine($"[DEBUG] Checking for circular dependencies and updating TestContext.Dependencies for {_allTests.Count} tests");
        
        // Update TestContext.Dependencies for all tests, with circular dependency detection built-in
        foreach (var test in _allTests)
        {
            test.Context.Dependencies.Clear();
            var visited = new HashSet<string>();
            
            try
            {
                foreach (var dep in GetAllDependencies(test, visited))
                {
                    test.Context.Dependencies.Add(dep.Context.TestDetails);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Error getting dependencies for test {test.TestId}: {ex.Message}");
                // Mark this test as failed if we can't resolve its dependencies
                test.Result = new TestResult
                {
                    State = TestState.Failed,
                    Start = DateTimeOffset.UtcNow,
                    End = DateTimeOffset.UtcNow,
                    Duration = TimeSpan.Zero,
                    Exception = new InvalidOperationException($"Failed to resolve dependencies: {ex.Message}"),
                    ComputerName = Environment.MachineName,
                    TestContext = test.Context
                };
                test.State = TestState.Failed;
            }
        }
        
        // Log how many tests failed due to circular dependencies
        var failedTests = _allTests.Where(t => t.State == TestState.Failed).ToList();
        if (failedTests.Any())
        {
            Console.WriteLine($"[DEBUG] {failedTests.Count} tests marked as failed after circular dependency check");
            foreach (var test in failedTests.Take(5))
            {
                Console.WriteLine($"[DEBUG] Failed test: {test.Context.TestName}");
            }
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