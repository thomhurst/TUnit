using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class ArrayAssertionTests
{
    [Test]
    public async Task Test_Array_IsEmpty()
    {
        var array = Array.Empty<int>();
        await Assert.That(array).IsEmpty();
    }

    [Test]
    public async Task Test_Array_IsNotEmpty()
    {
        var array = new[] { 1, 2, 3 };
        await Assert.That(array).IsNotEmpty();
    }

    [Test]
    public async Task Test_Array_IsSingleElement()
    {
        var array = new[] { 42 };
        await Assert.That(array).IsSingleElement();
    }

    [Test]
    public async Task Test_Array_IsNotSingleElement_Multiple()
    {
        var array = new[] { 1, 2, 3 };
        await Assert.That(array).IsNotSingleElement();
    }

    [Test]
    public async Task Test_Array_IsNotSingleElement_Empty()
    {
        var array = Array.Empty<string>();
        await Assert.That(array).IsNotSingleElement();
    }

    [Test]
    public async Task Test_Array_IsEmpty_String()
    {
        var array = Array.Empty<string>();
        await Assert.That(array).IsEmpty();
    }

    [Test]
    public async Task Test_Array_IsNotEmpty_String()
    {
        var array = new[] { "hello", "world" };
        await Assert.That(array).IsNotEmpty();
    }

    [Test]
    public async Task Test_Array_IsSingleElement_String()
    {
        var array = new[] { "single" };
        await Assert.That(array).IsSingleElement();
    }

    [Test]
    public async Task Test_Array_IsEmpty_Fails_With_Item_Details()
    {
        var array = new[] { 1, 2, 3 };
        var action = async () => await Assert.That(array).IsEmpty();

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message)
            .Contains("collection contains items: [1, 2, 3]");
    }

    [Test]
    public async Task Test_Array_IsEmpty_Fails_With_String_Item_Details()
    {
        var array = new[] { "hello", "world" };
        var action = async () => await Assert.That(array).IsEmpty();

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message)
            .Contains("collection contains items: [hello, world]");
    }

    [Test]
    public async Task Test_Array_IsEmpty_Fails_With_Large_Collection_Shows_Limited_Items()
    {
        var array = Enumerable.Range(1, 15).ToArray();
        var action = async () => await Assert.That(array).IsEmpty();

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message)
            .Contains("collection contains items: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, and 5 more...]");
    }
}
