using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4065;

public enum MyEnum
{
    One,
    Two,
    Three
}

[EngineTest(ExpectedResult.Pass)]
public class BugRepro4065
{
    [Test]
    [Arguments((MyEnum[])[MyEnum.One, MyEnum.Two])]
    public async Task EnumArrayWithCollectionExpression(MyEnum[] values)
    {
        await Assert.That(values.Length).IsEqualTo(2);
        await Assert.That(values[0]).IsEqualTo(MyEnum.One);
        await Assert.That(values[1]).IsEqualTo(MyEnum.Two);
    }

    [Test]
    [Arguments((MyEnum[])[MyEnum.Three])]
    public async Task EnumArraySingleElement(MyEnum[] values)
    {
        await Assert.That(values.Length).IsEqualTo(1);
        await Assert.That(values[0]).IsEqualTo(MyEnum.Three);
    }

    [Test]
    [Arguments(new MyEnum[] { MyEnum.One, MyEnum.Two })] // Old syntax (should still work)
    public async Task EnumArrayWithNewSyntax(MyEnum[] values)
    {
        await Assert.That(values.Length).IsEqualTo(2);
    }
}
