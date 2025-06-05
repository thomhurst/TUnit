using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class DataDrivenTests
{
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    public void DataSource_Method(int value)
    {
        // Dummy method
    }

    [Test]
    [Arguments(1, "String")]
    [Arguments(2, "String2")]
    [Arguments(3, "String3")]
    public void DataSource_Method(int value, string value2)
    {
        // Dummy method
    }

    [Test]
    [Arguments(TestEnum.One)]
    [Arguments(TestEnum.Two)]
    [Arguments(-1)]
    public void EnumValue(TestEnum testEnum)
    {
        // Dummy method
    }

    [Test]
    [Arguments(null)]
    public void NullValue(string? value)
    {
        // Dummy method
    }

    [Test]
    [Arguments("")]
    public void EmptyString(string? value)
    {
        // Dummy method
    }

    [Test]
    [Arguments("Foo bar!")]
    public void NonEmptyString(string? value)
    {
        // Dummy method
    }

    [Test]
    [Arguments(null)]
    [Arguments(false)]
    [Arguments(true)]
    public void BooleanString(bool? value)
    {
        // Dummy method
    }

    [Test]
    [Arguments(typeof(object))]
    public void Type(Type value)
    {
        // Dummy method
    }

    [Test]
    [Arguments(new[] { 1, 2, 3 })]
    public void IntegerArray(int[] values)
    {
        // Dummy method
    }

    [Test]
    [Arguments(int.MaxValue)]
    public void IntMaxValue(int value)
    {
        // Dummy method
    }
}