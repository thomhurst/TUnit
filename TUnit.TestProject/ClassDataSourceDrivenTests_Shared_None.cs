using TUnit.Core;

namespace TUnit.TestProject;

public class ClassDataSourceDrivenTests_Shared_None
{
    [DataSourceDrivenTest]
    [ClassDataSource(typeof(SomeClass), Shared = SharedType.None)]
    public void DataSource_Class(SomeClass value)
    {
        // Dummy method
    }

    [DataSourceDrivenTest]
    [ClassDataSource<SomeClass>(Shared = SharedType.None)]
    public void DataSource_Class_Generic(SomeClass value)
    {
        // Dummy method
    }

    public record SomeClass
    {
        public int Value => 1;
    }
}