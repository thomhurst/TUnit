namespace TUnit.Assertions.Tests.Old;

public class DynamicAssertionTests
{
    [Test]
    public async Task Test1()
    {
        dynamic? foo = null;
        await TUnitAssert.That((object?)foo).IsNull();
    }
}
