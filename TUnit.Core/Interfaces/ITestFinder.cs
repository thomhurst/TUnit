namespace TUnit.Core.Interfaces;

public interface ITestFinder
{
    IEnumerable<TestContext> GetTests(Type classType);

    TestContext[] GetTestsByNameAndParameters(string testName, IEnumerable<Type> methodParameterTypes,
        Type classType, IEnumerable<Type> classParameterTypes, IEnumerable<object?> classArguments);
    
    /// <summary>
    /// Gets all test contexts that match the specified predicate
    /// </summary>
    IEnumerable<TestContext> GetTests(Func<TestContext, bool> predicate);
    
    /// <summary>
    /// Gets all test contexts with the specified test name
    /// </summary>
    List<TestContext> GetTestsByName(string testName);
    
    /// <summary>
    /// Gets all test contexts with the specified test name and class type
    /// </summary>
    List<TestContext> GetTestsByName(string testName, Type classType);
    
    /// <summary>
    /// Reregisters a test with new arguments
    /// </summary>
    Task ReregisterTestWithArguments(TestContext context, object?[]? methodArguments, Dictionary<string, object?>? objectBag);
}
