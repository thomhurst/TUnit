using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions.Tests.Bugs;

public class Tests2129 
{
    [Test]
    public async Task Test()
    {
        await Assert.That(string () => throw new InvalidOperationException()).Throws<InvalidOperationException>();
    }
    
    [Test]
    public async Task Test2()
    {
        await Assert.That(Guid? () => throw new InvalidOperationException()).Throws<InvalidOperationException>();
    }
    
    [Test]
    public async Task Test3()
    {
        await Assert.That(Guid () => throw new InvalidOperationException()).Throws<InvalidOperationException>();
    }
}