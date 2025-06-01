using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class NullableByteArgumentTests
{
    [Test]
    [Arguments((byte)1)]
    [Arguments(null)]
    public void Test(byte? someByte)
    {
    }

    [Test]
    [Arguments((byte)1, (byte)1)]
    [Arguments((byte)1, null)]
    public void Test2(byte byte1, byte? byte2)
    {
    }
}