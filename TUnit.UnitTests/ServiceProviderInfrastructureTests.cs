using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Core;
using TUnit.Core.Services;

namespace TUnit.UnitTests;

/// <summary>
/// Tests for service provider infrastructure and dependency injection
/// </summary>
public class ServiceProviderInfrastructureTests
{
    [Test]
    public async Task TestServiceProvider_CanRegisterAndResolveService()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var testService = new TestService { Name = "Test Service" };

        // Act
        serviceProvider.AddSingleton<ITestService>(testService);
        var resolvedService = serviceProvider.GetService<ITestService>();

        // Assert
        await Assert.That(resolvedService).IsNotNull();
        await Assert.That(resolvedService).IsEqualTo(testService);
        await Assert.That(resolvedService!.Name).IsEqualTo("Test Service");
    }

    [Test]
    public async Task TestServiceProvider_CanRegisterFactory()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var factoryCallCount = 0;

        // Act
        serviceProvider.AddTransient<ITestService>(() =>
        {
            factoryCallCount++;
            return new TestService { Name = $"Factory Service {factoryCallCount}" };
        });

        var service1 = serviceProvider.GetService<ITestService>();
        var service2 = serviceProvider.GetService<ITestService>();

        // Assert
        await Assert.That(service1).IsNotNull();
        await Assert.That(service2).IsNotNull();
        await Assert.That(service1!.Name).IsEqualTo("Factory Service 1");
        await Assert.That(service2!.Name).IsEqualTo("Factory Service 2");
        await Assert.That(factoryCallCount).IsEqualTo(2);
    }

    [Test]
    public async Task TestServiceProvider_GetRequiredService_ThrowsWhenNotRegistered()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();

        // Act & Assert
        await Assert.That(() => serviceProvider.GetRequiredService<ITestService>())
            .Throws<InvalidOperationException>()
            .WithMessageContaining("Service of type")
            .And.HasMessageContaining("ITestService")
            .And.HasMessageContaining("is not registered");
    }

    [Test]
    public async Task TestServiceProvider_GetService_ReturnsNullWhenNotRegistered()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();

        // Act
        var service = serviceProvider.GetService<ITestService>();

        // Assert
        await Assert.That(service).IsNull();
    }

    [Test]
    public async Task TestServiceProvider_SupportsInterfaceAndConcreteTypes()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var concreteService = new TestService { Name = "Concrete" };

        // Act
        serviceProvider.AddSingleton<TestService>(concreteService);
        serviceProvider.AddSingleton<ITestService>(concreteService);

        var resolvedConcrete = serviceProvider.GetService<TestService>();
        var resolvedInterface = serviceProvider.GetService<ITestService>();

        // Assert
        await Assert.That(resolvedConcrete).IsEqualTo(concreteService);
        await Assert.That(resolvedInterface).IsEqualTo(concreteService);
        await Assert.That(resolvedConcrete).IsEqualTo(resolvedInterface);
    }

    [Test]
    public async Task TestContext_HasWorkingServiceProvider()
    {
        // Arrange & Act
        var context = TestContext.Current;
        var serviceProvider = context?.ServiceProvider;

        // Assert
        await Assert.That(context).IsNotNull();
        await Assert.That(serviceProvider).IsNotNull();
    }

    [Test]
    public async Task TestServiceProvider_CanOverrideRegistration()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var firstService = new TestService { Name = "First" };
        var secondService = new TestService { Name = "Second" };

        // Act
        serviceProvider.AddSingleton<ITestService>(firstService);
        serviceProvider.AddSingleton<ITestService>(secondService);
        var resolvedService = serviceProvider.GetService<ITestService>();

        // Assert
        await Assert.That(resolvedService).IsEqualTo(secondService);
        await Assert.That(resolvedService!.Name).IsEqualTo("Second");
    }

    [Test]
    public async Task TestServiceProvider_HandlesMultipleServiceTypes()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var testService = new TestService { Name = "Test" };
        var otherService = new OtherService { Value = 42 };

        // Act
        serviceProvider.AddSingleton<ITestService>(testService);
        serviceProvider.AddSingleton<IOtherService>(otherService);

        var resolvedTest = serviceProvider.GetService<ITestService>();
        var resolvedOther = serviceProvider.GetService<IOtherService>();

        // Assert
        await Assert.That(resolvedTest).IsNotNull();
        await Assert.That(resolvedOther).IsNotNull();
        await Assert.That(resolvedTest!.Name).IsEqualTo("Test");
        await Assert.That(resolvedOther!.Value).IsEqualTo(42);
    }

    // Test service interfaces and implementations
    public interface ITestService
    {
        string Name { get; set; }
    }

    public class TestService : ITestService
    {
        public string Name { get; set; } = "";
    }

    public interface IOtherService
    {
        int Value { get; set; }
    }

    public class OtherService : IOtherService
    {
        public int Value { get; set; }
    }
}
