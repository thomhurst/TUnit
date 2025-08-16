namespace TUnit.UnitTests;

/// <summary>
/// Integration tests to verify that empty InstanceMethodDataSource works correctly end-to-end
/// This ensures the fix for issue #2862 works properly
/// </summary>
public class EmptyDataSourceTests
{
    public IEnumerable<object[]> EmptyData()
    {
        return [];
    }
    
    public IEnumerable<int> NonEmptyData()
    {
        return [1, 2, 3];
    }
    
    public static IEnumerable<object[]> StaticEmptyData()
    {
        return [];
    }
    
    public static IEnumerable<string> StaticNonEmptyData()
    {
        return ["a", "b"];
    }

    /// <summary>
    /// This test should not fail with "Test instance is null" error for empty data sources
    /// Empty data sources should execute once with no parameters
    /// </summary>
    [Test]
    [InstanceMethodDataSource(nameof(EmptyData))]
    public async Task EmptyInstanceMethodDataSource_ShouldWork()
    {
        // This test will only run if the fix works - empty data sources should yield one empty result
        // If the bug exists, this test won't even reach here due to "Test instance is null" error
        await Assert.That(true).IsTrue(); // Test should reach here successfully
    }

    /// <summary>
    /// Test that non-empty instance method data sources continue to work
    /// </summary>
    [Test]
    [InstanceMethodDataSource(nameof(NonEmptyData))]
    public async Task NonEmptyInstanceMethodDataSource_ShouldWork(int value)
    {
        await Assert.That(value).IsGreaterThan(0);
        await Assert.That(value).IsLessThanOrEqualTo(3);
    }

    /// <summary>
    /// Test that empty static method data sources work correctly
    /// Empty data sources should execute once with no parameters
    /// </summary>
    [Test]
    [MethodDataSource(nameof(StaticEmptyData))]
    public async Task EmptyStaticMethodDataSource_ShouldWork()
    {
        // This test should reach here successfully with the fix
        await Assert.That(true).IsTrue();
    }

    /// <summary>
    /// Test that non-empty static method data sources continue to work
    /// </summary>
    [Test]
    [MethodDataSource(nameof(StaticNonEmptyData))]
    public async Task NonEmptyStaticMethodDataSource_ShouldWork(string value)
    {
        await Assert.That(value).IsNotNull();
        await Assert.That(value).IsNotEmpty();
    }
}