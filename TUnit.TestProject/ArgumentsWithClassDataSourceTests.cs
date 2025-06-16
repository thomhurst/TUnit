using System.Collections;
using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments(2)]
[ClassDataSource<DataSource>]
public class ArgumentsWithClassDataSourceTests(int classArg)
{
    private static readonly ConcurrentBag<string> ExecutedTests = new();

    [Test]
    public async Task TestWithArgumentsAndClassDataSource()
    {
        // Record this test execution
        ExecutedTests.Add($"Class:{classArg}");
        
        // With 2 Arguments and 1 ClassDataSource providing 3 values,
        // we should have 2 + 3 = 5 total test instances
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
        
        // Should have exactly 5 test instances (2 from Arguments + 3 from ClassDataSource)
        await Assert.That(executedTests.Count).IsEqualTo(5);
        
        // Verify we have the expected values
        var expected = new[]
        {
            "Class:1",      // From Arguments
            "Class:2",      // From Arguments
            "Class:10",     // From ClassDataSource
            "Class:20",     // From ClassDataSource
            "Class:30"      // From ClassDataSource
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