using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Services;

/// <summary>
/// Implementation of ITestDiscoveryService for discovering and managing test contexts
/// </summary>
public class TestDiscoveryService : ITestDiscoveryService
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
    /// Gets all test contexts that match the specified predicate
    /// </summary>
    public IEnumerable<TestContext> GetTests(Func<TestContext, bool> predicate)
    {
        return _allTests.Where(predicate);
    }
    
    /// <summary>
    /// Gets all test contexts with the specified test name
    /// </summary>
    public List<TestContext> GetTestsByName(string testName)
    {
        if (_testsByName.TryGetValue(testName, out var tests))
        {
            return new List<TestContext>(tests);
        }
        return new List<TestContext>();
    }
    
    /// <summary>
    /// Reregisters a test with new arguments
    /// </summary>
    public async Task ReregisterTestWithArguments(TestContext context, object?[]? methodArguments, Dictionary<string, object?>? objectBag)
    {
        // For now, we just register the updated context
        // In a real implementation, this would create a new test variation
        RegisterTest(context);
        await Task.CompletedTask;
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