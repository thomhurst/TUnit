using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7076;

[EngineTest(ExpectedResult.Pass)]
public class ArgumentsRowSkipTests
{
    [Test]
    [Arguments("a")]
    [Arguments("b")]
    [Arguments("c", Skip = "skip only c")]
    public async Task OnlyMarkedRowIsSkipped(string value)
    {
        await Assert.That(value).IsNotEqualTo("c");
    }

    [Test]
    [Arguments(1, DisplayName = "custom name for 1")]
    [Arguments(2)]
    public async Task DisplayNameOnlyAppliesToItsOwnRow(int value)
    {
        var displayName = TestContext.Current!.Metadata.DisplayName;

        if (value == 1)
        {
            await Assert.That(displayName).IsEqualTo("custom name for 1");
        }
        else
        {
            await Assert.That(displayName).IsNotEqualTo("custom name for 1");
        }
    }
}
