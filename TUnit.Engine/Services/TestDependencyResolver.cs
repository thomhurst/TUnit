using System.Collections.Concurrent;
using TUnit.Core;

namespace TUnit.Engine.Services;

/// <summary>
/// Resolves test dependencies on-demand during streaming
/// </summary>
internal sealed class TestDependencyResolver
{
    private readonly ConcurrentDictionary<string, ExecutableTest> _testsByName = new();
    private readonly ConcurrentDictionary<string, List<string>> _pendingDependents = new();
    
    public void RegisterTest(ExecutableTest test)
    {
        _testsByName[test.TestId] = test;
        
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
    
    public bool TryResolveDependencies(ExecutableTest test)
    {
        if (test.Dependencies.Length > 0)
        {
            return true; // Already resolved
        }
        
        return ResolveDependenciesForTest(test);
    }
    
    private bool ResolveDependenciesForTest(ExecutableTest test)
    {
        var dependencies = new List<ExecutableTest>();
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
    
    private IEnumerable<ExecutableTest> GetAllDependencies(
        ExecutableTest test, 
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
}