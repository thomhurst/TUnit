#if NET5_0_OR_GREATER
using TUnit.Assertions.Enums;

namespace TUnit.Assertions.Tests;

public class MemoryAssertionTests
{
    // Memory<T> tests
    [Test]
    public async Task Test_Memory_IsEmpty()
    {
        Memory<int> memory = Array.Empty<int>();
        await Assert.That(memory).IsEmpty();
    }

    [Test]
    public async Task Test_Memory_IsNotEmpty()
    {
        Memory<int> memory = new[] { 1, 2, 3 };
        await Assert.That(memory).IsNotEmpty();
    }

    [Test]
    public async Task Test_Memory_Contains()
    {
        Memory<int> memory = new[] { 1, 2, 3, 4, 5 };
        await Assert.That(memory).Contains(3);
    }

    [Test]
    public async Task Test_Memory_DoesNotContain()
    {
        Memory<int> memory = new[] { 1, 2, 3, 4, 5 };
        await Assert.That(memory).DoesNotContain(10);
    }

    [Test]
    public async Task Test_Memory_Count_IsEqualTo()
    {
        Memory<int> memory = new[] { 1, 2, 3 };
        await Assert.That(memory).Count().IsEqualTo(3);
    }

    [Test]
    public async Task Test_Memory_Count_IsGreaterThan()
    {
        Memory<int> memory = new[] { 1, 2, 3, 4, 5 };
        await Assert.That(memory).Count().IsGreaterThan(3);
    }

    [Test]
    public async Task Test_Memory_HasSingleItem()
    {
        Memory<int> memory = new[] { 42 };
        await Assert.That(memory).HasSingleItem();
    }

    [Test]
    public async Task Test_Memory_All()
    {
        Memory<int> memory = new[] { 2, 4, 6, 8 };
        await Assert.That(memory).All(x => x % 2 == 0);
    }

    [Test]
    public async Task Test_Memory_Any()
    {
        Memory<int> memory = new[] { 1, 2, 3, 4, 5 };
        await Assert.That(memory).Any(x => x > 3);
    }

    [Test]
    public async Task Test_Memory_IsInOrder()
    {
        Memory<int> memory = new[] { 1, 2, 3, 4, 5 };
        await Assert.That(memory).IsInOrder();
    }

    [Test]
    public async Task Test_Memory_IsInDescendingOrder()
    {
        Memory<int> memory = new[] { 5, 4, 3, 2, 1 };
        await Assert.That(memory).IsInDescendingOrder();
    }

    [Test]
    public async Task Test_Memory_HasDistinctItems()
    {
        Memory<int> memory = new[] { 1, 2, 3, 4, 5 };
        await Assert.That(memory).HasDistinctItems();
    }

    [Test]
    public async Task Test_Memory_Chaining_With_And()
    {
        Memory<int> memory = new[] { 1, 2, 3 };
        await Assert.That(memory).IsNotEmpty().And.Contains(2);
    }

    // ReadOnlyMemory<T> tests
    [Test]
    public async Task Test_ReadOnlyMemory_IsEmpty()
    {
        ReadOnlyMemory<int> memory = Array.Empty<int>();
        await Assert.That(memory).IsEmpty();
    }

    [Test]
    public async Task Test_ReadOnlyMemory_IsNotEmpty()
    {
        ReadOnlyMemory<int> memory = new[] { 1, 2, 3 };
        await Assert.That(memory).IsNotEmpty();
    }

    [Test]
    public async Task Test_ReadOnlyMemory_Contains()
    {
        ReadOnlyMemory<int> memory = new[] { 1, 2, 3, 4, 5 };
        await Assert.That(memory).Contains(3);
    }

    [Test]
    public async Task Test_ReadOnlyMemory_Count_IsEqualTo()
    {
        ReadOnlyMemory<int> memory = new[] { 1, 2, 3 };
        await Assert.That(memory).Count().IsEqualTo(3);
    }

    [Test]
    public async Task Test_ReadOnlyMemory_Chaining_With_And()
    {
        ReadOnlyMemory<int> memory = new[] { 1, 2, 3 };
        await Assert.That(memory).IsNotEmpty().And.Contains(2);
    }

    // IsEquivalentTo tests
    [Test]
    public async Task Test_Memory_IsEquivalentTo()
    {
        Memory<int> memory = new[] { 3, 1, 2 };
        await Assert.That(memory).IsEquivalentTo(new[] { 1, 2, 3 });
    }

    [Test]
    public async Task Test_Memory_IsEquivalentTo_Ordered()
    {
        Memory<int> memory = new[] { 1, 2, 3 };
        await Assert.That(memory).IsEquivalentTo(new[] { 1, 2, 3 }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task Test_Memory_IsEquivalentTo_Fails()
    {
        Memory<int> memory = new[] { 1, 2, 3 };
        var action = async () => await Assert.That(memory).IsEquivalentTo(new[] { 1, 2, 4 });

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message).Contains("does not contain expected item: 4");
    }

    [Test]
    public async Task Test_Memory_IsEquivalentTo_DifferentCount_Fails()
    {
        Memory<int> memory = new[] { 1, 2, 3 };
        var action = async () => await Assert.That(memory).IsEquivalentTo(new[] { 1, 2 });

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message).Contains("3 items but expected 2");
    }

    [Test]
    public async Task Test_ReadOnlyMemory_IsEquivalentTo()
    {
        ReadOnlyMemory<byte> memory = new byte[] { 0x01, 0x02, 0x03 };
        await Assert.That(memory).IsEquivalentTo(new byte[] { 0x01, 0x02, 0x03 });
    }

    [Test]
    public async Task Test_ReadOnlyMemory_IsEquivalentTo_Unordered()
    {
        ReadOnlyMemory<byte> memory = new byte[] { 0x03, 0x01, 0x02 };
        await Assert.That(memory).IsEquivalentTo(new byte[] { 0x01, 0x02, 0x03 });
    }

    [Test]
    public async Task Test_ReadOnlyMemory_IsEquivalentTo_Fails()
    {
        ReadOnlyMemory<byte> memory = new byte[] { 0x01, 0x02, 0x03 };
        var action = async () => await Assert.That(memory).IsEquivalentTo(new byte[] { 0x01, 0x02, 0x04 });

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message).Contains("does not contain expected item");
    }

    // Failure tests
    [Test]
    public async Task Test_Memory_IsEmpty_Fails()
    {
        Memory<int> memory = new[] { 1, 2, 3 };
        var action = async () => await Assert.That(memory).IsEmpty();

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message).Contains("collection contains items");
    }

    [Test]
    public async Task Test_Memory_Contains_Fails()
    {
        Memory<int> memory = new[] { 1, 2, 3 };
        var action = async () => await Assert.That(memory).Contains(99);

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message).Contains("item was not found");
    }

    [Test]
    public async Task Test_Memory_Count_Fails()
    {
        Memory<int> memory = new[] { 1, 2, 3 };
        var action = async () => await Assert.That(memory).Count().IsEqualTo(5);

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message).Contains("found 3");
    }

    // String memory tests (common use case)
    [Test]
    public async Task Test_Memory_String_Contains()
    {
        Memory<char> memory = "Hello World".ToCharArray();
        await Assert.That(memory).Contains('W');
    }

    [Test]
    public async Task Test_ReadOnlyMemory_String()
    {
        ReadOnlyMemory<char> memory = "Hello".AsMemory();
        await Assert.That(memory).IsNotEmpty();
        await Assert.That(memory).Count().IsEqualTo(5);
    }
}
#endif
