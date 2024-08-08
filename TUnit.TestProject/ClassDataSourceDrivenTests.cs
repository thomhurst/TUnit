using TUnit.TestProject.Dummy;

namespace TUnit.TestProject;

public class ClassDataSourceDrivenTests
{
    [Test]
    [ClassDataSource<SomeAsyncDisposableClass>]
    public void DataSource_Class(SomeAsyncDisposableClass value)
    {
        // Dummy method
    }

    [Test]
    [ClassDataSource<SomeAsyncDisposableClass>]
    public void DataSource_Class_Generic(SomeAsyncDisposableClass value)
    {
        // Dummy method
    }
}