namespace TUnit.Assertions.UnitTests;


public class ZeroAssertionTests
{
    [Test]
    public void Int()
    {
        int zero = 0;
        Assert.That<long>(zero).Is.EqualTo(0);
    }
    
    [Test]
    public void Long()
    {
        long zero = 0;
        Assert.That(zero);
    }
    
    [Test]
    public void Short()
    {
        short zero = 0;
        Assert.That<long>(zero);
    }
    
    [Test]
    public void Int_Bad()
    {
        int zero = 1;
        Assert.That<long>(zero);
    }
    
    [Test]
    public void Long_Bad()
    {
        long zero = 1;
        Assert.That(zero);
    }
    
    [Test]
    public void Short_Bad()
    {
        short zero = 1;
        Assert.That<long>(zero);
    }
}