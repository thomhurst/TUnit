using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions.Tests.Bugs;

public class Tests1770
{
    [Test]
    public async Task Throws_Nothing_Keeps_Type()
    {
        var guid = await Assert.That(Guid.NewGuid).ThrowsNothing();

        await Assert.That(guid.ToByteArray()).IsNotEmpty();
    }
    
    [Test]
    public async Task Throws_Nothing_Keeps_Type_Async_Delegate()
    {
        var guid = await Assert.That(async () =>
        {
            await Task.CompletedTask;
            return Guid.NewGuid();
        }).ThrowsNothing();

        await Assert.That(guid.ToByteArray()).IsNotEmpty();
    }
}