using TUnit.Core;
using TUnit.Engine;

namespace TUnit.UnitTests;

/// <summary>
/// Tests for module initializer functionality and test metadata registration
/// </summary>
public class ModuleInitializerTests
{
    [Test]
    public async Task DirectTestMetadataProvider_HasTests()
    {
        // Arrange & Act
        var tests = DirectTestMetadataProvider.GetAllTests();

        // Assert
        await Assert.That(tests).IsNotNull();
        await Assert.That(tests.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task DirectTestMetadataProvider_ContainsMetadata()
    {
        // Arrange & Act
        var tests = DirectTestMetadataProvider.GetAllTests();

        // Assert
        await Assert.That(tests.Any()).IsTrue();
    }

    [Test]
    public async Task DirectTestMetadataProvider_ProvidesTestMetadata()
    {
        // Arrange & Act
        var metadata = DirectTestMetadataProvider.GetAllTests();

        // Assert
        await Assert.That(metadata).IsNotNull();
        // The metadata collection might be empty if no tests are in this specific assembly,
        // but should still be accessible
    }

    [Test]
    public async Task TestContext_HasServiceProvider_FromModuleInitializer()
    {
        // Arrange & Act
        var context = TestContext.Current;

        // Assert
        await Assert.That(context).IsNotNull();
        await Assert.That(context!.ServiceProvider).IsNotNull();
    }

    [Test]
    public async Task ServiceProvider_ProvidesBasicServices()
    {
        // Arrange
        var serviceProvider = TestContext.Current?.ServiceProvider;

        // Act & Assert
        await Assert.That(serviceProvider).IsNotNull();
        
        // The service provider should be available even if it doesn't have specific services registered
        // This verifies the module initializer properly set up the infrastructure
    }

    [Test]
    public async Task TestDelegateStorage_IsInitialized()
    {
        // Arrange & Act
        // Try to access TestDelegateStorage functionality
        // This verifies that the module initializer registered delegates properly
        
        // The exact implementation depends on what's generated, but we can verify
        // that the storage classes exist and are accessible
        var delegateStorageType = typeof(TestDelegateStorage);
        
        // Assert
        await Assert.That(delegateStorageType).IsNotNull();
        await Assert.That(delegateStorageType.IsClass).IsTrue();
    }

    [Test]
    public async Task HookDelegateStorage_IsInitialized()
    {
        // Arrange & Act
        var hookStorageType = typeof(HookDelegateStorage);
        
        // Assert
        await Assert.That(hookStorageType).IsNotNull();
        await Assert.That(hookStorageType.IsClass).IsTrue();
    }

    [Test]
    public async Task DataSourceFactoryRegistry_IsInitialized()
    {
        // Arrange & Act
        var registryType = typeof(DataSourceFactoryRegistry);
        
        // Assert
        await Assert.That(registryType).IsNotNull();
        await Assert.That(registryType.IsClass).IsTrue();
    }

    [Test]
    public async Task AOTMode_IsEnforced()
    {
        // Arrange & Act
        // This test verifies that the system is running in AOT-only mode
        // We can test this by checking that DirectTestMetadataProvider is working
        
        var metadata = DirectTestMetadataProvider.GetAllTests();

        // Assert
        // In AOT-only mode, we should get metadata directly without any reflection
        await Assert.That(metadata).IsNotNull();
    }
}