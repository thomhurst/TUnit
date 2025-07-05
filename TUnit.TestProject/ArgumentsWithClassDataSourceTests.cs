using System.Collections;
using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments(2)]
[ClassDataSource(typeof(IntDataSource1))]
[ClassDataSource(typeof(IntDataSource2))]
public class ArgumentsWithClassDataSourceTests(int classArg)
{
    private static readonly ConcurrentBag<string> ExecutedTests = new();

    [Test]
    public async Task TestWithArgumentsAndClassDataSource()
    {
        // Record this test execution
        ExecutedTests.Add($"Class:{classArg}");
        
        // With 2 Arguments and 2 ClassDataSource,
        // we should have 2 * 2 = 4 total test instances
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
        
        // Log what we actually got
        Console.WriteLine($"Executed tests count: {executedTests.Count}");
        foreach (var test in executedTests)
        {
            Console.WriteLine($"  - {test}");
        }
        
        // Should have exactly 4 test instances (2 Arguments * 2 ClassDataSource)
        await Assert.That(executedTests.Count).IsEqualTo(4);
        
        // Verify we have the expected values
        var expected = new[]
        {
            "Class:1",      // From Arguments
            "Class:2",      // From Arguments  
            "Class:100",    // From IntDataSource1
            "Class:200"     // From IntDataSource2
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
    
    public class IntDataSource1
    {
        public static implicit operator int(IntDataSource1 _) => 100;
    }
    
    public class IntDataSource2
    {
        public static implicit operator int(IntDataSource2 _) => 200;
    }
}