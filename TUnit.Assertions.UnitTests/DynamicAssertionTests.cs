using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

public class DynamicAssertionTests
{
    [Test]
    public async Task Test1()
    {
        dynamic? foo = null;
        await TUnitAssert.That((object?)foo).IsNull();
    }
}
