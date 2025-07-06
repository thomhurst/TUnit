namespace TUnit.TestProject;

/// <summary>
/// Simple tests to verify generic type support
/// </summary>
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
public class SimpleGenericClassTests<T>
{
    [Test]
    [Arguments(42)]  // Will create SimpleGenericClassTests<int>
    public async Task TestWithValue(T value)
    {
        // Just verify the method runs with the correct type
        await Assert.That(value).IsNotEqualTo(default(T));
    }
}
