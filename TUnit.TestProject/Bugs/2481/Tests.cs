namespace TUnit.TestProject.Bugs._2481;

public class Tests
{
    [Test]
    [Property("Group", "Bugs")]
    [Property("Group", "2481")]
    [Property("Group", "TUnit")]
    public async Task Test()
    {
        var properties = TestContext.Current!.TestDetails.CustomProperties;
        
        var array = properties["Group"].ToArray();

        await Assert.That(array).HasCount().EqualTo(3)
            .And.Contains(x => x is "Bugs")
            .And.Contains(x => x is "2481")
            .And.Contains(x => x is "TUnit");
    }
}