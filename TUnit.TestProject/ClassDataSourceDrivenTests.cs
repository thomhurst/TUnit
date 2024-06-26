using TUnit.Core;

namespace TUnit.TestProject;

public class ClassDataSourceDrivenTests
{
    [DataSourceDrivenTest]
    [ClassDataSource(typeof(SomeClass))]
    public void DataSource_Class(SomeClass value)
    {
        // Dummy method
    }

    [DataSourceDrivenTest]
    [ClassDataSource<SomeClass>]
    public void DataSource_Class_Generic(SomeClass value)
    {
        // Dummy method
    }

    public record SomeClass
    {
        public int Value => 1;
    }
}