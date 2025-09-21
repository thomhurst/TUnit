using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class TestContextTests
{
    [Test]
    public async Task Test()
    {
        var id = TestContext.Current!.Id;

        var context = TestContext.GetById(id);

        await Assert.That(context).IsNotNull();
    }
}
