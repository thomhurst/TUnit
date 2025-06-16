using System.Collections;
using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments(2)]
[ClassDataSource<DataSource>]
public class AllDataSourcesCombinedTests(int classValue)
{
    private static readonly ConcurrentBag<string> ExecutedTests = new();

    [Test]
    [Arguments("A")]
    [Arguments("B")]
    [MethodDataSource(nameof(GetTestData))]
    public async Task TestWithAllDataSources(string methodValue)
    {
        // Record this test execution
        ExecutedTests.Add($"Class:{classValue},Method:{methodValue}");
        
        // With 2 class Arguments + 3 ClassDataSource values = 5 class instances
        // With 2 method Arguments + 2 MethodDataSource values = 4 method values
        // Total should be 5 * 4 = 20 test instances
        await Task.CompletedTask;
    }
    
    public static IEnumerable<string> GetTestData()
    {
        yield return "X";
        yield return "Y";
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
        
        // Should have exactly 20 test instances
        // Class: 2 Arguments + 3 ClassDataSource = 5 values
        // Method: 2 Arguments + 2 MethodDataSource = 4 values
        // Total: 5 * 4 = 20 combinations
        await Assert.That(executedTests.Count).IsEqualTo(20);
        
        // Verify we have all expected combinations
        var classValues = new[] { 1, 2, 10, 20, 30 };
        var methodValues = new[] { "A", "B", "X", "Y" };
        
        foreach (var classVal in classValues)
        {
            foreach (var methodVal in methodValues)
            {
                var expected = $"Class:{classVal},Method:{methodVal}";
                await Assert.That(executedTests).Contains(expected);
            }
        }
        
        // Also verify we don't have duplicates
        await Assert.That(executedTests.Distinct().Count()).IsEqualTo(20);
        
        // Clear for next run
        ExecutedTests.Clear();
    }
    
    private class DataSource : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator()
        {
            yield return 10;
            yield return 20;
            yield return 30;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}