using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;


public class StringAssertionTests
{
    [Test]
    public void NullOrEmpty_String()
    {
        var str = "Hello";
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(str).IsNullOrEmpty());
    }
    
    [Test]
    public void NullOrEmpty_Whitespace()
    {
        var str = " ";
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(str).IsNullOrEmpty());
    }
    
    [Test]
    public void NullOrEmpty_Null()
    {
        string? str = null;
        NUnitAssert.DoesNotThrowAsync(async () => await TUnitAssert.That(str).IsNullOrEmpty());
    }
    
    [Test]
    public void NullOrEmpty_Empty()
    {
        var str = "";
        NUnitAssert.DoesNotThrowAsync(async () => await TUnitAssert.That(str).IsNullOrEmpty());
    }
    
    [Test]
    public void NullOrWhitespace_String()
    {
        var str = "Hello";
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(str).IsNullOrWhitespace());
    }
    
    [Test]
    public void NullOrWhitespace_Whitespace()
    {
        var str = " ";
        NUnitAssert.DoesNotThrowAsync(async () => await TUnitAssert.That(str).IsNullOrWhitespace());
    }
    
    [Test]
    public void NullOrWhitespace_Null()
    {
        string? str = null;
        NUnitAssert.DoesNotThrowAsync(async () => await TUnitAssert.That(str).IsNullOrWhitespace());
    }
    
    [Test]
    public void NullOrWhitespace_Empty()
    {
        var str = "";
        NUnitAssert.DoesNotThrowAsync(async () => await TUnitAssert.That(str).IsNullOrWhitespace());
    }
    
    [Test]
    public void Null_String()
    {
        var str = "Hello";
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(str).IsNull());
    }
    
    [Test]
    public void Null_Whitespace()
    {
        var str = " ";
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(str).IsNull());
    }
    
    [Test]
    public void Null_Null()
    {
        string? str = null;
        NUnitAssert.DoesNotThrowAsync(async () => await TUnitAssert.That(str).IsNull());
    }
    
    [Test]
    public void Null_Empty()
    {
        var str = "";
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(str).IsNull());
    }
}