using System.Collections;
using System.Collections.Immutable;

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
    public async Task String_ReadOnlyDictionary_Contains_Key()
    {
        var dictionary = new ReadDictionary();
        
        await TUnitAssert.That(dictionary).ContainsKey("Blah");
    }
    
    [Test]
    public async Task String_ReadOnlyDictionary_Does_Not_Contain_Key()
    {
        IReadOnlyDictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>
        {
            ["Blah"] = []
        }.AsReadOnly();
        
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
    
    public class ReadDictionary : IReadOnlyDictionary<string, string>
    {
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get;
        }
        public bool ContainsKey(string key)
        {
            return true;
        }

        public bool TryGetValue(string key, out string value)
        {
            value = "Value";
            return true;
        }

        public string this[string key] => "Value";
        public IEnumerable<string> Keys
        {
            get;
        } = ["Blah"];
        public IEnumerable<string> Values
        {
            get;
        } = ["Value"];
    }
}