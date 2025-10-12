using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

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
}
