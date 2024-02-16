namespace TUnit.Assertions.UnitTests;


public class EqualsAssertionTests
{
    [Test]
    public async Task String()
    {
        var one = "1";
        await Assert.That(one).Is.EqualTo("2").And.Is.Not.EqualTo("1");
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