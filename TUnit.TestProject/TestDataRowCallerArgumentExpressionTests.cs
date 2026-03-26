using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class TestDataRowCallerArgumentExpressionTests
{
    [Test]
    [MethodDataSource(nameof(InferredDisplayNameData))]
    public async Task Inferred_DisplayName_Uses_CallerArgumentExpression(string username, string password)
    {
        await Assert.That(TestContext.Current!.Metadata.DisplayName)
            .IsEqualTo("""Inferred_DisplayName_Uses_CallerArgumentExpression(("admin", "secret123"))""");
    }

    [Test]
    [MethodDataSource(nameof(ExplicitDisplayNameData))]
    public async Task Explicit_DisplayName_Overrides_CallerArgumentExpression(string username, string password)
    {
        await Assert.That(TestContext.Current!.Metadata.DisplayName)
            .IsEqualTo("Admin login");
    }

    public static IEnumerable<TestDataRow<(string, string)>> InferredDisplayNameData()
    {
        yield return new(("admin", "secret123"));
    }

    public static IEnumerable<TestDataRow<(string, string)>> ExplicitDisplayNameData()
    {
        yield return new(("admin", "secret123"), DisplayName: "Admin login");
    }
}
