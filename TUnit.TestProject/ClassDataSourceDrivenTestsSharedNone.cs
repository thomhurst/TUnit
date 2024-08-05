using TUnit.Core;
using TUnit.TestProject.Dummy;

namespace TUnit.TestProject;

public class ClassDataSourceDrivenTestsSharedNone
{
    [DataSourceDrivenTest]
    [ClassDataSource<SomeAsyncDisposableClass>(Shared = SharedType.None)]
    public void DataSource_Class(SomeAsyncDisposableClass value)
    {
        // Dummy method
    }

    [DataSourceDrivenTest]
    [ClassDataSource<SomeAsyncDisposableClass>(Shared = SharedType.None)]
    public void DataSource_Class_Generic(SomeAsyncDisposableClass value)
    {
        // Dummy method
    }
}