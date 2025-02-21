using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Interfaces;
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

    [Test]
    [ClassDataSource<InitializableClass>]
    public async Task IsInitialized_With_1_ClassDataSource(InitializableClass class1)
    {
        await Assert.That(class1.IsInitialized).IsTrue();
    }

    [Test]
    [ClassDataSource<InitializableClass, InitializableClass>]
    public async Task IsInitialized_With_2_ClassDataSources(InitializableClass class1, InitializableClass class2)
    {
        await Assert.That(class1.IsInitialized).IsTrue();
        await Assert.That(class2.IsInitialized).IsTrue();
    }

    [Test]
    [ClassDataSource<InitializableClass, InitializableClass, InitializableClass>]
    public async Task IsInitialized_With_3_ClassDataSources(InitializableClass class1, InitializableClass class2, InitializableClass class3)
    {
        await Assert.That(class1.IsInitialized).IsTrue();
        await Assert.That(class2.IsInitialized).IsTrue();
        await Assert.That(class3.IsInitialized).IsTrue();
    }

    [Test]
    [ClassDataSource<InitializableClass, InitializableClass, InitializableClass, InitializableClass>]
    public async Task IsInitialized_With_4_ClassDataSources(InitializableClass class1, InitializableClass class2, InitializableClass class3, InitializableClass class4)
    {
        await Assert.That(class1.IsInitialized).IsTrue();
        await Assert.That(class2.IsInitialized).IsTrue();
        await Assert.That(class3.IsInitialized).IsTrue();
        await Assert.That(class4.IsInitialized).IsTrue();
    }

    [Test]
    [ClassDataSource<InitializableClass, InitializableClass, InitializableClass, InitializableClass, InitializableClass>]
    public async Task IsInitialized_With_5_ClassDataSources(InitializableClass class1, InitializableClass class2, InitializableClass class3, InitializableClass class4, InitializableClass class5)
    {
        await Assert.That(class1.IsInitialized).IsTrue();
        await Assert.That(class2.IsInitialized).IsTrue();
        await Assert.That(class3.IsInitialized).IsTrue();
        await Assert.That(class4.IsInitialized).IsTrue();
        await Assert.That(class5.IsInitialized).IsTrue();
    }
}

public class InitializableClass : IAsyncInitializer
{
    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public bool IsInitialized { get; private set; }
}