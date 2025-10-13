using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class ReproTests
{
    [Test]
    public async Task Test_IsTypeOf_With_NonObject_Type()
    {
        string str = "Hello";
        // This should work but might not if IsTypeOf only works with object
        await Assert.That(str).IsTypeOf<string>();
    }

    [Test]
    public async Task Test_IsNotNull_With_ByteArray()
    {
        byte[]? nullableBytes = new byte[] { 1, 2, 3 };
        // This should work and return byte[] (not byte[]?)
        var result = await Assert.That(nullableBytes).IsNotNull();
        // result should be byte[], not byte[]?
        Console.WriteLine(result.Length);
    }
}
