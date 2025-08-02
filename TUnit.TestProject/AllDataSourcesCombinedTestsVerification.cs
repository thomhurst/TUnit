using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments(2)]
[ClassDataSource(typeof(DataSource1))]
[ClassDataSource(typeof(DataSource2))]
[ClassDataSource(typeof(DataSource3))]
public class AllDataSourcesCombinedTestsVerification
{
    private readonly int classValue;
    private static readonly ConcurrentBag<string> ExecutedTests = [];
    
    public AllDataSourcesCombinedTestsVerification(int value)
    {
        classValue = value;
    }

    [Test]
    [Arguments("A")]
    [Arguments("B")]
    [MethodDataSource(nameof(GetTestData))]
    public async Task TestWithAllDataSources(string methodValue)
    {
        var testId = $"Class:{classValue},Method:{methodValue}";
        ExecutedTests.Add(testId);
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
        var executedTests = ExecutedTests.ToList();
        
        // Skip verification if no tests were executed (e.g., filtered run)
        if (executedTests.Count == 0)
        {
            return;
        }

        // Expected combinations: 5 class values × 4 method values = 20 total
        await Assert.That(executedTests.Count).IsEqualTo(20);
        
        // Clear for next run
        ExecutedTests.Clear();
    }

    public class DataSource1
    {
        public static implicit operator int(DataSource1 _) => 10;
    }

    public class DataSource2
    {
        public static implicit operator int(DataSource2 _) => 20;
    }

    public class DataSource3
    {
        public static implicit operator int(DataSource3 _) => 30;
    }
}