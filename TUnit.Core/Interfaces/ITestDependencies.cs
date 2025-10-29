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
    /// Gets tests matching the specified predicate.
    /// </summary>
    IEnumerable<TestContext> GetTests(Func<TestContext, bool> predicate);

    /// <summary>
    /// Gets all tests with the specified name.
    /// </summary>
    List<TestContext> GetTests(string testName);

    /// <summary>
    /// Gets all tests with the specified name and class type.
    /// </summary>
    List<TestContext> GetTests(string testName, Type classType);
}
