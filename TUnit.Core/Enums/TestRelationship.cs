namespace TUnit.Core.Enums;

/// <summary>
/// Defines the relationship between a test and its parent test, if any.
/// Used for tracking test hierarchies and informing the test runner about the category of relationship.
/// </summary>
public enum TestRelationship
{
    /// <summary>
    /// This test is independent and has no parent.
    /// </summary>
    None,

    /// <summary>
    /// An identical re-run of a test, typically following a failure.
    /// </summary>
    Retry,

    /// <summary>
    /// A test case generated as part of an initial set to explore a solution space.
    /// For example, the initial random inputs for a property-based test.
    /// </summary>
    Generated,

    /// <summary>
    /// A test case derived during the execution of a parent test, often in response to its outcome.
    /// This is the appropriate category for property-based testing shrink attempts, mutation testing variants,
    /// and other analytical test variations created at runtime based on parent test results.
    /// </summary>
    Derived
}
