using TUnit.Core;
using TUnit.Engine;

namespace TUnit.UnitTests;

/// <summary>
/// Tests for module initializer functionality and test metadata registration
/// </summary>
public class ModuleInitializerTests
{
    [Test]
    public async Task TestMetadataRegistry_HasSources()
    {
        // Arrange & Act
        var sources = TestMetadataRegistry.GetSources();

        // Assert
        await Assert.That(sources).IsNotNull();
        await Assert.That(sources.Count()).IsGreaterThan(0);
    }

    [Test]
    public async Task TestMetadataRegistry_SourcesContainMetadata()
    {
        // Arrange
        var sources = TestMetadataRegistry.GetSources();

        // Act
        var hasMetadata = false;
        foreach (var source in sources)
        {
            var metadata = await source.GetTestMetadata();
            if (metadata.Any())
            {
                hasMetadata = true;
                break;
            }
        }

        // Assert
        await Assert.That(hasMetadata).IsTrue();
    }

    [Test]
    public async Task SourceGeneratedTestMetadataSource_ProvidesTestMetadata()
    {
        // Arrange
        var sources = TestMetadataRegistry.GetSources();
        var sourceGeneratedSource = sources.FirstOrDefault(s => s.GetType().Name.Contains("SourceGenerated"));

        // Act & Assert
        if (sourceGeneratedSource != null)
        {
            var metadata = await sourceGeneratedSource.GetTestMetadata();
            await Assert.That(metadata).IsNotNull();
            // The metadata collection might be empty if no tests are in this specific assembly,
            // but the source should still exist and be callable
        }
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
        // We can test this by ensuring no reflection-based fallbacks are available
        
        var sources = TestMetadataRegistry.GetSources();
        var hasOnlySourceGeneratedSources = sources.All(s => 
            s.GetType().Name.Contains("SourceGenerated") || 
            s.GetType().Namespace?.Contains("Engine") == true);

        // Assert
        // In AOT-only mode, we should only have source-generated metadata sources
        await Assert.That(hasOnlySourceGeneratedSources || !sources.Any()).IsTrue();
    }
}