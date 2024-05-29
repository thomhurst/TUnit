using TUnit.Core;

namespace TUnit.TestProject;

public class ClassDataSourceDrivenTests_Shared_Keyed
{
    [DataSourceDrivenTest]
    [ClassDataSource(typeof(SomeClass), Shared = SharedType.Keyed, Key = "🔑")]
    public void DataSource_Class(SomeClass value)
    {
    }

    [DataSourceDrivenTest]
    [ClassDataSource<SomeClass>(Shared = SharedType.Keyed, Key = "🔑")]
    public void DataSource_Class_Generic(SomeClass value)
    {
    }

    public record SomeClass
    {
        public int Value => 1;
    }
}