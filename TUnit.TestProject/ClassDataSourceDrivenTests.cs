using TUnit.Core;

namespace TUnit.TestProject;

public class ClassDataSourceDrivenTests
{
    [DataSourceDrivenTest]
    [ClassDataSource(typeof(SomeClass))]
    public void DataSource_Class(SomeClass value)
    {
    }

    [DataSourceDrivenTest]
    [ClassDataSource<SomeClass>]
    public void DataSource_Class_Generic(SomeClass value)
    {
    }

    public record SomeClass
    {
        public int Value => 1;
    }
}