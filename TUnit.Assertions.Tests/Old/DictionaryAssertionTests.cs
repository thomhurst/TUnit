namespace TUnit.Assertions.Tests.Old;

public class DictionaryAssertionTests
{
    [Test]
    public async Task String_Dictionary_Contains_Key()
    {
        var dictionary = new Dictionary<string, byte[]>
        {
            ["Blah"] = []
        };
        
        await TUnitAssert.That(dictionary).ContainsKey("Blah");
    }
    
    [Test]
    public async Task String_Dictionary_Does_Not_Contain_Key()
    {
        var dictionary = new Dictionary<string, byte[]>
        {
            ["Blah"] = []
        };
        
        await TUnitAssert.That(dictionary).DoesNotContainKey("blah");
    }
    
    [Test]
    public async Task String_Dictionary_Contains_Key_IgnoreCase()
    {
        var dictionary = new Dictionary<string, byte[]>
        {
            ["Blah"] = []
        };
        
        await TUnitAssert.That(dictionary).ContainsKey("blah", StringComparer.InvariantCultureIgnoreCase);
    }
}