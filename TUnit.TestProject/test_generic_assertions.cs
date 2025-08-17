using TUnit.Assertions;

namespace TUnit.TestProject;

public class test_generic_assertions
{
    [Test]
    [Arguments("test")]
    public async Task GenericMethod_IsTypeOf_String<T>(T value)
    {
        // Test the two-parameter syntax first to make sure it works
        await Assert.That(value).IsTypeOf<string, T>();
    }

    [Test]
    [Arguments(42)]
    public async Task GenericMethod_IsNotTypeOf_String<T>(T value)
    {
        // Test the two-parameter syntax first to make sure it works
        await Assert.That(value).IsNotTypeOf<string, T>();
    }
}