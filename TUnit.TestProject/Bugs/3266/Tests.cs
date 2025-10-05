using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs.Bug3266;

// Mock test container - shared per test session
public class PulsarTestContainer : IAsyncInitializer, IAsyncDisposable
{
    public bool IsInitialized { get; private set; }
    public bool IsDisposed { get; private set; }

    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return default;
    }
}

// Mock connection class - depends on PulsarTestContainer being initialized
public class PulsarConnection : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<PulsarTestContainer>(Shared = SharedType.PerTestSession)]
    public required PulsarTestContainer Container { get; init; }

    public bool IsInitialized { get; private set; }
    public bool IsDisposed { get; private set; }

    public Task InitializeAsync()
    {
        // This should fail if Container.InitializeAsync() hasn't been called yet
        if (!Container.IsInitialized)
        {
            throw new InvalidOperationException(
                "PulsarConnection.InitializeAsync() called before nested Container property was initialized!");
        }

        IsInitialized = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return default;
    }
}

// Mock web app factory - also depends on PulsarTestContainer
public class WebAppFactory : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<PulsarTestContainer>(Shared = SharedType.PerTestSession)]
    public required PulsarTestContainer Container { get; init; }

    public bool IsInitialized { get; private set; }
    public bool IsDisposed { get; private set; }

    public Task InitializeAsync()
    {
        // This should fail if Container.InitializeAsync() hasn't been called yet
        if (!Container.IsInitialized)
        {
            throw new InvalidOperationException(
                "WebAppFactory.InitializeAsync() called before nested Container property was initialized!");
        }

        IsInitialized = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return default;
    }
}

// Base abstract class with WebAppFactory property
public abstract class AbstractClassA
{
    [ClassDataSource<WebAppFactory>(Shared = SharedType.None)]
    public required WebAppFactory WebApp { get; init; }
}

// Middle abstract class with PulsarConnection property
public abstract class AbstractClassB : AbstractClassA
{
    [ClassDataSource<PulsarConnection>(Shared = SharedType.None)]
    public required PulsarConnection Connection { get; init; }
}

// Concrete test class - reproduces issue #3266
[EngineTest(ExpectedResult.Pass)]
[NotInParallel]
public class Issue3266ReproTest : AbstractClassB
{
    [Test]
    public async Task NestedPropertiesShouldBeInitializedBeforeParentInitializeAsync()
    {
        // Verify all nested containers are initialized
        await Assert.That(Connection.Container.IsInitialized)
            .IsTrue()
            .Because("PulsarConnection's Container should be initialized");

        await Assert.That(WebApp.Container.IsInitialized)
            .IsTrue()
            .Because("WebAppFactory's Container should be initialized");

        // Verify parent objects are initialized
        await Assert.That(Connection.IsInitialized)
            .IsTrue()
            .Because("PulsarConnection should be initialized");

        await Assert.That(WebApp.IsInitialized)
            .IsTrue()
            .Because("WebAppFactory should be initialized");
    }

    [Test]
    public async Task BothPropertiesShouldShareTheSameContainer()
    {
        // Since both use SharedType.PerTestSession, they should get the same instance
        await Assert.That(Connection.Container)
            .IsSameReferenceAs(WebApp.Container)
            .Because("Both properties should share the same PulsarTestContainer instance");
    }
}
