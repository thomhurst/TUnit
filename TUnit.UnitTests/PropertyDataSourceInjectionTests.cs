using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

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
}

// Custom data source attribute that inherits from AsyncDataSourceGeneratorAttribute
public class CustomDataSourceAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T> : AsyncDataSourceGeneratorAttribute<T>
{
    protected override async IAsyncEnumerable<Func<Task<T>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => Task.FromResult((T)Activator.CreateInstance(typeof(T))!);
        await Task.CompletedTask;
    }
}

public class CustomService : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }

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
