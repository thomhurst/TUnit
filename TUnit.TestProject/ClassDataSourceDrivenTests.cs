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

    public record SomeClass : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }
        
        public int Value => 1;

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }
}