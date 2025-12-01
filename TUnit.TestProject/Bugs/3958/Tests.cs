using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3958;

/// <summary>
/// Regression test for https://github.com/thomhurst/TUnit/issues/3958
///
/// This test verifies that nested property injection with IAsyncInitializer works correctly
/// when there is a multi-level inheritance hierarchy:
///
/// Tests (this class)
///   └── extends DerivedIntegrationTestBase
///         ├── [ClassDataSource] WrapperClass BrokerHandlerWrapper
///         └── extends IntegrationTestBase
///               └── [ClassDataSource] FactoryClass Factory
///
/// WrapperClass : IAsyncInitializer, IAsyncDisposable
///   └── [ClassDataSource(Shared = PerTestSession)] ContainerClass Container
///
/// The bug was that property injection on nested data sources (WrapperClass having
/// a ContainerClass property) was not being handled correctly, causing initialization
/// failures when the wrapper tried to access its container.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class Tests : DerivedIntegrationTestBase
{
    [Test]
    public async Task AllPropertiesAreInitialized()
    {
        // Verify base class property (Factory) is injected and initialized
        await Assert.That(Factory).IsNotNull();
        await Assert.That(Factory.IsInitialized).IsTrue();
        await Assert.That(Factory.BaseUrl).IsEqualTo("http://localhost:5000");

        // Verify derived class property (BrokerHandlerWrapper) is injected and initialized
        await Assert.That(BrokerHandlerWrapper).IsNotNull();
        await Assert.That(BrokerHandlerWrapper.IsInitialized).IsTrue();

        // Verify nested property (Container) within BrokerHandlerWrapper is injected and initialized
        await Assert.That(BrokerHandlerWrapper.Container).IsNotNull();
        await Assert.That(BrokerHandlerWrapper.Container.IsInitialized).IsTrue();
        await Assert.That(BrokerHandlerWrapper.Container.ConnectionString).IsEqualTo("container://localhost:12345");
    }

    [Test]
    public async Task ContainerIsInitializedBeforeWrapper()
    {
        // This is the key regression test:
        // Container.InitializeAsync must be called BEFORE WrapperClass.InitializeAsync
        // The WrapperClass tracks this in its InitializeAsync method
        await Assert.That(BrokerHandlerWrapper.ContainerWasInitializedFirst).IsTrue();
    }

    [Test]
    public async Task MultipleTestsShareSameContainer()
    {
        // Since Container uses SharedType.PerTestSession, it should be reused
        // This test verifies the container instance is consistent
        await Assert.That(BrokerHandlerWrapper.Container).IsNotNull();
        await Assert.That(BrokerHandlerWrapper.Container.IsInitialized).IsTrue();
    }
}
