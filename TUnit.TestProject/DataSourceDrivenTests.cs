using TUnit.Core;

namespace TUnit.TestProject;

public class DataSourceDrivenTests
{
    [DataSourceDrivenTest]
    [MethodDataSource(nameof(SomeMethod))]
    public void DataSource_Method(int value)
    {
    }
    
    [DataSourceDrivenTest]
    [ClassDataSource(typeof(SomeClass))]
    public void DataSource_Class(SomeClass value)
    {
    }

    public static int SomeMethod() => 1;

    public record SomeClass
    {
        public int Value => 1;
    }
}