using TUnit.Core;

namespace TUnit.UnitTests;

/// <summary>
/// Tests for delegate generation and strongly-typed storage functionality
/// </summary>
public class DelegateGenerationTests
{
    [Test]
    public async Task TestDelegateStorage_CanRegisterInstanceFactory()
    {
        // Arrange
        const string key = "test_instance_factory";
        Func<object?[], object> factory = args => new TestClass();

        // Act
        TestDelegateStorage.RegisterInstanceFactory(key, factory);
        var retrievedFactory = TestDelegateStorage.GetInstanceFactory(key);

        // Assert
        await Assert.That(retrievedFactory).IsNotNull();
        await Assert.That(retrievedFactory).IsEqualTo(factory);
    }

    [Test]
    public async Task TestDelegateStorage_CanRegisterTestInvoker()
    {
        // Arrange
        const string key = "test_invoker";
        Func<object, object?[], Task> invoker = async (instance, args) => 
        {
            await Task.CompletedTask;
        };

        // Act
        TestDelegateStorage.RegisterTestInvoker(key, invoker);
        var retrievedInvoker = TestDelegateStorage.GetTestInvoker(key);

        // Assert
        await Assert.That(retrievedInvoker).IsNotNull();
        await Assert.That(retrievedInvoker).IsEqualTo(invoker);
    }

    [Test]
    public async Task TestDelegateStorage_GetNonExistentFactory_ReturnsNull()
    {
        // Arrange
        const string key = "non_existent_factory";

        // Act
        var factory = TestDelegateStorage.GetInstanceFactory(key);

        // Assert
        await Assert.That(factory).IsNull();
    }

    [Test]
    public async Task TestDelegateStorage_GetNonExistentInvoker_ReturnsNull()
    {
        // Arrange
        const string key = "non_existent_invoker";

        // Act
        var invoker = TestDelegateStorage.GetTestInvoker(key);

        // Assert
        await Assert.That(invoker).IsNull();
    }

    [Test]
    public async Task HookDelegateStorage_CanRegisterHook()
    {
        // Arrange
        const string key = "test_hook";
        Func<object?, HookContext, Task> hook = async (instance, context) => 
        {
            await Task.CompletedTask;
        };

        // Act
        HookDelegateStorage.RegisterHook(key, hook);
        var retrievedHook = HookDelegateStorage.GetHook(key);

        // Assert
        await Assert.That(retrievedHook).IsNotNull();
        await Assert.That(retrievedHook).IsEqualTo(hook);
    }

    [Test]
    public async Task HookDelegateStorage_GetNonExistentHook_ReturnsNull()
    {
        // Arrange
        const string key = "non_existent_hook";

        // Act
        var hook = HookDelegateStorage.GetHook(key);

        // Assert
        await Assert.That(hook).IsNull();
    }

    [Test]
    public async Task DataSourceFactoryRegistry_CanRegisterFactory()
    {
        // Arrange
        const string key = "test_data_source";
        Func<CancellationToken, IEnumerable<object?[]>> factory = ct => 
            new[] { new object?[] { 1, "test" }, new object?[] { 2, "example" } };

        // Act
        DataSourceFactoryRegistry.Register(key, factory);
        var retrievedFactory = DataSourceFactoryRegistry.GetFactory(key);

        // Assert
        await Assert.That(retrievedFactory).IsNotNull();
        await Assert.That(retrievedFactory).IsEqualTo(factory);
    }

    [Test]
    public async Task DataSourceFactoryRegistry_GetNonExistentFactory_ReturnsNull()
    {
        // Arrange
        const string key = "non_existent_data_source";

        // Act
        var factory = DataSourceFactoryRegistry.GetFactory(key);

        // Assert
        await Assert.That(factory).IsNull();
    }

    [Test]
    public async Task DataSourceFactoryRegistry_FactoryCanProduceData()
    {
        // Arrange
        const string key = "test_data_producer";
        Func<CancellationToken, IEnumerable<object?[]>> factory = ct => 
            new[] { new object?[] { 42, "test_value" } };

        DataSourceFactoryRegistry.Register(key, factory);
        var retrievedFactory = DataSourceFactoryRegistry.GetFactory(key);

        // Act
        var data = retrievedFactory!(CancellationToken.None).ToList();

        // Assert
        await Assert.That(data).HasCount().EqualTo(1);
        await Assert.That(data[0]).HasCount().EqualTo(2);
        await Assert.That(data[0][0]).IsEqualTo(42);
        await Assert.That(data[0][1]).IsEqualTo("test_value");
    }

    private class TestClass
    {
        public string TestProperty { get; set; } = "test";
    }
}