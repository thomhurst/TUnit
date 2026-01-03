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

        // Dictionary methods can chain with other dictionary methods
        await Assert.That(dictionary)
            .ContainsKey("key1")
            .And.ContainsKey("key2");

        // Collection methods work on dictionaries
        await Assert.That(dictionary)
            .IsNotEmpty()
            .And.HasCount(2)
            .And.Contains(new KeyValuePair<string, int>("key2", 2));
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

        // Collection methods work on IReadOnlyDictionary
        await Assert.That(dictionary)
            .IsNotEmpty()
            .And.HasCount(2);

        // Dictionary-specific methods also work
        await Assert.That(dictionary)
            .ContainsKey("key1");
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
        // Cast both to IEnumerable to use collection equivalency
        await Assert.That((IEnumerable<KeyValuePair<string, int>>)dictionary1)
            .IsEquivalentTo(dictionary2);
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

    // ===================================
    // ContainsValue Tests
    // ===================================

    [Test]
    public async Task Dictionary_ContainsValue_Passes_When_Value_Exists()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 100,
            ["key2"] = 200,
            ["key3"] = 300
        };

        await Assert.That(dictionary).ContainsValue(200);
    }

    [Test]
    public async Task Dictionary_ContainsValue_Fails_When_Value_Not_Found()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 100
        };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(dictionary).ContainsValue(999));

        await Assert.That(exception.Message).Contains("contain value");
    }

    [Test]
    public async Task Dictionary_DoesNotContainValue_Passes_When_Value_Not_Exists()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 100
        };

        await Assert.That(dictionary).DoesNotContainValue(999);
    }

    [Test]
    public async Task Dictionary_DoesNotContainValue_Fails_When_Value_Exists()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 100,
            ["key2"] = 200
        };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(dictionary).DoesNotContainValue(100));

        await Assert.That(exception.Message).Contains("not contain value");
    }

    // ===================================
    // ContainsKeyWithValue Tests
    // ===================================

    [Test]
    public async Task Dictionary_ContainsKeyWithValue_Passes_When_Match()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 100,
            ["key2"] = 200
        };

        await Assert.That(dictionary).ContainsKeyWithValue("key2", 200);
    }

    [Test]
    public async Task Dictionary_ContainsKeyWithValue_Fails_When_Key_Not_Found()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 100
        };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(dictionary).ContainsKeyWithValue("nonexistent", 100));

        await Assert.That(exception.Message).Contains("key");
    }

    [Test]
    public async Task Dictionary_ContainsKeyWithValue_Fails_When_Value_Different()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 100
        };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(dictionary).ContainsKeyWithValue("key1", 999));

        await Assert.That(exception.Message).Contains("value");
    }

    // ===================================
    // AllKeys Tests
    // ===================================

    [Test]
    public async Task Dictionary_AllKeys_Passes_When_All_Match()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["prefix_a"] = 1,
            ["prefix_b"] = 2,
            ["prefix_c"] = 3
        };

        await Assert.That(dictionary).AllKeys(key => key.StartsWith("prefix_"));
    }

    [Test]
    public async Task Dictionary_AllKeys_Fails_When_Some_Dont_Match()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["prefix_a"] = 1,
            ["other_b"] = 2  // Doesn't start with "prefix_"
        };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(dictionary).AllKeys(key => key.StartsWith("prefix_")));

        await Assert.That(exception.Message).Contains("all keys");
    }

    // ===================================
    // AllValues Tests
    // ===================================

    [Test]
    public async Task Dictionary_AllValues_Passes_When_All_Match()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["a"] = 10,
            ["b"] = 20,
            ["c"] = 30
        };

        await Assert.That(dictionary).AllValues(val => val > 0);
    }

    [Test]
    public async Task Dictionary_AllValues_Fails_When_Some_Dont_Match()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["a"] = 10,
            ["b"] = -5  // Negative
        };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(dictionary).AllValues(val => val > 0));

        await Assert.That(exception.Message).Contains("all values");
    }

    // ===================================
    // AnyKey Tests
    // ===================================

    [Test]
    public async Task Dictionary_AnyKey_Passes_When_At_Least_One_Matches()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["first"] = 1,
            ["special_key"] = 2,
            ["last"] = 3
        };

        await Assert.That(dictionary).AnyKey(key => key.Contains("special"));
    }

    [Test]
    public async Task Dictionary_AnyKey_Fails_When_None_Match()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["first"] = 1,
            ["second"] = 2
        };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(dictionary).AnyKey(key => key.Contains("special")));

        await Assert.That(exception.Message).Contains("any key");
    }

    // ===================================
    // AnyValue Tests
    // ===================================

    [Test]
    public async Task Dictionary_AnyValue_Passes_When_At_Least_One_Matches()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 100,
            ["c"] = 3
        };

        await Assert.That(dictionary).AnyValue(val => val >= 100);
    }

    [Test]
    public async Task Dictionary_AnyValue_Fails_When_None_Match()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2
        };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(dictionary).AnyValue(val => val >= 100));

        await Assert.That(exception.Message).Contains("any value");
    }

    // ===================================
    // Chaining with new methods
    // ===================================

    [Test]
    public async Task Dictionary_Chain_New_Methods()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 100,
            ["key2"] = 200,
            ["key3"] = 300
        };

        await Assert.That(dictionary)
            .ContainsKey("key1")
            .And.ContainsValue(200)
            .And.AllValues(v => v > 0);
    }

    [Test]
    public async Task Dictionary_ContainsKey_With_Custom_Comparer()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["Hello"] = 1
        };

        // Using custom comparer for case-insensitive matching
        await Assert.That(dictionary)
            .ContainsKey("HELLO", StringComparer.OrdinalIgnoreCase);
    }
}
