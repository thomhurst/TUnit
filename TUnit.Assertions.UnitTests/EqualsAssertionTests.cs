using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

public class EqualsAssertionTests
{
    [Test]
    public async Task Assertion_Message_Has_Correct_Expression()
    {
        var one = "1";
        
        await NUnitAssert.ThatAsync(async () =>
                await TUnitAssert.That(one).IsEqualTo("2", StringComparison.Ordinal).And.IsNotEqualTo("1").And.IsTypeOf(typeof(string)),
            Throws.Exception.Message.Contain("Assert.That(one).IsEqualTo(\"2\", StringComparison.Ordinal).And.IsNotEqualTo(\"1\", StringComparison.Ord...")
        );
    }
    
    [Test]
    public async Task Long()
    {
        long zero = 0;
        await TUnitAssert.That(zero).IsEqualTo(0);
    }
    
    [Test]
    public async Task Short()
    {
        short zero = 0;
        await TUnitAssert.That<long>(zero).IsEqualTo(0);
    }
    
    [Test]
    public void Int_Bad()
    {
        int zero = 1;
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That<long>(zero).IsEqualTo(0));
    }
    
    [Test]
    public void Long_Bad()
    {
        long zero = 1;
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(zero).IsEqualTo(0));
    }
    
    [Test]
    public void Short_Bad()
    {
        short zero = 1;
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That<long>(zero).IsEqualTo(0));
    }
}