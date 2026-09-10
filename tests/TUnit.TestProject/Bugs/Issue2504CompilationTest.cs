using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs;

/// <summary>
/// Test for issue #2504 - Compilation issue with collection expression syntax in MethodDataSource Arguments.
/// This test verifies that both collection expression syntax and traditional array syntax compile correctly.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class Issue2504CompilationTest
{
    [Test]
    [MethodDataSource(nameof(GetData), Arguments = [5])]  // Collection expression syntax - previously caused compilation error
    [MethodDataSource(nameof(GetData), Arguments = new object[] { 10 })]  // Traditional array syntax
    public async Task TestWithCollectionExpressionSyntax(int value)
    {
        // Test should receive 10 (5 * 2) and 20 (10 * 2)
        await Assert.That(value).IsIn([10, 20]);
    }

    [Test]
    [MethodDataSource(nameof(GetDataWithMultipleParams), Arguments = ["hello", 42])]  // Collection expression with mixed types
    public async Task TestWithMultipleArgumentsCollectionExpression(string text, int number)
    {
        await Assert.That(text).IsEqualTo("hello_modified");
        await Assert.That(number).IsEqualTo(84);  // 42 * 2
    }

    public static int GetData(int input)
    {
        return input * 2;
    }

    public static IEnumerable<object[]> GetDataWithMultipleParams(string baseText, int baseNumber)
    {
        yield return [baseText + "_modified", baseNumber * 2];
    }
}