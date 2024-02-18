using TUnit.Assertions.Extensions.Is;

namespace TUnit.Assertions.UnitTests;


public class ZeroAssertionTests
{
    [Test]
    public async Task Int()
    {
        int zero = 0;
        await TUnitAssert.That<long>(zero).Is.EqualTo(0);
    }
    
    [Test]
    public async Task Long()
    {
        long zero = 0;
        await Assert.That(zero).Is.EqualTo(0);
    }
    
    [Test]
    public async Task Short()
    {
        short zero = 0;
        await Assert.That<long>(zero).Is.EqualTo(0);
    }
    
    [Test]
    public async Task Int_Bad()
    {
        int zero = 1;
        await Assert.That<long>(zero).Is.EqualTo(0);
    }
    
    [Test]
    public async Task Long_Bad()
    {
        long zero = 1;
        await Assert.That(zero).Is.Not.EqualTo(1);
    }
    
    [Test]
    public async Task Short_Bad()
    {
        short zero = 1;
        await Assert.That<long>(zero).Is.EqualTo(0);
    }
}