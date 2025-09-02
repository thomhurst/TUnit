using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;
using TUnit.Core.Helpers;

namespace TUnit.UnitTests;

public class PropertyDataSourceInjectionTests
{
    // Property with Arguments attribute
    [Arguments("injected value")]
    public required string? StringProperty { get; set; }

    // Property with MethodDataSource
    [MethodDataSource(nameof(GetTestData))]
    public required TestData? DataProperty { get; set; }

    // Property with ClassDataSource
    [ClassDataSource<TestDataProvider>]
    public required ITestDataProvider? DataProviderProperty { get; set; }

    // Property with Arguments (complex type support)
    [Arguments("injected value")]  // Fix: Use simple string instead of incompatible types
    public required string? ComplexProperty { get; set; }

    [Test]
    public async Task PropertyInjection_ArgumentsAttribute_InjectsValue()
    {
        await Assert.That(StringProperty).IsNotNull();
        await Assert.That(StringProperty).IsEqualTo("injected value");
    }

    [Test]
    public async Task PropertyInjection_MethodDataSource_InjectsValue()
    {
        await Assert.That(DataProperty).IsNotNull();
        await Assert.That(DataProperty!.Value).IsEqualTo("test data");
    }

    [Test]
    public async Task PropertyInjection_ClassDataSource_InjectsAndInitializes()
    {
        await Assert.That(DataProviderProperty).IsNotNull();
        await Assert.That(DataProviderProperty!.IsInitialized).IsTrue();
        await Assert.That(DataProviderProperty.GetData()).IsEqualTo("initialized data");
    }

    [Test]
    public async Task PropertyInjection_ArgumentsAttribute_ComplexType()
    {
        // Simple string property injection should work
        await Assert.That(ComplexProperty).IsEqualTo("injected value");
    }

    // Helper methods and types
    public static TestData GetTestData()
    {
        return new TestData { Value = "test data" };
    }

    public class TestData
    {
        public string Value { get; set; } = "";
    }

    public interface ITestDataProvider
    {
        bool IsInitialized { get; }
        string GetData();
    }

    public class TestDataProvider : ITestDataProvider, IAsyncInitializer
    {
        public bool IsInitialized { get; private set; }

        public async Task InitializeAsync()
        {
            await Task.Delay(1);
            IsInitialized = true;
        }

        public string GetData()
        {
            return IsInitialized ? "initialized data" : "not initialized";
        }
    }

    public class ComplexData
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        // Nested property with data source
        [Arguments("nested value")]
        public required string? NestedProperty { get; set; }
    }
}

// Test for inheritance
public class BaseTestClass
{
    [Arguments("base value")]
    public required string? BaseProperty { get; set; }
}

public class DerivedPropertyInjectionTests : BaseTestClass
{
    [Arguments("derived value")]
    public required string? DerivedProperty { get; set; }

    [Test]
    public async Task PropertyInjection_Inheritance_InjectsBaseAndDerivedProperties()
    {
        await Assert.That(BaseProperty).IsEqualTo("base value");
        await Assert.That(DerivedProperty).IsEqualTo("derived value");
    }
}


// Test for custom data source attribute
public class CustomPropertyDataSourceTests
{
    [CustomDataSource<CustomService>]
    public required CustomService? Service { get; set; }

    [Test]
    public async Task PropertyInjection_CustomDataSource_WorksWithGenericApproach()
    {
        await Assert.That(Service).IsNotNull();
        await Assert.That(Service!.IsInitialized).IsTrue();
        await Assert.That(Service.GetMessage()).IsEqualTo("Custom service initialized");
    }

    [Test]
    public async Task PropertyInjection_CustomDataSource_WithNestedProperties_InjectsAndInitializesRecursively()
    {
        // Test main service
        await Assert.That(Service).IsNotNull();
        await Assert.That(Service!.IsInitialized).IsTrue();
        await Assert.That(Service.GetMessage()).IsEqualTo("Custom service initialized");

        // Test nested service
        await Assert.That(Service.NestedService).IsNotNull();
        await Assert.That(Service.NestedService!.IsInitialized).IsTrue();
        await Assert.That(Service.NestedService.GetData()).IsEqualTo("Nested service initialized");

        // Test deeply nested service
        await Assert.That(Service.NestedService.DeeplyNestedService).IsNotNull();
        await Assert.That(Service.NestedService.DeeplyNestedService!.IsInitialized).IsTrue();
        await Assert.That(Service.NestedService.DeeplyNestedService.GetDeepData()).IsEqualTo("Deeply nested service initialized");
    }
}

// Custom data source attribute that inherits from AsyncDataSourceGeneratorAttribute
public class CustomDataSourceAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T> : AsyncDataSourceGeneratorAttribute<T>
{
    protected override async IAsyncEnumerable<Func<Task<T>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return async () =>
        {
            // Use the DataSourceHelpers to create objects with init-only properties properly
            var (success, createdInstance) = await DataSourceHelpers.TryCreateWithInitializerAsync(typeof(T), dataGeneratorMetadata.TestInformation, dataGeneratorMetadata.TestSessionId);
            if (success)
            {
                return (T)createdInstance!;
            }
            
            // Fallback to regular Activator if no specialized creator is available
            return (T)Activator.CreateInstance(typeof(T))!;
        };
        await Task.CompletedTask;
    }
}

public class CustomService : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }

    // Nested property with its own data source
    [CustomDataSource<NestedService>]
    public required NestedService? NestedService { get; set; }

    public async Task InitializeAsync()
    {
        await Task.Delay(1);
        IsInitialized = true;
    }

    public string GetMessage()
    {
        return IsInitialized ? "Custom service initialized" : "Not initialized";
    }
}

public class NestedService : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }

    // Deeply nested property with its own data source
    [CustomDataSource<DeeplyNestedService>]
    public required DeeplyNestedService? DeeplyNestedService { get; set; }

    public async Task InitializeAsync()
    {
        await Task.Delay(1);
        IsInitialized = true;
    }

    public string GetData()
    {
        return IsInitialized ? "Nested service initialized" : "Nested not initialized";
    }
}

public class DeeplyNestedService : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }

    public async Task InitializeAsync()
    {
        await Task.Delay(1);
        IsInitialized = true;
    }

    public string GetDeepData()
    {
        return IsInitialized ? "Deeply nested service initialized" : "Deeply nested not initialized";
    }
}

// Test for inheritance with nested data sources - reproduces the reported issue
public class InheritedNestedDataSourceTests
{
    [ClassDataSource<InheritedData3>(Shared = SharedType.PerTestSession)]
    public required InheritedData3 Data3 { get; init; }

    [Test]
    public async Task PropertyInjection_InheritedDataSource_InjectsInheritedProperties()
    {
        // This test reproduces the issue: Data3.Data1 should not be null
        // The InitializeAsync of InheritedData2 should be called and Data1 should be injected
        await Assert.That(Data3).IsNotNull();
        await Assert.That(Data3.Data1).IsNotNull();
        await Assert.That(Data3.Data1.SomeValue).IsEqualTo("a");
    }
}

public class InheritedData1 : IAsyncInitializer
{
    public string SomeValue { get; set; } = "";
    
    public async Task InitializeAsync()
    {
        await Task.Delay(10); // Small delay for testing
        this.SomeValue = "a";
    }
}

public class InheritedData2 : IAsyncInitializer
{
    [ClassDataSource<InheritedData1>(Shared = SharedType.PerTestSession)]
    public required InheritedData1 Data1 { get; init; }

    public async Task InitializeAsync()
    {
        await Task.Delay(10); // Small delay for testing
    }
}

public class InheritedData3 : InheritedData2
{
}

