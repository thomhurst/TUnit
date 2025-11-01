using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Tests for error scenarios with MixedParametersDataSource
/// These tests are expected to fail during test initialization
/// </summary>
public class MixedParametersDataSourceErrorTests
{
    // Note: These tests intentionally have incorrect configurations
    // They should fail during test discovery/initialization, not during execution

    // [Test]
    // [MixedParametersDataSource]
    // public async Task ParameterWithoutDataSource_ShouldFail(
    //     [Arguments(1, 2)] int x,
    //     int y) // Missing data source attribute - should fail
    // {
    //     await Task.CompletedTask;
    // }

    // [Test]
    // [MixedParametersDataSource]
    // public async Task NoParametersWithDataSources_ShouldFail()
    // {
    //     // Should fail because there are no parameters with data sources
    //     await Task.CompletedTask;
    // }
}

/// <summary>
/// Tests that should pass - verifying edge case handling
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class MixedParametersDataSourceEdgeCaseTests
{
    public static IEnumerable<string> GetEmptyStrings()
    {
        // Intentionally empty
        yield break;
    }

    // Note: SkipIfEmpty is not a property of MethodDataSource
    // This test is commented out for now
    // [Test]
    // [MixedParametersDataSource]
    // public async Task EmptyDataSource_ShouldHandleGracefully(
    //     [Arguments(1)] int x,
    //     [MethodDataSource(nameof(GetEmptyStrings))] string y)
    // {
    //     // Test behavior when data source returns nothing
    //     await Assert.That(x).IsEqualTo(1);
    // }

    [Test]
    [MixedParametersDataSource]
    public async Task ParameterWithNullValues(
        [Arguments(null, 1, 2)] int? x,
        [Arguments(null, "a")] string? y)
    {
        // Should handle null values correctly
        // Creates 3 × 2 = 6 test cases including nulls
        await Task.CompletedTask;
    }
}
