using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[AutoFixtureGenerator<int, string, bool>]  // Generator 1
[AutoFixtureGenerator]                     // Generator 2
public class MultipleClassDataGeneratorsTests(int value1, string value2, bool value3)
{
    private static readonly ConcurrentBag<string> ExecutedTests = new();

    [Test]
    public async Task TestWithMultipleClassGenerators()
    {
        // Record this test execution
        ExecutedTests.Add($"Test executed with: {value1}, {value2}, {value3}");

        // With 2 class-level generators, we should have 2 test instances
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

        // Should have exactly 2 test instances (one per class generator)
        await Assert.That(executedTests.Count).IsEqualTo(2);

        // Clear for next run
        ExecutedTests.Clear();
    }
}
