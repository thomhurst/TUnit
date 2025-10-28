namespace TUnit.Core.Enums;

/// <summary>
/// Defines the relationship between a test and its parent test, if any.
/// Used for tracking test hierarchies in scenarios like property-based testing shrinking and retry logic.
/// </summary>
public enum TestRelationship
{
    /// <summary>
    /// This test is independent and has no parent.
    /// </summary>
    None,

    /// <summary>
    /// This test is a retry of a failed test with the same or modified arguments.
    /// </summary>
    Retry,

    /// <summary>
    /// This test was created during the shrinking phase of property-based testing,
    /// attempting to find a minimal reproduction with smaller inputs.
    /// </summary>
    ShrinkAttempt,

    /// <summary>
    /// This test was generated from a property test template (initial generation phase).
    /// </summary>
    Generated,

    /// <summary>
    /// This test was dynamically created at runtime for other purposes.
    /// </summary>
    Dynamic
}
