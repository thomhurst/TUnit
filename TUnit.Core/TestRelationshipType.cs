namespace TUnit.Core;

/// <summary>
/// Defines the relationship type between tests for property testing and other scenarios
/// </summary>
public enum TestRelationshipType
{
    /// <summary>
    /// Standard independent test with no relationship to other tests
    /// </summary>
    Independent,

    /// <summary>
    /// Original generated test from a property data source
    /// </summary>
    PropertyTestOriginal,

    /// <summary>
    /// Shrunk test case generated from a failing property test
    /// </summary>
    PropertyTestShrink
}
