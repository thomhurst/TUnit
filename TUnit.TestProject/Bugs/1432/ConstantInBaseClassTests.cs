using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._1432;

[EngineTest(ExpectedResult.Pass)]
internal abstract class ConstantInBaseClassTestsBase
{
    protected const string BaseValue = "Value";
}

internal class ConstantInBaseClassTests : ConstantInBaseClassTestsBase
{
    [Test]
    [Arguments(BaseValue)]
    public async Task SomeTest(string value)
    {
        await Assert.That(value).IsEqualTo(value);
    }
}