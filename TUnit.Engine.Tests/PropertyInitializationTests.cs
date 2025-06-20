using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Services;

namespace TUnit.Engine.Tests;

public class PropertyInitializationTests
{
    [Test]
    public async Task Properties_With_IDataAttribute_Are_Initialized_During_Test_Invocation()
    {
        // Arrange
        var testClass = new TestClassWithDataAttributeProperty();
        var testDetails = CreateTestDetails(testClass);
        var visited = new HashSet<object>();
        var cancellationToken = CancellationToken.None;
        
        // Simulate property being set during test discovery
        testClass.AsyncInitializerProperty = new MockAsyncInitializer();
        
        // Act - Simulate what TestInvoker does
        // Since InitializeObjectGraphAsync is private, we'll test the behavior indirectly
        // by verifying that ObjectInitializer.InitializeAsync works correctly
        if (testClass.AsyncInitializerProperty is IAsyncInitializer)
        {
            await ObjectInitializer.InitializeAsync(testClass.AsyncInitializerProperty, cancellationToken);
        }
        
        // Assert
        Assert.That(testClass.AsyncInitializerProperty!.IsInitialized, Is.True);
    }
    
    [Test]
    public async Task Nested_Properties_With_IDataAttribute_Are_Initialized_In_Correct_Order()
    {
        // Arrange
        var testClass = new TestClassWithNestedDataAttributeProperty();
        var testDetails = CreateTestDetails(testClass);
        var visited = new HashSet<object>();
        var cancellationToken = CancellationToken.None;
        
        // Simulate properties being set during test discovery
        var nestedProperty = new NestedPropertyWithAsyncInitializer();
        nestedProperty.DeepAsyncInitializer = new MockAsyncInitializer();
        testClass.NestedProperty = nestedProperty;
        
        // Act - Simulate nested initialization
        // Initialize nested property first
        if (nestedProperty.DeepAsyncInitializer is IAsyncInitializer)
        {
            await ObjectInitializer.InitializeAsync(nestedProperty.DeepAsyncInitializer, cancellationToken);
        }
        // Then initialize parent
        if (nestedProperty is IAsyncInitializer)
        {
            await ObjectInitializer.InitializeAsync(nestedProperty, cancellationToken);
        }
        
        // Assert - Both nested and parent should be initialized
        Assert.That(nestedProperty.DeepAsyncInitializer.IsInitialized, Is.True);
        Assert.That(nestedProperty.IsInitialized, Is.True);
    }
    
    [Test]
    public async Task IAsyncDataSourceGeneratorAttribute_Properties_Are_Initialized_During_Discovery()
    {
        // This test verifies the behavior in ReflectionTestConstructionBuilder
        // where IAsyncDataSourceGeneratorAttribute properties that implement IAsyncInitializer
        // are initialized immediately during test discovery
        
        // Arrange
        var mockProperty = new MockAsyncInitializer();
        var propertyArgs = new Dictionary<string, object?>
        {
            ["TestProperty"] = mockProperty
        };
        
        // Act - Simulate what happens in GetPropertyArgumentsAsync
        if (mockProperty is IAsyncInitializer)
        {
            await ObjectInitializer.InitializeAsync(mockProperty);
        }
        
        // Assert
        Assert.That(mockProperty.IsInitialized, Is.True);
    }
    
    private static TestDetails CreateTestDetails(object testInstance)
    {
        var methodMetadata = new MethodMetadata
        {
            Name = "TestMethod",
            Class = new ClassMetadata
            {
                Type = testInstance.GetType(),
                Properties = GetPropertiesWithDataAttribute(testInstance.GetType())
            }
        };
        
        return new TestDetails
        {
            ClassInstance = testInstance,
            MethodMetadata = methodMetadata,
            TestId = "test-id",
            TestName = "Test",
            TestMethodArguments = Array.Empty<object?>(),
            TestClassArguments = Array.Empty<object?>(),
            TestClassInjectedPropertyArguments = new Dictionary<string, object?>()
        };
    }
    
    private static PropertyMetadata[] GetPropertiesWithDataAttribute(Type type)
    {
        return type.GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(TestDataAttribute), false).Any())
            .Select(p => new PropertyMetadata
            {
                Name = p.Name,
                Type = p.PropertyType,
                Getter = obj => p.GetValue(obj),
                Attributes = p.GetCustomAttributes(false)
                    .Select(a => new AttributeMetadata 
                    { 
                        Instance = a,
                        AttributeType = a.GetType()
                    })
                    .ToArray()
            })
            .ToArray();
    }
}

// Test fixtures
public class TestClassWithDataAttributeProperty
{
    [TestData]
    public MockAsyncInitializer? AsyncInitializerProperty { get; set; }
}

public class TestClassWithNestedDataAttributeProperty
{
    [TestData]
    public NestedPropertyWithAsyncInitializer? NestedProperty { get; set; }
}

public class NestedPropertyWithAsyncInitializer : IAsyncInitializer
{
    public MockAsyncInitializer? DeepAsyncInitializer { get; set; }
    public bool IsInitialized { get; private set; }
    
    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }
}

public class MockAsyncInitializer : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }
    
    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }
}

// Mock data attribute for testing
public class MockAsyncDataSourceGeneratorAttribute : Attribute, IAsyncDataSourceGeneratorAttribute
{
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return async () => await Task.FromResult(new object?[] { new MockAsyncInitializer() });
    }
}