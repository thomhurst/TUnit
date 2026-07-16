using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class SkippableArgumentTests
{
    [Test]
    [Arguments(1)]
    [Arguments(2, Skip = "Skipping value 2")]
    [Arguments(3)]
    public async Task Test1(int value)
    {
        await Assert.That(value).IsEqualTo(1).Or.IsEqualTo(3);
    }
}
