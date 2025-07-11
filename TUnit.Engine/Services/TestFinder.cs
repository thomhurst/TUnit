using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Services;

/// <summary>
/// Implementation of ITestFinder for discovering and managing test contexts
/// </summary>
public class TestFinder : ITestFinder
{
    private readonly ConcurrentBag<TestContext> _allTests = new();
    private readonly ConcurrentDictionary<string, List<TestContext>> _testsByName = new();
    
    /// <summary>
    /// Registers a test context with the discovery service
    /// </summary>
    public void RegisterTest(TestContext testContext)
    {
        _allTests.Add(testContext);
        _testsByName.AddOrUpdate(testContext.TestName, 
            new List<TestContext> { testContext },
            (_, list) =>
            {
                list.Add(testContext);
                return list;
            });
    }
    
    /// <summary>
    /// Gets all test contexts for the specified class type
    /// </summary>
    public IEnumerable<TestContext> GetTests(Type classType)
    {
        return _allTests.Where(t => t.TestDetails?.ClassType == classType);
    }

    /// <summary>
    /// Gets test contexts by name and parameters
    /// </summary>
    public TestContext[] GetTestsByNameAndParameters(string testName, IEnumerable<Type> methodParameterTypes,
        Type classType, IEnumerable<Type> classParameterTypes, IEnumerable<object?> classArguments)
    {
        var paramTypes = methodParameterTypes?.ToArray() ?? Array.Empty<Type>();
        var classParamTypes = classParameterTypes?.ToArray() ?? Array.Empty<Type>();
        
        // If no parameter types are specified, match by name and class type only
        if (paramTypes.Length == 0 && classParamTypes.Length == 0)
        {
            return _allTests.Where(t => 
                t.TestName == testName &&
                t.TestDetails?.ClassType == classType).ToArray();
        }
        
        return _allTests.Where(t => 
            t.TestName == testName &&
            t.TestDetails?.ClassType == classType &&
            ParameterTypesMatch(t.TestDetails.TestMethodParameterTypes, paramTypes) &&
            ClassParametersMatch(t, classParamTypes, classArguments)).ToArray();
    }
    
    private bool ParameterTypesMatch(Type[]? testParamTypes, Type[] expectedParamTypes)
    {
        if (testParamTypes == null && expectedParamTypes.Length == 0) return true;
        if (testParamTypes == null || testParamTypes.Length != expectedParamTypes.Length) return false;
        
        for (int i = 0; i < testParamTypes.Length; i++)
        {
            if (testParamTypes[i] != expectedParamTypes[i]) return false;
        }
        return true;
    }
    
    private bool ClassParametersMatch(TestContext context, Type[] classParamTypes, IEnumerable<object?> classArguments)
    {
        // For now, just check parameter count
        var argCount = classArguments?.Count() ?? 0;
        var actualArgCount = context.TestDetails?.TestClassArguments?.Length ?? 0;
        return argCount == actualArgCount;
    }
    
    /// <summary>
    /// Clears all registered tests (useful for testing)
    /// </summary>
    public void Clear()
    {
        // ConcurrentBag doesn't have Clear in netstandard2.0, so we need to drain it
        while (_allTests.TryTake(out _))
        {
            // Keep taking items until the bag is empty
        }
        _testsByName.Clear();
    }
}