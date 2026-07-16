using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._1603;

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    [Test]
    [Arguments((short) -123)]
    public async Task Casted_Integer_To_Short_Converts(short value)
    {
        await Assert.That(value).IsEqualTo((short) -123);
    }

    [Test]
    [Arguments(-123)]
    public async Task Integer_To_Short_Converts(short value)
    {
        await Assert.That(value).IsEqualTo((short) -123);
    }
}
