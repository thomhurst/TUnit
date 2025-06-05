using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class NestedClassDataSourceDrivenTests
{
    [Test]
    [ClassDataSource<SomeClass1>(Shared = SharedType.PerClass)]
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

    [Test]
    [ClassDataSource<SomeClass1>(Shared = SharedType.PerClass)]
    [DependsOn(nameof(DataSource_Class))]
    public async Task DataSource_Class2(SomeClass1 value)
    {
        Console.WriteLine(value);

        await Assert.That(value.IsInitialized).IsTrue();
        await Assert.That(value.InnerClass.IsInitialized).IsTrue();
        await Assert.That(value.InnerClass.InnerClass.IsInitialized).IsTrue();

        await Assert.That(value.IsDisposed).IsFalse();
        await Assert.That(value.InnerClass.IsDisposed).IsFalse();
        await Assert.That(value.InnerClass.InnerClass.IsDisposed).IsFalse();
    }

    [Test]
    [ClassDataSource<SomeClass1>(Shared = SharedType.PerClass)]
    [DependsOn(nameof(DataSource_Class2))]
    public async Task DataSource_Class3(SomeClass1 value)
    {
        Console.WriteLine(value);

        await Assert.That(value.IsInitialized).IsTrue();
        await Assert.That(value.InnerClass.IsInitialized).IsTrue();
        await Assert.That(value.InnerClass.InnerClass.IsInitialized).IsTrue();

        await Assert.That(value.IsDisposed).IsFalse();
        await Assert.That(value.InnerClass.IsDisposed).IsFalse();
        await Assert.That(value.InnerClass.InnerClass.IsDisposed).IsFalse();
    }

    public record SomeClass1 : IAsyncInitializer, IAsyncDisposable
    {
        [ClassDataSource<SomeClass2>(Shared = SharedType.PerAssembly)]
        public required SomeClass2 InnerClass { get; init; }

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

    public record SomeClass2 : IAsyncInitializer, IAsyncDisposable
    {
        [ClassDataSource<SomeClass3>(Shared = SharedType.PerAssembly)]
        public required SomeClass3 InnerClass { get; init; }

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
