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
        var properties = TestContext.Current!.Metadata.TestDetails.CustomProperties;

        var array = properties["Group"].ToArray();

        await Assert.That(array).HasCount().EqualTo(3);
        await Assert.That(array).Contains((string x) => x == "Bugs");
        await Assert.That(array).Contains((string x) => x == "2481");
        await Assert.That(array).Contains((string x) => x == "TUnit");
    }
}
