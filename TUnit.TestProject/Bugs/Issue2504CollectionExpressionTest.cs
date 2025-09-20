using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs;

/// <summary>
/// Test for issue #2504 - Collection expression syntax in MethodDataSource Arguments
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class Issue2504CollectionExpressionTest
{
    private static readonly ConcurrentBag<string> ExecutedTests = [];

    [Test]
    [MethodDataSource(nameof(GetDataWithSingleIntParam), Arguments = [5])]  // Collection expression syntax  
    [MethodDataSource(nameof(GetDataWithSingleIntParam), Arguments = new object[] { 10 })]  // Traditional array syntax
    public async Task TestWithSingleArgument(int value)
    {
        ExecutedTests.Add($"SingleParam:{value}");
        await Assert.That(value).IsIn(10, 20);  // 5*2=10, 10*2=20
    }

    [Test]
    [MethodDataSource(nameof(GetDataWithMultipleParams), Arguments = [10, "test"])]  // Collection expression syntax
    public async Task TestWithMultipleArguments(int number, string text)
    {
        ExecutedTests.Add($"MultiParam:{number}-{text}");
        await Assert.That(number).IsEqualTo(15);
        await Assert.That(text).IsEqualTo("test_modified");
    }
    
    [Test]
    [MethodDataSource(nameof(GetDataWithArrayParam), Arguments = [new int[] { 4, 5 }])]  // Collection expression with array element
    public async Task TestWithArrayArgument(int value)
    {
        ExecutedTests.Add($"ArrayParam:{value}");
        await Assert.That(value).IsIn(4, 5);
    }

    public static IEnumerable<int> GetDataWithSingleIntParam(int multiplier)
    {
        // Return test data based on the multiplier
        yield return multiplier * 2;
    }

    public static IEnumerable<object[]> GetDataWithMultipleParams(int baseNumber, string baseText)
    {
        // Return modified values
        yield return [baseNumber + 5, baseText + "_modified"];
    }
    
    public static IEnumerable<int[]> GetDataWithArrayParam(int[] values)
    {
        // Return each value from the array as test data
        foreach (var value in values)
        {
            yield return [value];
        }
    }

    [After(Assembly)]
    public static async Task VerifyTestsExecuted()
    {
        var executedTests = ExecutedTests.ToList();

        // Skip verification if no tests were executed (e.g., filtered run)
        if (executedTests.Count == 0)
        {
            return;
        }

        // Should have 5 test instances total:
        // - 1 from first MethodDataSource with [5]
        // - 1 from second MethodDataSource with { 10 }
        // - 1 from TestWithMultipleArguments
        // - 2 from TestWithArrayArgument (array has 2 elements)
        await Assert.That(executedTests.Count).IsEqualTo(5);

        // Verify we have the expected values
        var expected = new[]
        {
            "SingleParam:10",  // 5 * 2
            "SingleParam:20",  // 10 * 2 (this should be 20, not 15!)
            "MultiParam:15-test_modified",
            "ArrayParam:4",
            "ArrayParam:5"
        };

        foreach (var expectedTest in expected)
        {
            await Assert.That(executedTests).Contains(expectedTest);
        }

        // Clear for next run
        ExecutedTests.Clear();
    }
}