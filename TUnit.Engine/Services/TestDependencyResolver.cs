using TUnit.Core;

namespace TUnit.Engine.Services;

internal sealed class TestDependencyResolver
{
    private readonly List<AbstractExecutableTest> _allTests =
    [
    ];
    private readonly Dictionary<Type, List<AbstractExecutableTest>> _testsByType = new();
    private readonly Dictionary<string, List<AbstractExecutableTest>> _testsByMethodName = new();
    private readonly List<AbstractExecutableTest> _testsWithPendingDependencies =
    [
    ];
    private readonly HashSet<AbstractExecutableTest> _testsBeingResolved =
    [
    ];
    private readonly object _resolutionLock = new();

    public void RegisterTest(AbstractExecutableTest test)
    {
        lock (_resolutionLock)
        {
            _allTests.Add(test);
            
            var testType = test.Metadata.TestClassType;
            if (!_testsByType.TryGetValue(testType, out var testsForType))
            {
                testsForType =
                [
                ];
                _testsByType[testType] = testsForType;
            }
            testsForType.Add(test);
            
            var methodName = test.Metadata.TestMethodName;
            if (!_testsByMethodName.TryGetValue(methodName, out var testsForMethod))
            {
                testsForMethod =
                [
                ];
                _testsByMethodName[methodName] = testsForMethod;
            }
            testsForMethod.Add(test);
            
            ResolvePendingDependencies();
        }
    }

    public bool TryResolveDependencies(AbstractExecutableTest test)
    {
        lock (_resolutionLock)
        {
            if (test.Dependencies.Length > 0)
            {
                return true;
            }
            
            return ResolveDependenciesForTest(test);
        }
    }

    private bool ResolveDependenciesForTest(AbstractExecutableTest test)
    {
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
                    if (!_testsWithPendingDependencies.Contains(test))
                    {
                        _testsWithPendingDependencies.Add(test);
                    }
                    allResolved = false;
                }
                else
                {
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
                var uniqueDependencies = new Dictionary<AbstractExecutableTest, ResolvedDependency>();
                foreach (var dep in resolvedDependencies)
                {
                    if (dep.Test == test)
                    {
                        continue;
                    }
                    
                    if (!uniqueDependencies.ContainsKey(dep.Test))
                    {
                        uniqueDependencies[dep.Test] = dep;
                    }
                }
                
                test.Dependencies = uniqueDependencies.Values.ToArray();
                
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
        
        IEnumerable<AbstractExecutableTest> searchScope;
        
        if (dependency.ClassType != null && string.IsNullOrEmpty(dependency.MethodName))
        {
            if (_testsByType.TryGetValue(dependency.ClassType, out var testsInType))
            {
                searchScope = testsInType;
            }
            else
            {
                return matches;
            }
        }
        else if (!string.IsNullOrEmpty(dependency.MethodName))
        {
            if (dependency.MethodName != null && _testsByMethodName.TryGetValue(dependency.MethodName, out var testsWithMethod))
            {
                searchScope = testsWithMethod;
            }
            else
            {
                return matches;
            }
        }
        else
        {
            searchScope = _allTests;
        }
        
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
        var pendingTests = _testsWithPendingDependencies.ToList();
        
        foreach (var test in pendingTests)
        {
            if (test.Dependencies.Length > 0)
            {
                _testsWithPendingDependencies.Remove(test);
                continue;
            }
            
            ResolveDependenciesForTest(test);
        }
    }
    
    public void ResolveAllDependencies()
    {
        lock (_resolutionLock)
        {
            foreach (var test in _allTests)
            {
                if (test.Dependencies.Length == 0 && test.Metadata.Dependencies.Length > 0)
                {
                    ResolveDependenciesForTest(test);
                }
            }
            
            var maxRetries = 3;
            for (int retry = 0; retry < maxRetries && _testsWithPendingDependencies.Count > 0; retry++)
            {
                ResolvePendingDependencies();
            }
            
            if (_testsWithPendingDependencies.Count > 0)
            {
                foreach (var test in _testsWithPendingDependencies)
                {
                    CreateDependencyResolutionFailedResult(test);
                }
            }
        }
    }
    
    
    public IReadOnlyList<TestDetails> GetTransitiveDependencies(TestDetails testDetails)
    {
        var visited = new HashSet<TestDetails>();
        var result = new List<TestDetails>();
        
        void CollectDependencies(TestDetails current)
        {
            if (!visited.Add(current))
            {
                return;
            }
            
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

    private static void CreateDependencyResolutionFailedResult(AbstractExecutableTest test)
    {
        test.State = TestState.Failed;
        var now = DateTimeOffset.UtcNow;
        test.Result = new TestResult
        {
            State = TestState.Failed,
            Start = now,
            End = now,
            Duration = TimeSpan.Zero,
            Exception = new InvalidOperationException(
                $"Could not resolve all dependencies for test {test.Metadata.TestClassType.Name}.{test.Metadata.TestMethodName}"),
            ComputerName = Environment.MachineName
        };
    }
}