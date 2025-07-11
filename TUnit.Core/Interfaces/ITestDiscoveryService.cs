namespace TUnit.Core.Interfaces;

/// <summary>
/// Service for discovering and managing test contexts
/// </summary>
public interface ITestDiscoveryService
{
    /// <summary>
    /// Gets all test contexts that match the specified predicate
    /// </summary>
    IEnumerable<TestContext> GetTests(Func<TestContext, bool> predicate);
    
    /// <summary>
    /// Gets all test contexts with the specified test name
    /// </summary>
    List<TestContext> GetTestsByName(string testName);
    
    /// <summary>
    /// Reregisters a test with new arguments
    /// </summary>
    Task ReregisterTestWithArguments(TestContext context, object?[]? methodArguments, Dictionary<string, object?>? objectBag);
}