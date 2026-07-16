namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides helpers for creating isolated resource names per test instance.
/// Useful for database tables, queue names, cache keys, and other resources
/// that need to be unique across parallel test execution.
/// Accessed via <see cref="TestContext.Isolation"/>.
/// </summary>
public interface ITestIsolation
{
    /// <summary>
    /// Gets a unique identifier for this test instance.
    /// This value is assigned atomically and is guaranteed to be unique across all test instances in the process.
    /// </summary>
    int UniqueId { get; }

    /// <summary>
    /// Creates an isolated name by combining a base name with the test's unique identifier.
    /// Use for database tables, Redis keys, Kafka topics, etc.
    /// </summary>
    /// <param name="baseName">The base name for the resource.</param>
    /// <returns>A unique name in the format "Test_{UniqueId}_{baseName}".</returns>
    /// <example>
    /// <code>
    /// // In a test with UniqueId = 42:
    /// var tableName = TestContext.Current!.Isolation.GetIsolatedName("todos");  // Returns "Test_42_todos"
    /// var topicName = TestContext.Current!.Isolation.GetIsolatedName("orders"); // Returns "Test_42_orders"
    /// </code>
    /// </example>
    string GetIsolatedName(string baseName);

    /// <summary>
    /// Creates an isolated prefix using the test's unique identifier.
    /// Use for key prefixes in Redis, Kafka topic prefixes, etc.
    /// </summary>
    /// <param name="separator">The separator character. Defaults to "_".</param>
    /// <returns>A unique prefix in the format "test{separator}{UniqueId}{separator}".</returns>
    /// <example>
    /// <code>
    /// // In a test with UniqueId = 42:
    /// var prefix = TestContext.Current!.Isolation.GetIsolatedPrefix();       // Returns "test_42_"
    /// var dotPrefix = TestContext.Current!.Isolation.GetIsolatedPrefix("."); // Returns "test.42."
    /// </code>
    /// </example>
    string GetIsolatedPrefix(string separator = "_");
}
