namespace TUnit.Core.Interfaces;

/// <summary>
/// Factory for creating test execution contexts.
/// </summary>
public interface ITestContextFactory
{
    /// <summary>
    /// Creates a test context for the given test definition and arguments.
    /// </summary>
    /// <param name="testDetails">The test details</param>
    /// <param name="classInstance">The test class instance</param>
    /// <param name="classArguments">Class constructor arguments</param>
    /// <param name="methodArguments">Test method arguments</param>
    /// <returns>A configured test context</returns>
    TestContext CreateContext(
        TestDetails testDetails,
        object? classInstance,
        object?[]? classArguments,
        object?[]? methodArguments);

    /// <summary>
    /// Creates test details from a test definition.
    /// </summary>
    /// <param name="definition">The test definition</param>
    /// <param name="testName">The formatted test name</param>
    /// <returns>Test details for execution</returns>
    TestDetails CreateTestDetails(
        TestDefinition definition,
        string testName);
}