using System.Collections.Concurrent;
using TUnit.Core;

namespace TUnit.Engine.Services;

internal sealed class TestDependencyResolver
{
    // Primary storage - all tests registered in the system
    private readonly List<AbstractExecutableTest> _allTests = new();
    
    // Indices for efficient lookup by Type and MethodName (still needed for initial matching)
    private readonly Dictionary<Type, List<AbstractExecutableTest>> _testsByType = new();
    private readonly Dictionary<string, List<AbstractExecutableTest>> _testsByMethodName = new();
    
    // Track tests with unresolved dependencies (using object references, not strings)
    private readonly List<AbstractExecutableTest> _testsWithPendingDependencies = new();
    
    // Prevent circular resolution attempts
    private readonly HashSet<AbstractExecutableTest> _testsBeingResolved = new();
    
    // Lock for thread-safe registration and resolution
    private readonly object _resolutionLock = new();

    public void RegisterTest(AbstractExecutableTest test)
    {
        lock (_resolutionLock)
        {
            _allTests.Add(test);
            
            // Index by type for class-level dependency lookups
            var testType = test.Metadata.TestClassType;
            if (!_testsByType.TryGetValue(testType, out var testsForType))
            {
                testsForType = new List<AbstractExecutableTest>();
                _testsByType[testType] = testsForType;
            }
            testsForType.Add(test);
            
            // Index by method name for method-level dependency lookups
            var methodName = test.Metadata.TestMethodName;
            if (!_testsByMethodName.TryGetValue(methodName, out var testsForMethod))
            {
                testsForMethod = new List<AbstractExecutableTest>();
                _testsByMethodName[methodName] = testsForMethod;
            }
            testsForMethod.Add(test);
            
            // Try to resolve any pending dependencies that might now be satisfiable
            ResolvePendingDependencies();
        }
    }

    public bool TryResolveDependencies(AbstractExecutableTest test)
    {
        lock (_resolutionLock)
        {
            // Already resolved?
            if (test.Dependencies.Length > 0)
            {
                return true;
            }
            
            return ResolveDependenciesForTest(test);
        }
    }

    private bool ResolveDependenciesForTest(AbstractExecutableTest test)
    {
        // Prevent circular resolution
        if (_testsBeingResolved.Contains(test))
        {
            return false;
        }
        
        _testsBeingResolved.Add(test);
        
        try
        {
            var resolvedDependencies = new List<ResolvedDependency>();
            var allResolved = true;
            
            foreach (var dependencyMetadata in test.Metadata.Dependencies)
            {
                var matchingTests = FindMatchingTests(dependencyMetadata, test);
                
                if (matchingTests.Count == 0)
                {
                    // No matches found yet - mark this test as pending
                    if (!_testsWithPendingDependencies.Contains(test))
                    {
                        _testsWithPendingDependencies.Add(test);
                    }
                    allResolved = false;
                }
                else
                {
                    // Add all matching tests as resolved dependencies
                    foreach (var matchingTest in matchingTests)
                    {
                        resolvedDependencies.Add(new ResolvedDependency
                        {
                            Test = matchingTest,
                            Metadata = dependencyMetadata
                        });
                    }
                }
            }
            
            if (allResolved)
            {
                // Remove duplicates and self-dependencies
                var uniqueDependencies = new Dictionary<AbstractExecutableTest, ResolvedDependency>();
                foreach (var dep in resolvedDependencies)
                {
                    // Skip self-dependencies
                    if (dep.Test == test)
                    {
                        continue;
                    }
                    
                    // Keep first occurrence of each test
                    if (!uniqueDependencies.ContainsKey(dep.Test))
                    {
                        uniqueDependencies[dep.Test] = dep;
                    }
                }
                
                test.Dependencies = uniqueDependencies.Values.ToArray();
                
                // Remove from pending list if it was there
                _testsWithPendingDependencies.Remove(test);
                
                return true;
            }
            
            return false;
        }
        finally
        {
            _testsBeingResolved.Remove(test);
        }
    }
    
    private List<AbstractExecutableTest> FindMatchingTests(TestDependency dependency, AbstractExecutableTest dependentTest)
    {
        var matches = new List<AbstractExecutableTest>();
        
        // Determine the search scope based on dependency type
        IEnumerable<AbstractExecutableTest> searchScope;
        
        if (dependency.ClassType != null && string.IsNullOrEmpty(dependency.MethodName))
        {
            // Class-level dependency - search only in the specified type
            if (_testsByType.TryGetValue(dependency.ClassType, out var testsInType))
            {
                searchScope = testsInType;
            }
            else
            {
                return matches; // No tests in that type
            }
        }
        else if (!string.IsNullOrEmpty(dependency.MethodName))
        {
            // Method-level dependency - search in tests with that method name
            if (dependency.MethodName != null && _testsByMethodName.TryGetValue(dependency.MethodName, out var testsWithMethod))
            {
                searchScope = testsWithMethod;
            }
            else
            {
                return matches; // No tests with that method name
            }
        }
        else
        {
            // General dependency - search all tests
            searchScope = _allTests;
        }
        
        // Filter using the Matches logic
        foreach (var test in searchScope)
        {
            if (dependency.Matches(test.Metadata, dependentTest.Metadata))
            {
                matches.Add(test);
            }
        }
        
        return matches;
    }
    
