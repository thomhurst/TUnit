using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

public class StringContainsAssertionTests
{
    [Test]
    public async Task Contains_Success()
    {
        var value1 = "Foo";
        var value2 = "Foo";
        await TUnitAssert.That(value1).Contains(value2);
    }
    
    [Test]
    public async Task Contains_Trimmed1_Success()
    {
        var value1 = "Foo  ";
        var value2 = "  Foo ";
        await TUnitAssert.That(value1).Contains(value2).WithTrimming();
    }
    
    [Test]
    public async Task Contains_Trimmed2_Success()
    {
        var value1 = "Foo ";
        var value2 = "  Foo";
        await TUnitAssert.That(value1).Contains(value2).WithTrimming();
    }
    
    [Test]
    public async Task IgnoringWhitespace_Success()
    {
        var value1 = "       F    o    o    ";
        var value2 = "Foo";
        await TUnitAssert.That(value1).Contains(value2).IgnoringWhitespace();
    }
    
    [Test]
    public void Contains_Failure()
    {
        var value1 = "Foo";
        var value2 = "Bar";
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).Contains(value2));
    }
    
    [Test]
    public void Contains_Trimmed_Failure()
    {
        var value1 = "Foo";
        var value2 = "Foo! ";
        
        var exception = NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).Contains(value2).WithTrimming());
        NUnitAssert.That(exception!.Message, Does.EndWith("Assert.That(value1).Contains(value2, StringComparison.Ordinal).WithTrimming()"));
    }
    
    [Test]
    public void IgnoringWhitespace_Failure()
    {
        var value1 = "       F    o    o    ";
        var value2 = "Foo!";
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).Contains(value2).IgnoringWhitespace());
    }
}