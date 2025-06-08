using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class NestedClassDataSourceDrivenTests2
{
    [Test]
    [MyDataProvider]
    public async Task DataSource_Class(SomeClass1 value)
    {
        Console.WriteLine(value);

        await Assert.That(value.IsInitialized).IsTrue();
        await Assert.That(value.InnerClass.IsInitialized).IsTrue();
        await Assert.That(value.InnerClass.InnerClass.IsInitialized).IsTrue();

        await Assert.That(value.IsDisposed).IsFalse();
        await Assert.That(value.InnerClass.IsDisposed).IsFalse();
        await Assert.That(value.InnerClass.InnerClass.IsDisposed).IsFalse();
    }

    [method: SetsRequiredMembers]
    public class MyDataProvider() : DataSourceGeneratorAttribute<SomeClass1>
    {
        [ClassDataSource<SomeClass1>]
        public required SomeClass1 InnerClass { get; init; }

        public override IEnumerable<Func<SomeClass1>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
        {
            yield return () => InnerClass;
        }
    }

    public record SomeClass1 : IAsyncInitializer, IAsyncDisposable
    {
        [ClassDataSource<SomeClass2>(Shared = SharedType.PerAssembly)]
        public required SomeClass2 InnerClass { get; init; }

        public async Task InitializeAsync()
        {
            await Assert.That(InnerClass.IsInitialized).IsTrue();
            IsInitialized = true;
        }

        public bool IsInitialized
        {
            get;
            private set;
        }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return default;
        }

        public bool IsDisposed { get; set; }
    }

    public record SomeClass2 : IAsyncInitializer, IAsyncDisposable
    {
        [ClassDataSource<SomeClass3>(Shared = SharedType.PerAssembly)]
        public required SomeClass3 InnerClass { get; init; }

        public async Task InitializeAsync()
        {
            await Assert.That(InnerClass.IsInitialized).IsTrue();
            IsInitialized = true;
        }

        public bool IsInitialized
        {
            get;
            private set;
        }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return default;
        }

        public bool IsDisposed { get; set; }
    }

    public record SomeClass3 : IAsyncInitializer, IAsyncDisposable
    {
        public Task InitializeAsync()
        {
            IsInitialized = true;
            return Task.CompletedTask;
        }

        public bool IsInitialized
        {
            get;
            private set;
        }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return default;
        }

        public bool IsDisposed { get; set; }
    }
}
