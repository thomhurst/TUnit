using TUnit.Core;

namespace TUnit.TestProject;

public class DataDrivenTests
{
    [DataDrivenTest]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    public void DataSource_Method(int value)
    {
    }
    
    [DataDrivenTest]
    [Arguments(1, "String")]
    [Arguments(2, "String2")]
    [Arguments(3, "String3")]
    public void DataSource_Method(int value, string value2)
    {
    }
    
    [DataDrivenTest]
    [Arguments(TestEnum.One)]
    [Arguments(TestEnum.Two)]
    [Arguments(-1)]
    public void EnumValue(TestEnum testEnum)
    {
    }
    
    [DataDrivenTest]
    [Arguments(null)]
    public void NullValue(string? value)
    {
    }
    
    [DataDrivenTest]
    [Arguments("")]
    public void EmptyString(string? value)
    {
    }
    
    [DataDrivenTest]
    [Arguments("Foo bar!")]
    public void NonEmptyString(string? value)
    {
    }
    
    [DataDrivenTest]
    [Arguments(null)]
    [Arguments(false)]
    [Arguments(true)]
    public void BooleanString(bool? value)
    {
    }
}