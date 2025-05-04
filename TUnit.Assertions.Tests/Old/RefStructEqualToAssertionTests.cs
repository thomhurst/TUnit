using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

public class RefStructEqualToAssertionTests
{
    [Test]
    public async Task EqualsTo_Success()
    {
        var value1 = new MyRefStruct();
        var value2 = new MyRefStruct();

        await TUnitAssert.That(value1.Value).IsEqualTo(value2.Value);
    }
    public ref struct MyRefStruct
    {
        public string Value { get; set; }
    }
}