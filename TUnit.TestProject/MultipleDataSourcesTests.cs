using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments(2)]
public class MultipleDataSourcesTests(int classArg)
{
    private static readonly ConcurrentBag<string> ExecutedTests = new();

    [Test]
    [Arguments("A")]
    [Arguments("B")]
    public async Task TestWithMultipleDataSources(string methodArg)
    {
        // Record this test execution
        ExecutedTests.Add($"Class:{classArg},Method:{methodArg}");
        
        // With 2 class-level Arguments and 2 method-level Arguments,
        // we should have 2 * 2 = 4 total test instances eventually
        await Task.CompletedTask;
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
        
        // Should have exactly 4 test instances
        await Assert.That(executedTests.Count).IsEqualTo(4);
        
        // Verify we have the expected combinations
        var expected = new[]
        {
            "Class:1,Method:A",
            "Class:1,Method:B",
            "Class:2,Method:A",
            "Class:2,Method:B"
        };

        foreach (var expectedTest in expected)
        {
            await Assert.That(executedTests).Contains(expectedTest);
        }
        
        // Also verify we don't have duplicates
        await Assert.That(executedTests.Distinct().Count()).IsEqualTo(4);
        
        // Clear for next run
        ExecutedTests.Clear();
    }
}