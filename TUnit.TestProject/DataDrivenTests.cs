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
        // Dummy method
    }
    
    [DataDrivenTest]
    [Arguments(1, "String")]
    [Arguments(2, "String2")]
    [Arguments(3, "String3")]
    public void DataSource_Method(int value, string value2)
    {
        // Dummy method
    }
    
    [DataDrivenTest]
    [Arguments(TestEnum.One)]
    [Arguments(TestEnum.Two)]
    [Arguments(-1)]
    public void EnumValue(TestEnum testEnum)
    {
        // Dummy method
    }
    
    [DataDrivenTest]
    [Arguments(null)]
    public void NullValue(string? value)
    {
        // Dummy method
    }
    
    [DataDrivenTest]
    [Arguments("")]
    public void EmptyString(string? value)
    {
        // Dummy method
    }
    
    [DataDrivenTest]
    [Arguments("Foo bar!")]
    public void NonEmptyString(string? value)
    {
        // Dummy method
    }
    
    [DataDrivenTest]
    [Arguments(null)]
    [Arguments(false)]
    [Arguments(true)]
    public void BooleanString(bool? value)
    {
        // Dummy method
    }
}