using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class ReproTests
{
    [Test]
    public async Task Test_IsTypeOf_With_Object_Type()
    {
        object str = "Hello";
        // This should work - we're asserting that an object is of type string
        await Assert.That(str).IsTypeOf<string>();
    }
    
    [Test]
    public async Task Test_IsTypeOf_With_Nullable_Object_Type()
    {
        object? obj = "Hello";
        // This should also work
        await Assert.That(obj).IsTypeOf<string>();
    }

    [Test]
    public async Task Test_IsNotNull_With_ByteArray_NonNullable()
    {
        byte[] nonNullableBytes = new byte[] { 1, 2, 3 };
        // This should work and return byte[]
        var result = await Assert.That(nonNullableBytes).IsNotNull();
        Console.WriteLine($"Result type: {result?.GetType().FullName}");
        Console.WriteLine($"Has Length property: {result is byte[]}");
    }
    
    [Test]
    public async Task Test_IsNotNull_With_ByteArray_Nullable()
    {
        byte[]? nullableBytes = new byte[] { 1, 2, 3 };
        // When used with byte[]?, the compiler infers IEnumerable<byte> due to overload resolution
        var result = await Assert.That(nullableBytes).IsNotNull();
        // Check what type was actually returned at runtime (it's byte[], but compile-time type is IEnumerable<byte>)
        Console.WriteLine($"Result type: {result?.GetType().FullName}");
        // Workaround: cast to array to access Length
        Console.WriteLine($"Length: {((byte[])result).Length}");
    }
}
