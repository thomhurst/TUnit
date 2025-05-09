using System.Collections.Immutable;
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
    
    [Test]
    public async Task Immutable_Dictionary_Does_Not_Contain_Key()
    {
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () =>
            await TUnitAssert.That(ImmutableDictionary<string, int>.Empty.Add("Hello", 1))
                .IsEquivalentTo(ImmutableDictionary<string, int>.Empty.Add("Hello2", 1))
        );
    }
}