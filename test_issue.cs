using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TestIssue;

public class Tests
{
    // Test 1: ThrowsExactly works as expected (should fail)
    public static async Task Test1()
    {
        await Assert.That(() => throw new ArgumentOutOfRangeException(paramName: "quantity", message: "must be less than 20"))
                    .ThrowsExactly<ArgumentException>();
    }

    // Test 2: ThrowsExactly with WithParameterName fails to throw (should fail but doesn't)
    public static async Task Test2()
    {
        await Assert.That(() => throw new ArgumentOutOfRangeException(paramName: "quantity", message: "must be less than 20"))
                    .ThrowsExactly<ArgumentException>()
                    .WithParameterName("quantity");
    }

    // Test 3: ThrowsExactly with wrong exception type does throw
    public static async Task Test3()
    {
        await Assert.That(() => throw new ArgumentException(paramName: "quantity", message: "must be less than 20"))
                    .ThrowsExactly<ArgumentOutOfRangeException>()
                    .WithParameterName("quantity");
    }

    public static async Task Main()
    {
        Console.WriteLine("Test 1: ThrowsExactly without WithParameterName");
        try
        {
            await Test1();
            Console.WriteLine("  PASSED (unexpected)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FAILED (expected): {ex.Message}");
        }

        Console.WriteLine("\nTest 2: ThrowsExactly with WithParameterName (subclass thrown)");
        try
        {
            await Test2();
            Console.WriteLine("  PASSED (unexpected - THIS IS THE BUG!)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FAILED (expected): {ex.Message}");
        }

        Console.WriteLine("\nTest 3: ThrowsExactly with wrong exception type");
        try
        {
            await Test3();
            Console.WriteLine("  PASSED (unexpected)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FAILED (expected): {ex.Message}");
        }
    }
}
