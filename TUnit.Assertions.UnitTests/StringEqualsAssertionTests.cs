using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

public class StringEqualsAssertionTests
{
    [Test]
    public async Task Equals_Success()
    {
        var value1 = "Foo";
        var value2 = "Foo";
        await TUnitAssert.That(value1).IsEqualTo(value2);
    }
    
    [Test]
    public async Task Equals_Trimmed1_Success()
    {
        var value1 = "Foo";
        var value2 = "Foo ";
        await TUnitAssert.That(value1).IsEqualTo(value2).WithTrimming();
    }
    
    [Test]
    public async Task Equals_Trimmed2_Success()
    {
        var value1 = "Foo ";
        var value2 = "Foo";
        await TUnitAssert.That(value1).IsEqualTo(value2).WithTrimming();
    }
    
    [Test]
    public async Task IgnoringWhitespace_Success()
    {
        var value1 = "       F    o    o    ";
        var value2 = "Foo";
        await TUnitAssert.That(value1).IsEqualTo(value2).IgnoringWhitespace();
    }
    
    [Test]
    public async Task Equals_NullAndEmptyEquality_Success()
    {
        var value1 = "";
        string? value2 = null;
        await TUnitAssert.That(value1).IsEqualTo(value2).WithNullAndEmptyEquality();
    }
    
    [Test]
    public async Task Equals_NullAndEmptyEquality2_Success()
    {
        string? value1 = null;
        var value2 = "";
        
        await TUnitAssert.That(value1).IsEqualTo(value2).WithNullAndEmptyEquality();
    }
    
    [Test]
    public void Equals_Failure()
    {
        var value1 = "Foo";
        var value2 = "Bar";
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2));
    }
    
    [Test]
    public void Equals_Trimmed1_Failure()
    {
        var value1 = "Foo";
        var value2 = "Foo! ";
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2).WithTrimming());
    }
    
    [Test]
    public void Equals_Trimmed2_Failure()
    {
        var value1 = "Foo! ";
        var value2 = "Foo";
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2).WithTrimming());
    }
    
    [Test]
    public void IgnoringWhitespace_Failure()
    {
        var value1 = "       F    o    o    !";
        var value2 = "Foo";
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2).IgnoringWhitespace());
    }
    
    [Test]
    public void Equals_NullAndEmptyEquality_Failure()
    {
        var value1 = "1";
        string? value2 = null;
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2).WithNullAndEmptyEquality());
    }
    
    [Test]
    public void Equals_NullAndEmptyEquality2_Failure()
    {
        string? value1 = null;
        var value2 = "1";
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2).WithNullAndEmptyEquality());
    }
}