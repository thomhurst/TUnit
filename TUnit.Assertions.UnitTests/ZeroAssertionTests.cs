using Is = TUnit.Assertions.Is;

namespace TUnit.Assertions.UnitTests;


public class ZeroAssertionTests
{
    [Test]
    public void Int()
    {
        int zero = 0;
        Assert.That(zero, Is.Zero);
    }
    
    [Test]
    public void Long()
    {
        long zero = 0;
        Assert.That(zero, Is.Zero);
    }
    
    [Test]
    public void Short()
    {
        short zero = 0;
        Assert.That(zero, Is.Zero);
    }
    
    [Test]
    public void Int_Bad()
    {
        int zero = 1;
        Assert.That(zero, Is.Zero);
    }
    
    [Test]
    public void Long_Bad()
    {
        long zero = 1;
        Assert.That(zero, Is.Zero);
    }
    
    [Test]
    public void Short_Bad()
    {
        short zero = 1;
        Assert.That(zero, Is.Zero);
    }
}