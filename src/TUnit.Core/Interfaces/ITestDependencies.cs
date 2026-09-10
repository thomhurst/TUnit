using TUnit.Core.Enums;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides access to test dependency information and relationships.
/// Accessed via <see cref="TestContext.Dependencies"/>.
/// </summary>
public interface ITestDependencies
{
    /// <summary>
    /// Gets the collection of tests that this test depends on.
    /// Tests in this collection will execute before this test runs.
    /// </summary>
    IReadOnlyList<TestDetails> DependsOn { get; }

    /// <summary>
    /// Gets the parent test ID if this test is part of a relationship.
    /// </summary>
    string? ParentTestId { get; }

    /// <summary>
    /// Gets the relationship type of this test.
    /// </summary>
    TestRelationship Relationship { get; }

    /// <summary>
    /// Gets all registered tests that match the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter tests by.</param>
    /// <returns>A read-only list of matching test contexts.</returns>
    IReadOnlyList<TestContext> GetTests(Func<TestContext, bool> predicate);

    /// <summary>
    /// Gets all registered tests that match the specified test name.
    /// </summary>
    /// <param name="testName">The name of the test method.</param>
    /// <returns>A read-only list of matching test contexts.</returns>
    IReadOnlyList<TestContext> GetTests(string testName);

    /// <summary>
    /// Gets all registered tests that match the specified test name and class type.
    /// </summary>
    /// <param name="testName">The name of the test method.</param>
    /// <param name="classType">The type of the test class.</param>
    /// <returns>A read-only list of matching test contexts.</returns>
    IReadOnlyList<TestContext> GetTests(string testName, Type classType);
}
