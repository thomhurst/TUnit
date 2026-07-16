using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs.Issue5267;

[EngineTest(ExpectedResult.Pass)]
public class StateBagDataSourcePropagationTests
{
    public static IEnumerable<string> TestData()
    {
        var builderContext = TestBuilderContext.Current;
        if (builderContext != null)
        {
            builderContext.StateBag["DataGeneratedAt"] = "2025-01-01";
            builderContext.StateBag["GeneratorVersion"] = "1.0";
        }

        yield return "test1";
        yield return "test2";
    }

    [Test]
    [MethodDataSource(nameof(TestData))]
    public async Task StateBag_Data_From_DataSource_Should_Be_Available_In_TestContext(string value)
    {
        await Assert.That(TestContext.Current!.StateBag["DataGeneratedAt"]).IsEqualTo("2025-01-01");
        await Assert.That(TestContext.Current!.StateBag["GeneratorVersion"]).IsEqualTo("1.0");
    }
}
