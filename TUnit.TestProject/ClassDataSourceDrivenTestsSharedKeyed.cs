using TUnit.Core;
using TUnit.TestProject.Dummy;

namespace TUnit.TestProject;

public class ClassDataSourceDrivenTestsSharedKeyed
{
    [DataSourceDrivenTest]
    [ClassDataSource(typeof(SomeAsyncDisposableClass), Shared = SharedType.Keyed, Key = "🔑")]
    public void DataSource_Class(SomeAsyncDisposableClass value)
    {
        // Dummy method
    }

    [DataSourceDrivenTest]
    [ClassDataSource<SomeAsyncDisposableClass>(Shared = SharedType.Keyed, Key = "🔑")]
    public void DataSource_Class_Generic(SomeAsyncDisposableClass value)
    {
        // Dummy method
    }
}