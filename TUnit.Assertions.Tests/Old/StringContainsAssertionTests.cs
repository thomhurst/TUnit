namespace TUnit.Assertions.Tests.Old;

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
    public async Task Contains_Failure()
    {
        var value1 = "Foo";
        var value2 = "Bar";
        
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).Contains(value2));
    }
    
    [Test]
    public async Task Contains_Trimmed_Failure()
    {
        var value1 = "Foo";
        var value2 = "Foo! ";
        
        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).Contains(value2).WithTrimming());
        await TUnitAssert.That(exception!.Message).EndsWith("Assert.That(value1).Contains(value2, StringComparison.Ordinal).WithTrimming()");
    }
    
    [Test]
    public async Task IgnoringWhitespace_Failure()
    {
        var value1 = "       F    o    o    ";
        var value2 = "Foo!";
        
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).Contains(value2).IgnoringWhitespace());
    }
}