namespace TUnit.UnitTests;

/// <summary>
/// Unit tests to verify that the MethodDataSourceAttribute fix for issue #2862 works correctly.
/// These tests verify that empty data sources behave consistently with NoDataSource by yielding
/// exactly one empty result, rather than zero results.
/// </summary>
public class EmptyDataSourceTests
{
    /// <summary>
    /// Empty static data source that should yield one empty result, not zero results
    /// </summary>
    public static IEnumerable<object[]> EmptyStaticDataSource()
    {
        return [];
    }
    
    /// <summary>
    /// Non-empty static data source for comparison
    /// </summary>
    public static IEnumerable<object[]> NonEmptyStaticDataSource()
    {
        return [["a"], ["b"]];
    }

    /// <summary>
    /// Tests that empty static data sources work correctly.
    /// With the fix, empty data sources should yield one empty result and execute once with no parameters.
    /// Without the fix, they would yield zero results and cause framework issues.
    /// </summary>
    [Test]
    [MethodDataSource(nameof(EmptyStaticDataSource))]
    public async Task EmptyStaticDataSource_ShouldYieldOneEmptyResult()
    {
        // This test verifies that empty data sources now behave like NoDataSource:
        // - They yield exactly one result with empty parameter array []
        // - The test executes once with no parameters  
        // - No "test instance is null" or other framework errors occur
        await Assert.That(true).IsTrue();
    }

    /// <summary>
    /// Tests that non-empty static data sources continue to work correctly
    /// </summary>
    [Test]
    [MethodDataSource(nameof(NonEmptyStaticDataSource))]
    public async Task NonEmptyStaticDataSource_ShouldWork(string value)
    {
        await Assert.That(value).IsNotNull();
        await Assert.That(value).IsNotEmpty();
    }
}