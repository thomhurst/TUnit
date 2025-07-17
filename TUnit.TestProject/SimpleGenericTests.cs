using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Simple tests to verify generic type support
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class SimpleGenericMethodTests
{
    [Test]
    [Arguments(42)]
    [Arguments("hello")]
    public async Task GenericMethod_SimpleCase<T>(T value)
    {
        // Just verify the method runs with different types
        await Assert.That(value).IsNotEqualTo(default(T));
    }
}

/// <summary>
/// Simple generic class test
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class SimpleGenericClassTests<T>
{
    [Test]
    [Arguments(42)]  // Will create SimpleGenericClassTests<int>
    [Arguments("hello")]  // Will create SimpleGenericClassTests<string>
    public async Task TestWithValue(T value)
    {
        // Just verify the method runs with the correct type
        await Assert.That(value).IsNotEqualTo(default(T));
    }
}

/// <summary>
/// Simple generic class test
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[Arguments(42)]  // Will create SimpleGenericClassTests<int>
[Arguments("hello")]  // Will create SimpleGenericClassTests<string>
public class SimpleGenericClassConstructorTests<T>(T value)
{
    [Test]
    public async Task TestWithValue()
    {
        // Just verify the method runs with the correct type
        await Assert.That(value).IsNotEqualTo(default(T));
    }
}

/// <summary>
/// Simple generic class test
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[Arguments(42)]  // Will create SimpleGenericClassTests<int>
[Arguments("hello")]  // Will create SimpleGenericClassTests<string>
public class SimpleGenericClassConstructorAndMethodTests<T>(T value)
{
    [Test]
    [Arguments(10.5)]
    public async Task TestWithValue<T2>(T2 methodValue)
    {
        await Assert.That(value).IsNotEqualTo(default(T));
        await Assert.That(methodValue).IsNotEqualTo(default(T2));
    }
}
