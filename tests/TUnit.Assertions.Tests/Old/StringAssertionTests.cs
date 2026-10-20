using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests.Old;

public class StringAssertionTests
{
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
