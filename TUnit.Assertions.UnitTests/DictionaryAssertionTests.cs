using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

public class DictionaryAssertionTests
{
    [Test]
    public async Task String_Dictionary_Contains_Key()
    {
        var dictionary = new Dictionary<string, byte[]>
        {
            ["Blah"] = []
        };
        
        await TUnitAssert.That(dictionary).Does.ContainKey("Blah");
    }
}