using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class MethodDataSourceWithArgumentsTests
{
    private static readonly ConcurrentBag<string> ExecutedTests = new();

    [Test]
    [Arguments("A")]
    [Arguments("B")]
    [MethodDataSource(nameof(GetTestData))]
    public async Task TestWithMethodDataSourceAndArguments(string value)
    {
        // Record this test execution
        ExecutedTests.Add($"Method:{value}");

        // With 2 Arguments and 1 MethodDataSource providing 3 values,
        // we should have 2 + 3 = 5 total test instances
        await Task.CompletedTask;
    }

    public static IEnumerable<string> GetTestData()
    {
        yield return "X";
        yield return "Y";
        yield return "Z";
    }

    [After(Assembly)]
    public static async Task VerifyExpectedCombinations()
    {
        // Get the executed tests
        var executedTests = ExecutedTests.ToList();

        // Skip verification if no tests were executed (e.g., filtered run)
        if (executedTests.Count == 0)
        {
            return;
        }

        // Should have exactly 5 test instances (2 from Arguments + 3 from MethodDataSource)
        await Assert.That(executedTests.Count).IsEqualTo(5);

        // Verify we have the expected values
        var expected = new[]
        {
            "Method:A",     // From Arguments
            "Method:B",     // From Arguments
            "Method:X",     // From MethodDataSource
            "Method:Y",     // From MethodDataSource
            "Method:Z"      // From MethodDataSource
        };

        foreach (var expectedTest in expected)
        {
            await Assert.That(executedTests).Contains(expectedTest);
        }

        // Also verify we don't have duplicates
        await Assert.That(executedTests.Distinct().Count()).IsEqualTo(5);

        // Clear for next run
        ExecutedTests.Clear();
    }
}
