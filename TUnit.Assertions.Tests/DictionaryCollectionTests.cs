namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests to verify that dictionaries now behave as collections of KeyValuePair items,
/// inheriting all collection assertion methods while maintaining dictionary-specific methods.
/// </summary>
public class DictionaryCollectionTests
{
    [Test]
    public async Task Dictionary_IsEmpty_Works()
    {
        var dictionary = new Dictionary<string, int>();

        await Assert.That(dictionary).IsEmpty();
    }

    [Test]
    public async Task Dictionary_IsNotEmpty_Works()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        await Assert.That(dictionary).IsNotEmpty();
    }

    [Test]
    public async Task Dictionary_HasCount_Works()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1,
            ["key2"] = 2,
            ["key3"] = 3
        };

        await Assert.That(dictionary).HasCount(3);
    }

    [Test]
    public async Task Dictionary_Contains_KeyValuePair_Works()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1,
            ["key2"] = 2
        };

        await Assert.That(dictionary).Contains(new KeyValuePair<string, int>("key1", 1));
    }

    [Test]
    public async Task Dictionary_Contains_Predicate_Works()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1,
            ["key2"] = 2,
            ["key3"] = 3
        };

        await Assert.That(dictionary).Contains(kvp => kvp.Key == "key2" && kvp.Value == 2);
    }

    [Test]
    public async Task Dictionary_All_Works()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1,
            ["key2"] = 2,
            ["key3"] = 3
        };

        await Assert.That(dictionary).All(kvp => kvp.Value > 0);
    }

    [Test]
    public async Task Dictionary_Any_Works()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1,
            ["key2"] = 2,
            ["key3"] = 3
        };

        await Assert.That(dictionary).Any(kvp => kvp.Value == 2);
    }

    [Test]
    public async Task Dictionary_ContainsKey_StillWorks()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        // Dictionary-specific method should still work
        await Assert.That(dictionary).ContainsKey("key1");
    }

    [Test]
    public async Task Dictionary_DoesNotContainKey_StillWorks()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        // Dictionary-specific method should still work
        await Assert.That(dictionary).DoesNotContainKey("key2");
    }

    [Test]
    public async Task Dictionary_And_Chain_Preserves_Type()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1,
            ["key2"] = 2
        };

        // Should be able to chain collection and dictionary-specific methods
        await Assert.That(dictionary)
            .ContainsKey("key1")
            .And.IsNotEmpty()
            .And.HasCount(2)
            .And.Contains(new KeyValuePair<string, int>("key2", 2))
            .And.ContainsKey("key2");
    }

    [Test]
    public async Task Dictionary_Or_Chain_Works()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        await Assert.That(dictionary)
            .ContainsKey("nonexistent")
            .Or.ContainsKey("key1");
    }

    [Test]
    public async Task IReadOnlyDictionary_Collection_Methods_Work()
    {
        IReadOnlyDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1,
            ["key2"] = 2
        };

        await Assert.That(dictionary)
            .IsNotEmpty()
            .And.HasCount(2)
            .And.ContainsKey("key1");
    }

    [Test]
    public async Task Dictionary_IsEquivalentTo_Works()
    {
        var dictionary1 = new Dictionary<string, int>
        {
            ["key1"] = 1,
            ["key2"] = 2
        };

        var dictionary2 = new Dictionary<string, int>
        {
            ["key2"] = 2,
            ["key1"] = 1
        };

        // IsEquivalentTo works on collections regardless of order
        await Assert.That(dictionary1).IsEquivalentTo(dictionary2);
    }

    [Test]
    public async Task Dictionary_HasDistinctItems_Works()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1,
            ["key2"] = 2,
            ["key3"] = 1  // Same value, different key - still distinct KeyValuePairs
        };

        await Assert.That(dictionary).HasDistinctItems();
    }
}
