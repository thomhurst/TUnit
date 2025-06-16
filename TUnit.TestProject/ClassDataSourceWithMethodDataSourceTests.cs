using System.Collections;
using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<DataSource>]
public class ClassDataSourceWithMethodDataSourceTests(int classValue)
{
    private static readonly ConcurrentBag<string> ExecutedTests = new();

    [Test]
    [MethodDataSource(nameof(GetTestData))]
    public async Task TestWithClassAndMethodDataSource(string methodValue)
    {
        // Record this test execution
        ExecutedTests.Add($"Class:{classValue},Method:{methodValue}");
        
        // With ClassDataSource providing 3 values and MethodDataSource providing 2 values,
        // we should have 3 * 2 = 6 total test instances (cartesian product)
        await Task.CompletedTask;
    }
    
    public static IEnumerable<string> GetTestData()
    {
        yield return "Alpha";
        yield return "Beta";
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
        
        // Should have exactly 6 test instances (3 from ClassDataSource * 2 from MethodDataSource)
        await Assert.That(executedTests.Count).IsEqualTo(6);
        
        // Verify we have the expected combinations
        var expected = new[]
        {
            "Class:100,Method:Alpha",
            "Class:100,Method:Beta",
            "Class:200,Method:Alpha",
            "Class:200,Method:Beta",
            "Class:300,Method:Alpha",
            "Class:300,Method:Beta"
        };

        foreach (var expectedTest in expected)
        {
            await Assert.That(executedTests).Contains(expectedTest);
        }
        
        // Also verify we don't have duplicates
        await Assert.That(executedTests.Distinct().Count()).IsEqualTo(6);
        
        // Clear for next run
        ExecutedTests.Clear();
    }
    
    private class DataSource : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator()
        {
            yield return 100;
            yield return 200;
            yield return 300;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}