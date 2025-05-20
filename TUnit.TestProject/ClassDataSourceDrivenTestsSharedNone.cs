using TUnit.TestProject.Library.Models;

namespace TUnit.TestProject;

public class ClassDataSourceDrivenTestsSharedNone
{
    [Test]
    [ClassDataSource<SomeAsyncDisposableClass>(Shared = SharedType.None)]
    public void DataSource_Class(SomeAsyncDisposableClass value)
    {
        // Dummy method
    }

    [Test]
    [ClassDataSource<SomeAsyncDisposableClass>(Shared = SharedType.None)]
    public void DataSource_Class_Generic(SomeAsyncDisposableClass value)
    {
        // Dummy method
    }
}