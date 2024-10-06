using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;


public class ZeroAssertionTests
{
    [Test]
    public async Task Int()
    {
        int zero = 0;
        await TUnitAssert.That<long>(zero).IsEqualTo(0);
    }
    
    [Test]
    public async Task Long()
    {
        long zero = 0;
        await Assert.That(zero).IsEqualTo(0);
    }
    
    [Test]
    public async Task Short()
    {
        short zero = 0;
        await Assert.That<long>(zero).IsEqualTo(0);
    }
    
    [Test]
    public void Int_Bad()
    {
        int zero = 1;
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await Assert.That<long>(zero).IsEqualTo(0));
    }
    
    [Test]
    public void Long_Bad()
    {
        long zero = 1;
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await Assert.That(zero).IsNotEqualTo(1));
    }
    
    [Test]
    public void Short_Bad()
    {
        short zero = 1;
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await Assert.That<long>(zero).IsEqualTo(0));
    }
}