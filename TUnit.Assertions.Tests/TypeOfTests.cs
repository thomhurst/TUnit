using System.Text;

namespace TUnit.Assertions.Tests;

public class TypeOfTests
{
    [Test]
    public async Task Returns_Casted_Object()
    {
        object? obj = new StringBuilder();

        var result = await Assert.That(obj).IsTypeOf<StringBuilder>();

        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task IsTypeOf_Works_With_NonObject_Type()
    {
        // This test verifies the fix for the issue where IsTypeOf only worked with object types
        string str = "Hello";

        var result = await Assert.That(str).IsTypeOf<string, string>();

        await Assert.That(result).IsEqualTo("Hello");
    }

    [Test]
    public async Task IsTypeOf_Works_With_Object_SingleTypeParameter()
    {
        object obj = "Hello";

        var result = await Assert.That(obj).IsTypeOf<string>();

        await Assert.That(result).IsEqualTo("Hello");
    }

    [Test]
    public async Task IsTypeOf_Works_With_NullableObject()
    {
        object? nullableObj = new StringBuilder("Test");

        var result = await Assert.That(nullableObj).IsTypeOf<StringBuilder>();

        await Assert.That(result.ToString()).IsEqualTo("Test");
    }

    [Test]
    public async Task IsTypeOf_Fails_When_Type_Mismatch()
    {
        object obj = "Hello";

        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(obj).IsTypeOf<int>();
        });
    }

    [Test]
    public async Task IsTypeOf_TwoTypeParameters_Works_With_AnySourceType()
    {
        // Test with string as source type
        string str = "Hello";
        var result1 = await Assert.That(str).IsTypeOf<string, string>();
        await Assert.That(result1).IsEqualTo("Hello");

        // Test with int as source type
        int number = 42;
        var result2 = await Assert.That(number).IsTypeOf<int, int>();
        await Assert.That(result2).IsEqualTo(42);
    }
}
