using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class PropertyArrayArgumentsTests
{
    // This might work if Arguments processes single string
    [Arguments("single string")]
    public string StringProperty { get; set; } = null!;

    [Test]
    public async Task TestArrayProperties()
    {
        await Assert.That(StringProperty).IsEqualTo("single string");
    }
}