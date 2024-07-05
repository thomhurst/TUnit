using TUnit.Core;
using TUnit.TestProject.Dummy;

namespace TUnit.TestProject;

public class ClassDataSourceDrivenTests
{
    [DataSourceDrivenTest]
    [ClassDataSource(typeof(SomeAsyncDisposableClass))]
    public void DataSource_Class(SomeAsyncDisposableClass value)
    {
        // Dummy method
    }

    [DataSourceDrivenTest]
    [ClassDataSource<SomeAsyncDisposableClass>]
    public void DataSource_Class_Generic(SomeAsyncDisposableClass value)
    {
        // Dummy method
    }
}