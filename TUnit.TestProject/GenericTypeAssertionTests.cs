using TUnit.Assertions;

namespace TUnit.TestProject;

public class GenericTypeAssertionTests
{
    [Test]
    [Arguments("test")]
    public async Task GenericMethod_IsTypeOf_String_Explicit<T>(T value)
    {
        // Explicit two-parameter version should work
        await Assert.That(value).IsTypeOf<T, string>();
    }

    [Test]
    [Arguments(42)]
    public async Task GenericMethod_IsNotTypeOf_String_Explicit<T>(T value)
    {
        // Explicit two-parameter version should work  
        await Assert.That(value).IsNotTypeOf<T, string>();
    }

    [Test]
    [Arguments("test")]
    public async Task GenericMethod_IsTypeOf_String_Inferred<T>(T value)
    {
        // Try single-parameter version - this is the goal
        await Assert.That(value).IsTypeOf<string>();
    }

    [Test]
    [Arguments(42)]
    public async Task GenericMethod_IsNotTypeOf_String_Inferred<T>(T value)
    {
        // Try single-parameter version - this is the goal
        await Assert.That(value).IsNotTypeOf<string>();
    }
}