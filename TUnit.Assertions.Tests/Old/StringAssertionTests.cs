namespace TUnit.Assertions.Tests.Old;


public class StringAssertionTests
{
    [Test]
    public async Task NullOrEmpty_String()
    {
        var str = "Hello";
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(str).IsNullOrEmpty());
    }

    [Test]
    public async Task NullOrEmpty_Whitespace()
    {
        var str = " ";
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(str).IsNullOrEmpty());
    }

    [Test]
    public async Task NullOrEmpty_Null()
    {
        string? str = null;
        await TUnitAssert.That(str).IsNullOrEmpty();
    }

    [Test]
    public async Task NullOrEmpty_Empty()
    {
        var str = "";
        await TUnitAssert.That(str).IsNullOrEmpty();
    }

    [Test]
    public async Task NullOrWhitespace_String()
    {
        var str = "Hello";
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(str).IsNullOrWhitespace());
    }

    [Test]
    public async Task NullOrWhitespace_Whitespace()
    {
        var str = " ";
        await TUnitAssert.That(str).IsNullOrWhitespace();
    }

    [Test]
    public async Task NullOrWhitespace_Null()
    {
        string? str = null;
        await TUnitAssert.That(str).IsNullOrWhitespace();
    }

    [Test]
    public async Task NullOrWhitespace_Empty()
    {
        var str = "";
        await TUnitAssert.That(str).IsNullOrWhitespace();
    }

    [Test]
    public async Task Null_String()
    {
        var str = "Hello";
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(str).IsNull());
    }

    [Test]
    public async Task Null_Whitespace()
    {
        var str = " ";
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(str).IsNull());
    }

    [Test]
    public async Task Null_Null()
    {
        string? str = null;
        await TUnitAssert.That(str).IsNull();
    }

    [Test]
    public async Task Null_Empty()
    {
        var str = "";
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(str).IsNull());
    }
}
