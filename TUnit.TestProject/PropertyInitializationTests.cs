using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

// Test that properties with IDataAttribute are initialized correctly
public class PropertyInitializationTests
{
    [TestData]
    public required InitializableProperty? TestProperty { get; set; }

    [NestedDataGenerator]
    public required PropertyWithNestedDependencies? NestedProperty { get; set; }

    [Test]
    public async Task Property_With_IDataAttribute_Should_Be_Initialized()
    {
        // TestProperty should be set during discovery and initialized during invocation
        await Assert.That(TestProperty).IsNotNull();
        await Assert.That(TestProperty!.IsInitialized).IsTrue();
    }

    [Test]
    public async Task Nested_Properties_Should_Be_Initialized_Before_Parent()
    {
        // NestedProperty and its nested properties should all be initialized
        await Assert.That(NestedProperty).IsNotNull();
        await Assert.That(NestedProperty!.IsInitialized).IsTrue();
        await Assert.That(NestedProperty.NestedDependency).IsNotNull();
        await Assert.That(NestedProperty.NestedDependency!.IsInitialized).IsTrue();
        await Assert.That(NestedProperty.NestedDependency.DeepDependency).IsNotNull();
        await Assert.That(NestedProperty.NestedDependency.DeepDependency!.IsInitialized).IsTrue();
    }
}

// Support classes
public class InitializableProperty : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }
}

public class PropertyWithNestedDependencies : IAsyncInitializer
{
    public NestedDependency? NestedDependency { get; set; }
    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        // This should be called AFTER nested dependencies are initialized
        if (NestedDependency?.IsInitialized != true ||
            NestedDependency?.DeepDependency?.IsInitialized != true)
        {
            throw new InvalidOperationException("Nested dependencies should be initialized first!");
        }

        IsInitialized = true;
        return Task.CompletedTask;
    }
}

public class NestedDependency : IAsyncInitializer
{
    public DeepDependency? DeepDependency { get; set; }
    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        // This should be called AFTER deep dependency is initialized
        if (DeepDependency?.IsInitialized != true)
        {
            throw new InvalidOperationException("Deep dependency should be initialized first!");
        }

        IsInitialized = true;
        return Task.CompletedTask;
    }
}

public class DeepDependency : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }
}

// Custom data generator that creates objects with nested dependencies
public class NestedDataGeneratorAttribute : AsyncDataSourceGeneratorAttribute<PropertyWithNestedDependencies>
{
    protected override async IAsyncEnumerable<Func<Task<PropertyWithNestedDependencies>>> GenerateDataSourcesAsync(
        DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return async () =>
        {
            var property = new PropertyWithNestedDependencies
            {
                NestedDependency = new NestedDependency
                {
                    DeepDependency = new DeepDependency()
                }
            };

            return await Task.FromResult(property);
        };
    }
}
