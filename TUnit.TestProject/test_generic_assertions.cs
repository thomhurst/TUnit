using TUnit.Assertions;

namespace TUnit.TestProject;

public class test_generic_assertions
{
    [Test]
    [Arguments("test")]
    public async Task GenericMethod_IsTypeOf_String<T>(T value)
    {
        // Test the two-parameter syntax - this works!
        await Assert.That(value).IsTypeOf<string, T>();
    }

    [Test]
    [Arguments(42)]
    public async Task GenericMethod_IsNotTypeOf_String<T>(T value)
    {
        // Test the two-parameter syntax - this works!
        await Assert.That(value).IsNotTypeOf<string, T>();
    }

    [Test]
    [Arguments(123)]
    public async Task GenericMethod_IsTypeOf_Int32<T>(T value)
    {
        // Test with different expected type
        await Assert.That(value).IsTypeOf<int, T>();
    }

    [Test]
    [Arguments("hello")]
    public async Task GenericMethod_IsNotTypeOf_Int32<T>(T value)
    {
        // Test with different expected type
        await Assert.That(value).IsNotTypeOf<int, T>();
    }
}