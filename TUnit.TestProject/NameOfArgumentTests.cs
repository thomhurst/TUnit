using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class NameOfArgumentTests
{
    [Test]
    [Arguments(nameof(TestName))]
    public async Task TestName(string name)
    {
        await Assert.That(name).IsEqualTo("TestName");
    }
}
