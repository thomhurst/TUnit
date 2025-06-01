using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._2481;

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    [Test]
    [Property("Group", "Bugs")]
    [Property("Group", "2481")]
    [Property("Group", "TUnit")]
    public async Task Test()
    {
        var properties = TestContext.Current!.TestDetails.CustomProperties;

        await Assert.That(properties).HasCount().EqualTo(1);

        var array = properties["Group"].ToArray();

        await Assert.That(array).HasCount().EqualTo(3)
            .And.Contains(x => x is "Bugs")
            .And.Contains(x => x is "2481")
            .And.Contains(x => x is "TUnit");
    }
}