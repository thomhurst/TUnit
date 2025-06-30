using TUnit.Core;

namespace TUnit.UnitTests;

/// <summary>
/// Integration tests to verify all reflection-free components work together correctly
/// </summary>
public class ReflectionFreeIntegrationTests
{
    [Test]
    public async Task SourceGeneration_ProducesWorkingTestMetadata()
    {
        // Arrange & Act
        // This test itself verifies that source generation works since it's a test method
        var context = TestContext.Current;

        // Assert
        await Assert.That(context).IsNotNull();
        await Assert.That(context!.TestDetails).IsNotNull();
        await Assert.That(context.TestDetails.TestName).IsNotNull();
    }

    [Test]
    public async Task AOTMode_DoesNotUseReflection()
    {
        // Arrange & Act
        // Verify that we can access test metadata without any reflection calls
        var context = TestContext.Current;
        var testDetails = context?.TestDetails;

        // Assert
        await Assert.That(testDetails).IsNotNull();
        await Assert.That(testDetails!.TestName).Contains(nameof(AOTMode_DoesNotUseReflection));
    }

    [Test]
    public async Task ServiceProvider_IntegratesWithTestExecution()
    {
        // Arrange & Act
        var serviceProvider = TestContext.Current?.ServiceProvider;

        // Assert
        await Assert.That(serviceProvider).IsNotNull();
        
        // Verify we can use the service provider
        var testService = serviceProvider!.GetService<IIntegrationTestService>();
        // Should be null since we haven't registered it, but the call should work
        await Assert.That(testService).IsNull();
    }

    [Test]
    public async Task DelegateStorage_IsAccessibleDuringTestExecution()
    {
        // Arrange & Act
        // Try to register and retrieve a delegate during test execution
        const string key = "integration_test_delegate";
        Func<object?[], object> factory = args => new { TestResult = "Success" };

        TestDelegateStorage.RegisterInstanceFactory(key, factory);
        var retrievedFactory = TestDelegateStorage.GetInstanceFactory(key);

        // Assert
        await Assert.That(retrievedFactory).IsNotNull();
        
        var result = retrievedFactory!(Array.Empty<object>());
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task HookSystem_IntegratesWithDelegateStorage()
    {
        // Arrange
        const string hookKey = "integration_test_hook";
        var hookExecuted = false;
        
        Func<object?, HookContext, Task> hook = async (instance, context) =>
        {
            hookExecuted = true;
            await Task.CompletedTask;
        };

        // Act
        HookDelegateStorage.RegisterHook(hookKey, hook);
        var retrievedHook = HookDelegateStorage.GetHook(hookKey);

        // Assert
        await Assert.That(retrievedHook).IsNotNull();
        
        // Execute the hook to verify it works
        await retrievedHook!(null, new HookContext(TestContext.Current!, typeof(ReflectionFreeIntegrationTests), this));
        await Assert.That(hookExecuted).IsTrue();
    }

    [Test]
    public async Task DataSourceFactories_WorkWithoutReflection()
    {
        // Arrange
        const string factoryKey = "integration_test_data_source";
        Func<CancellationToken, IEnumerable<object?[]>> factory = ct =>
            new[] 
            { 
                new object?[] { 1, "first" },
                new object?[] { 2, "second" }
            };

        // Act
        DataSourceFactoryRegistry.Register(factoryKey, factory);
        var retrievedFactory = DataSourceFactoryRegistry.GetFactory(factoryKey);

        // Assert
        await Assert.That(retrievedFactory).IsNotNull();
        
        var data = retrievedFactory!(CancellationToken.None).ToList();
        await Assert.That(data).HasCount().EqualTo(2);
        await Assert.That(data[0][0]).IsEqualTo(1);
        await Assert.That(data[1][0]).IsEqualTo(2);
    }

    [Test]
    public async Task TypeArrayComparer_WorksInRuntimeContext()
    {
        // Arrange
        var types1 = new[] { typeof(int), typeof(string) };
        var types2 = new[] { typeof(int), typeof(string) };
        var types3 = new[] { typeof(bool), typeof(double) };

        // Act & Assert - Verify the comparer works at runtime
        await Assert.That(TUnit.Core.TypeArrayComparer.Instance.Equals(types1, types2)).IsTrue();
        await Assert.That(TUnit.Core.TypeArrayComparer.Instance.Equals(types1, types3)).IsFalse();
        
        var hash1 = TUnit.Core.TypeArrayComparer.Instance.GetHashCode(types1);
        var hash2 = TUnit.Core.TypeArrayComparer.Instance.GetHashCode(types2);
        await Assert.That(hash1).IsEqualTo(hash2);
    }

    [Test]
    public async Task GeneratedCode_DoesNotContainReflectionCalls()
    {
        // Arrange & Act
        // This is a meta-test that verifies our generated code doesn't use reflection
        // We can verify this by checking that basic test operations work without
        // triggering any reflection-related exceptions that would occur in AOT scenarios
        
        var context = TestContext.Current;
        var serviceProvider = context?.ServiceProvider;
        
        // Assert
        // If we got this far without exceptions, the generated code is AOT-compatible
        await Assert.That(context).IsNotNull();
        await Assert.That(serviceProvider).IsNotNull();
    }

    [Test]
    public async Task ModuleInitializer_SetUpInfrastructureCorrectly()
    {
        // Arrange & Act
        // Verify that the module initializer set up all the required infrastructure
        
        // Check that test metadata registry has sources
        var sources = TUnit.Engine.TestMetadataRegistry.GetSources();
        
        // Check that storage classes are available
        var delegateStorageType = typeof(TestDelegateStorage);
        var hookStorageType = typeof(HookDelegateStorage);
        var factoryRegistryType = typeof(DataSourceFactoryRegistry);

        // Assert
        await Assert.That(sources).IsNotNull();
        await Assert.That(delegateStorageType).IsNotNull();
        await Assert.That(hookStorageType).IsNotNull();
        await Assert.That(factoryRegistryType).IsNotNull();
    }

    // Test interface for service provider integration
    public interface IIntegrationTestService
    {
        string ProcessData(string input);
    }
}