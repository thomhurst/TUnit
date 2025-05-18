using TUnit.TestProject.Library.Models;

namespace TUnit.TestProject;

public class ClassDataSourceDrivenTestsSharedForClass
{
    [Test]
    [ClassDataSource<SomeAsyncDisposableClass>(Shared = SharedType.PerClass)]
    public void DataSource_Class(SomeAsyncDisposableClass value)
    {
        // Dummy method
    }

    [Test]
    [ClassDataSource<SomeAsyncDisposableClass>(Shared = SharedType.PerClass)]
    public void DataSource_Class_Generic(SomeAsyncDisposableClass value)
    {
        // Dummy method
    }
}