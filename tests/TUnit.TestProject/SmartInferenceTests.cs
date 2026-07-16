namespace TUnit.TestProject;

// Test scenario: Class has generic type parameter T, but no class-level [Arguments]
// Method has [Arguments] with different types that should infer T
public class SmartInferenceTests<T>
{
    [Test]
    [Arguments(42)]
    [Arguments("hello")]
    [Arguments(3.14)]
    public async Task TestSmartInference(T value)
    {
        // Should generate:
        // SmartInferenceTests<int>.TestSmartInference(42)
        // SmartInferenceTests<string>.TestSmartInference("hello") 
        // SmartInferenceTests<double>.TestSmartInference(3.14)
        await Assert.That(value).IsEqualTo(value); // Simple assertion that works with any type
    }
}

// Test scenario: Multiple class type parameters with mixed inference
public class MultipleTypeParameterTests<T, U>
{
    [Test] 
    [Arguments(42, "hello")]
    [Arguments(99, "world")]
    public async Task TestMultipleParams(T first, U second)
    {
        // Should generate:
        // MultipleTypeParameterTests<int, string>.TestMultipleParams(42, "hello")
        // MultipleTypeParameterTests<int, string>.TestMultipleParams(99, "world")
        await Assert.That(first).IsEqualTo(first);
        await Assert.That(second).IsEqualTo(second);
    }
}

// Test scenario: Generic collections should work - using data source method since arrays can't be in attributes
public class GenericCollectionTests<T>
{
    public static IEnumerable<T[]> GetArrayData()
    {
        // This won't work for smart inference since it's runtime - keeping as an example
        yield return new T[] { };
    }
}