    private void ResolvePendingDependencies()
    {
        // Try to resolve any tests that previously had unresolved dependencies
        // Use a copy to avoid modification during iteration
        var pendingTests = _testsWithPendingDependencies.ToList();
        
        foreach (var test in pendingTests)
        {
            // Skip if already resolved (defensive check)
            if (test.Dependencies.Length > 0)
            {
                _testsWithPendingDependencies.Remove(test);
                continue;
            }
            
            // Try to resolve again
            ResolveDependenciesForTest(test);
        }
    }
    
    public void ResolveAllDependencies()
    {
        lock (_resolutionLock)
        {
            // First pass - try to resolve all tests
            foreach (var test in _allTests)
            {
                if (test.Dependencies.Length == 0 && test.Metadata.Dependencies.Length > 0)
                {
                    ResolveDependenciesForTest(test);
                }
            }
            
            // Second pass - retry any that failed (in case of forward dependencies)
            var maxRetries = 3;
            for (int retry = 0; retry < maxRetries && _testsWithPendingDependencies.Count > 0; retry++)
            {
                ResolvePendingDependencies();
            }
            
            // Log any remaining unresolved dependencies
            if (_testsWithPendingDependencies.Count > 0)
            {
                foreach (var test in _testsWithPendingDependencies)
                {
                    // Mark test as having failed dependency resolution
                    test.State = TestState.Failed;
                    test.Result = new TestResult
                    {
                        State = TestState.Failed,
                        Start = DateTimeOffset.UtcNow,
                        End = DateTimeOffset.UtcNow,
                        Duration = TimeSpan.Zero,
                        Exception = new InvalidOperationException(
                            $"Could not resolve all dependencies for test {test.Metadata.TestClassType.Name}.{test.Metadata.TestMethodName}"),
                        ComputerName = Environment.MachineName
                    };
                }
            }
        }
    }
    
    public void CheckForCircularDependencies()
    {
        // Check for circular dependencies in all tests
        foreach (var test in _allTests)
        {
            if (test.Dependencies.Length > 0)
            {
                var visited = new HashSet<AbstractExecutableTest>();
                var recursionStack = new HashSet<AbstractExecutableTest>();
                
                if (HasCircularDependency(test, visited, recursionStack))
                {
                    test.State = TestState.Failed;
                    test.Result = new TestResult
                    {
                        State = TestState.Failed,
                        Start = DateTimeOffset.UtcNow,
                        End = DateTimeOffset.UtcNow,
                        Duration = TimeSpan.Zero,
                        Exception = new InvalidOperationException(
                            $"Circular dependency detected for test {test.Metadata.TestClassType.Name}.{test.Metadata.TestMethodName}"),
                        ComputerName = Environment.MachineName
                    };
                }
            }
        }
    }
    
    private bool HasCircularDependency(AbstractExecutableTest test, HashSet<AbstractExecutableTest> visited, HashSet<AbstractExecutableTest> recursionStack)
    {
        visited.Add(test);
        recursionStack.Add(test);
        
        foreach (var dependency in test.Dependencies)
        {
            if (!visited.Contains(dependency.Test))
            {
                if (HasCircularDependency(dependency.Test, visited, recursionStack))
                {
                    return true;
                }
            }
            else if (recursionStack.Contains(dependency.Test))
            {
                return true; // Found a cycle
            }
        }
        
        recursionStack.Remove(test);
        return false;
    }
    
    public IReadOnlyList<TestDetails> GetTransitiveDependencies(TestDetails testDetails)
    {
        var visited = new HashSet<TestDetails>();
        var result = new List<TestDetails>();
        
        void CollectDependencies(TestDetails current)
        {
            if (!visited.Add(current))
            {
                return; // Already visited
            }
            
            // Find the actual test object
            var test = _allTests.FirstOrDefault(t => 
                t.Metadata.TestClassType == current.ClassType &&
                t.Metadata.TestMethodName == current.TestName);
            
            if (test?.Dependencies != null)
            {
                foreach (var dep in test.Dependencies)
                {
                    var depDetails = dep.Test.Context.TestDetails;
                    result.Add(depDetails);
                    CollectDependencies(depDetails);
                }
            }
        }
        
        CollectDependencies(testDetails);
        return result;
    }
}