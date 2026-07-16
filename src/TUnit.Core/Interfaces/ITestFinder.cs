namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines a service for finding and retrieving test contexts by type or method signature.
/// </summary>
/// <remarks>
/// <para>
/// This interface is used internally by the TUnit framework to locate tests for
/// dependency resolution (via <c>[DependsOn]</c>) and other scenarios where tests
/// need to be looked up at runtime.
/// </para>
/// <para>
/// Third-party extensions can use this interface to query the set of registered tests
/// for a given class or to find specific tests by their method signature.
/// </para>
/// </remarks>
public interface ITestFinder
{
    /// <summary>
    /// Gets all test contexts for tests defined in the specified class type.
    /// </summary>
    /// <param name="classType">The type of the test class to search.</param>
    /// <returns>An enumerable of test contexts for all tests in the specified class.</returns>
    IEnumerable<TestContext> GetTests(Type classType);

    /// <summary>
    /// Gets test contexts matching a specific method name and parameter signature.
    /// </summary>
    /// <param name="testName">The name of the test method.</param>
    /// <param name="methodParameterTypes">The types of the test method parameters.</param>
    /// <param name="classType">The type of the test class containing the method.</param>
    /// <param name="classParameterTypes">The types of the class constructor parameters.</param>
    /// <param name="classArguments">The class constructor argument values used to match a specific class instance.</param>
    /// <returns>An array of matching test contexts.</returns>
    TestContext[] GetTestsByNameAndParameters(string testName, IEnumerable<Type> methodParameterTypes,
        Type classType, IEnumerable<Type> classParameterTypes, IEnumerable<object?> classArguments);
}
