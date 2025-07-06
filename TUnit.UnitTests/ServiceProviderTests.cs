using TUnit.Core.Services;

namespace TUnit.UnitTests;

/// <summary>
/// Tests to verify that the service provider infrastructure is working
/// </summary>
public class ServiceProviderTests
{
    [Test]
    public async Task TestContext_HasServiceProvider()
    {
        // Arrange & Act
        var context = TestContext.Current;

        // Assert
        await Assert.That(context).IsNotNull();
        await Assert.That(context!.ServiceProvider).IsNotNull();
    }

    [Test]
    public async Task ServiceProvider_IsTestServiceProvider()
    {
        // Arrange
        var context = TestContext.Current;

        // Act
        var serviceProvider = context!.ServiceProvider;

        // Assert
        await Assert.That(serviceProvider).IsNotNull();
        await Assert.That(serviceProvider).IsTypeOf<TestServiceProvider>();
    }

    [Test]
    public async Task ServiceProvider_CanResolveItself()
    {
        // Arrange
        var context = TestContext.Current;
        var serviceProvider = context!.ServiceProvider;

        // Act
        var resolvedProvider = serviceProvider!.GetService(typeof(IServiceProvider));

        // Assert
        await Assert.That(resolvedProvider).IsNotNull();
        await Assert.That(resolvedProvider).IsSameReferenceAs(serviceProvider);
    }

    [Test]
    public async Task ServiceProvider_ReturnsNullForUnregisteredService()
    {
        // Arrange
        var context = TestContext.Current;
        var serviceProvider = context!.ServiceProvider;

        // Act
        var service = serviceProvider!.GetService(typeof(ITestService));

        // Assert
        await Assert.That(service).IsNull();
    }

    [Test]
    public async Task GetRequiredService_ThrowsForUnregisteredService()
    {
        // Arrange
        var context = TestContext.Current;
        var serviceProvider = context!.ServiceProvider;

        // Act & Assert
        Exception? thrownException = null;
        try
        {
            ServiceProviderExtensions.GetRequiredService<ITestService>(serviceProvider!);
        }
        catch (Exception ex)
        {
            thrownException = ex;
        }

        await Assert.That(thrownException).IsNotNull();
        await Assert.That(thrownException).IsTypeOf<InvalidOperationException>();
        await Assert.That(thrownException!.Message).Contains("ITestService");
    }
}
