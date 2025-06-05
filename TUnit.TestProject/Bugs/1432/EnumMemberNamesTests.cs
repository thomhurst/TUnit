using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._1432;

[EngineTest(ExpectedResult.Pass)]
public class EnumMemberNamesTests
{
    [Test]
    [Arguments(nameof(SomeEnum.A))]
    [Arguments(nameof(SomeEnum.B))]
    [Arguments(nameof(SomeEnum.C))]
    public void SomeTest(string value)
    {
    }

    public enum SomeEnum
    {
        A, B, C
    }
}