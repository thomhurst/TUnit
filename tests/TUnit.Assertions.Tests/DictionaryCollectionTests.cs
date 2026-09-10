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

        await Assert.That(dictionary).Count().IsEqualTo(3);
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
            .And.Count().IsEqualTo(2)
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
            .And.Count().IsEqualTo(2);

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

    // ===================================
    // IDictionary<TKey, TValue> Tests
    // ===================================

    [Test]
    public async Task IDictionary_ContainsKey_Works()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1,
            ["key2"] = 2
        };

        await Assert.That(dictionary).ContainsKey("key1");
    }

    [Test]
    public async Task IDictionary_DoesNotContainKey_Works()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        await Assert.That(dictionary).DoesNotContainKey("nonexistent");
    }

    [Test]
    public async Task IDictionary_ContainsValue_Works()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 100,
            ["key2"] = 200
        };

        await Assert.That(dictionary).ContainsValue(200);
    }

    [Test]
    public async Task IDictionary_DoesNotContainValue_Works()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 100
        };

        await Assert.That(dictionary).DoesNotContainValue(999);
    }

    [Test]
    public async Task IDictionary_ContainsKeyWithValue_Works()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 100,
            ["key2"] = 200
        };

        await Assert.That(dictionary).ContainsKeyWithValue("key2", 200);
    }

    [Test]
    public async Task IDictionary_AllKeys_Works()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["prefix_a"] = 1,
            ["prefix_b"] = 2
        };

        await Assert.That(dictionary).AllKeys(k => k.StartsWith("prefix_"));
    }

    [Test]
    public async Task IDictionary_AllValues_Works()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["a"] = 10,
            ["b"] = 20
        };

        await Assert.That(dictionary).AllValues(v => v > 0);
    }

    [Test]
    public async Task IDictionary_AnyKey_Works()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["first"] = 1,
            ["special"] = 2
        };

        await Assert.That(dictionary).AnyKey(k => k == "special");
    }

    [Test]
    public async Task IDictionary_AnyValue_Works()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 100
        };

        await Assert.That(dictionary).AnyValue(v => v >= 100);
    }

    [Test]
    public async Task IDictionary_And_Chain_Works()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 100,
            ["key2"] = 200
        };

        await Assert.That(dictionary)
            .ContainsKey("key1")
            .And.ContainsValue(200)
            .And.AllValues(v => v > 0);
    }

    [Test]
    public async Task IDictionary_Or_Chain_Works()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 100
        };

        await Assert.That(dictionary)
            .ContainsKey("nonexistent")
            .Or.ContainsKey("key1");
    }

    [Test]
    public async Task IDictionary_Collection_Methods_Work()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1,
            ["key2"] = 2
        };

        // IDictionary should also have access to collection methods
        await Assert.That(dictionary)
            .IsNotEmpty()
            .And.Count().IsEqualTo(2);
    }

    [Test]
    public async Task IDictionary_ContainsKey_With_Custom_Comparer()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["Hello"] = 1
        };

        await Assert.That(dictionary)
            .ContainsKey("HELLO", StringComparer.OrdinalIgnoreCase);
    }

    [Test]
    public async Task Dictionary_ContainsKey_And_Value_IsEqualTo_Passes()
    {
        var dictionary = new Dictionary<string, long>
        {
            ["Key"] = 1234L
        };

        await Assert.That(dictionary).ContainsKey("Key").And.Value.IsEqualTo(1234L);
    }

    [Test]
    public async Task Dictionary_ContainsKey_And_Value_IsEqualTo_Fails_When_Value_Different()
    {
        var dictionary = new Dictionary<string, long>
        {
            ["Key"] = 1234L
        };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(dictionary).ContainsKey("Key").And.Value.IsEqualTo(9999L));

        await Assert.That(exception.Message).Contains("1234");
    }

    [Test]
    public async Task Dictionary_ContainsKey_And_Value_Fails_When_Key_Missing()
    {
        var dictionary = new Dictionary<string, long>
        {
            ["Key"] = 1234L
        };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(dictionary).ContainsKey("Missing").And.Value.IsEqualTo(1234L));

        // The ContainsKey check runs first (pre-work), so a missing key fails with the
        // standard "contain key" message rather than a raw KeyNotFoundException.
        await Assert.That(exception.Message).Contains("contain key");
    }

    [Test]
    public async Task Dictionary_ContainsKey_And_Value_Member()
    {
        var dictionary = new Dictionary<string, Holder>
        {
            ["Key"] = new Holder(1234L)
        };

        await Assert.That(dictionary)
            .ContainsKey("Key").And.Value.Member(x => x.Inner, p => p.IsEqualTo(1234L));
    }

    [Test]
    public async Task Dictionary_ContainsKey_And_Value_Supports_Other_Assertions()
    {
        var dictionary = new Dictionary<string, long>
        {
            ["Key"] = 1234L
        };

        await Assert.That(dictionary).ContainsKey("Key").And.Value.IsGreaterThan(1000L);
        await Assert.That(dictionary).ContainsKey("Key").And.Value.IsNotEqualTo(0L);
    }

    [Test]
    public async Task Dictionary_ContainsKey_And_Value_Then_Value_Level_And()
    {
        var dictionary = new Dictionary<string, long>
        {
            ["Key"] = 1234L
        };

        await Assert.That(dictionary)
            .ContainsKey("Key").And.Value.IsGreaterThan(1000L).And.IsLessThan(2000L);
    }

    [Test]
    public async Task Dictionary_And_Value_Still_Allows_Dictionary_Chaining()
    {
        var dictionary = new Dictionary<string, long>
        {
            ["Key"] = 1234L,
            ["Other"] = 1L
        };

        // The And continuation still exposes the regular dictionary methods.
        await Assert.That(dictionary).ContainsKey("Key").And.ContainsKey("Other");
    }

    [Test]
    public async Task Dictionary_LongerChain_Then_Value()
    {
        var dictionary = new Dictionary<string, long>
        {
            ["First"] = 1L,
            ["Key"] = 1234L
        };

        // Earlier assertions in the chain run as pre-work before the value is read.
        await Assert.That(dictionary)
            .ContainsKey("First").And.ContainsKey("Key").And.Value.IsEqualTo(1234L);
    }

    [Test]
    public async Task Dictionary_ContainsKey_With_Comparer_And_Value()
    {
        var dictionary = new Dictionary<string, long>
        {
            ["Hello"] = 1234L
        };

        await Assert.That(dictionary)
            .ContainsKey("HELLO", StringComparer.OrdinalIgnoreCase).And.Value.IsEqualTo(1234L);
    }

    [Test]
    public async Task IReadOnlyDictionary_ContainsKey_And_Value_IsEqualTo()
    {
        IReadOnlyDictionary<string, long> dictionary = new Dictionary<string, long>
        {
            ["Key"] = 1234L
        };

        await Assert.That(dictionary).ContainsKey("Key").And.Value.IsEqualTo(1234L);
    }

    [Test]
    public async Task IDictionary_ContainsKey_And_Value_IsEqualTo()
    {
        IDictionary<string, long> dictionary = new Dictionary<string, long>
        {
            ["Key"] = 1234L
        };

        await Assert.That(dictionary).ContainsKey("Key").And.Value.IsEqualTo(1234L);
    }

    // ── Collection-typed value drill-in (issue #6185 follow-up) ───────────────────────
    // When the dictionary value is itself a collection, .Value must expose the collection
    // surface (Count/Contains/IsEmpty/…) rather than binding to LINQ's Enumerable.Count (CS0411).

    [Test]
    public async Task Dictionary_ContainsKey_And_Value_Count_When_Value_Is_Collection()
    {
        IDictionary<string, IEnumerable<int>> dictionary = new Dictionary<string, IEnumerable<int>>
        {
            ["Key"] = new[] { 1, 2 }
        };

        await Assert.That(dictionary).ContainsKey("Key").And.Value.Count().IsEqualTo(2);
    }

    [Test]
    public async Task IReadOnlyDictionary_ContainsKey_And_Value_Count_When_Value_Is_Collection()
    {
        IReadOnlyDictionary<string, IEnumerable<string>> dictionary = new Dictionary<string, IEnumerable<string>>
        {
            ["Key"] = new[] { "a", "b", "c" }
        };

        await Assert.That(dictionary).ContainsKey("Key").And.Value.Count().IsGreaterThan(2);
    }

    [Test]
    public async Task Dictionary_ContainsKey_And_Value_Collection_Contains_And_IsNotEmpty()
    {
        IDictionary<string, IEnumerable<int>> dictionary = new Dictionary<string, IEnumerable<int>>
        {
            ["Key"] = new[] { 10, 20, 30 }
        };

        await Assert.That(dictionary).ContainsKey("Key").And.Value.Contains(20);
        await Assert.That(dictionary).ContainsKey("Key").And.Value.IsNotEmpty();
    }

    [Test]
    public async Task Dictionary_ContainsKey_And_Value_Collection_Count_Fails_With_Count_Message()
    {
        IDictionary<string, IEnumerable<int>> dictionary = new Dictionary<string, IEnumerable<int>>
        {
            ["Key"] = new[] { 1, 2 }
        };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(dictionary).ContainsKey("Key").And.Value.Count().IsEqualTo(5));

        await Assert.That(exception.Message).Contains("count");
    }

    [Test]
    public async Task Dictionary_ContainsKey_And_Value_Collection_Fails_When_Key_Missing()
    {
        IDictionary<string, IEnumerable<int>> dictionary = new Dictionary<string, IEnumerable<int>>
        {
            ["Key"] = new[] { 1, 2 }
        };

        // The ContainsKey pre-work still runs first, so a missing key fails with the
        // standard "contain key" message rather than reading the (absent) collection.
        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(dictionary).ContainsKey("Missing").And.Value.Count().IsEqualTo(2));

        await Assert.That(exception.Message).Contains("contain key");
    }

    [Test]
    public async Task Dictionary_ContainsKey_And_Value_List_HasItemAt()
    {
        IDictionary<string, IList<int>> dictionary = new Dictionary<string, IList<int>>
        {
            ["Key"] = new List<int> { 10, 20, 30 }
        };

        // List-specific surface (HasItemAt / ItemAt) is preserved, not degraded to a bare enumerable.
        await Assert.That(dictionary).ContainsKey("Key").And.Value.HasItemAt(1, 20);
        await Assert.That(dictionary).ContainsKey("Key").And.Value.ItemAt(2).IsEqualTo(30);
    }

    [Test]
    public async Task Dictionary_ContainsKey_And_Value_Set_IsSubsetOf()
    {
        IDictionary<string, ISet<int>> dictionary = new Dictionary<string, ISet<int>>
        {
            ["Key"] = new HashSet<int> { 1, 2 }
        };

        await Assert.That(dictionary).ContainsKey("Key").And.Value.IsSubsetOf(new[] { 1, 2, 3 });
    }

    [Test]
    public async Task Dictionary_ContainsKey_And_Value_NestedDictionary_ContainsKey()
    {
        IDictionary<string, IDictionary<string, int>> dictionary = new Dictionary<string, IDictionary<string, int>>
        {
            ["Outer"] = new Dictionary<string, int> { ["Inner"] = 42 }
        };

        await Assert.That(dictionary).ContainsKey("Outer").And.Value.ContainsKey("Inner");
        await Assert.That(dictionary).ContainsKey("Outer").And.Value.ContainsKeyWithValue("Inner", 42);
    }

    [Test]
    public async Task Dictionary_ContainsKey_And_Value_Array_Count()
    {
        IDictionary<string, int[]> dictionary = new Dictionary<string, int[]>
        {
            ["Key"] = new[] { 1, 2, 3 }
        };

        await Assert.That(dictionary).ContainsKey("Key").And.Value.Count().IsEqualTo(3);
    }

    [Test]
    public async Task Dictionary_ContainsKey_And_Value_ConcreteList_Count_And_MissingKey()
    {
        IDictionary<string, List<int>> dictionary = new Dictionary<string, List<int>>
        {
            ["Key"] = new List<int> { 1, 2 }
        };

        await Assert.That(dictionary).ContainsKey("Key").And.Value.Count().IsEqualTo(2);

        // Concrete List<T> uses the upcast seed; the ContainsKey pre-work must still run first.
        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(dictionary).ContainsKey("Missing").And.Value.Count().IsEqualTo(2));

        await Assert.That(exception.Message).Contains("contain key");
    }

    [Test]
    public async Task Dictionary_ContainsKey_And_Value_ConcreteDictionary_ContainsKey_And_MissingKey()
    {
        IDictionary<string, Dictionary<string, int>> dictionary = new Dictionary<string, Dictionary<string, int>>
        {
            ["Outer"] = new Dictionary<string, int> { ["Inner"] = 7 }
        };

        await Assert.That(dictionary).ContainsKey("Outer").And.Value.ContainsKey("Inner");

        // Concrete Dictionary<K,V> uses the upcast seed; the ContainsKey pre-work must still run first.
        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(dictionary).ContainsKey("Missing").And.Value.ContainsKey("Inner"));

        await Assert.That(exception.Message).Contains("contain key");
    }

    [Test]
    public async Task Dictionary_IsNotEmpty_Preserves_Dictionary_Continuation()
    {
        var dictionary = new Dictionary<string, long>
        {
            ["Key"] = 1234L
        };

        // IsNotEmpty now keeps the dictionary continuation, so ContainsKey/.Value remain available.
        await Assert.That(dictionary)
            .IsNotEmpty()
            .And.ContainsKey("Key").And.Value.IsEqualTo(1234L);
    }

    [Test]
    public async Task Dictionary_IsEmpty_Preserves_Dictionary_Continuation()
    {
        var nonEmpty = new Dictionary<string, long>
        {
            ["Key"] = 1234L
        };

        await Assert.That(nonEmpty)
            .IsEmpty()
            .Or.ContainsKey("Key");
    }

    [Test]
    public async Task IDictionary_IsNotEmpty_Preserves_Dictionary_Continuation()
    {
        IDictionary<string, long> dictionary = new Dictionary<string, long>
        {
            ["Key"] = 1234L
        };

        await Assert.That(dictionary)
            .IsNotEmpty()
            .And.ContainsKey("Key").And.Value.IsEqualTo(1234L);
    }

    [Test]
    public async Task Dictionary_Count_Preserves_Dictionary_Continuation()
    {
        var dictionary = new Dictionary<string, long>
        {
            ["Key"] = 1234L,
            ["Other"] = 1L
        };

        await Assert.That(dictionary)
            .Count().IsEqualTo(2).And.ContainsKey("Key").And.Value.IsEqualTo(1234L);
    }

    [Test]
    public async Task Dictionary_Count_Comparison_Methods_Work()
    {
        var dictionary = new Dictionary<string, long>
        {
            ["a"] = 1L,
            ["b"] = 2L,
            ["c"] = 3L
        };

        await Assert.That(dictionary).Count().IsGreaterThan(2);
        await Assert.That(dictionary).Count().IsLessThanOrEqualTo(3).And.ContainsKey("a");
        await Assert.That(dictionary).Count().IsNotEqualTo(0).And.IsNotEmpty();
        await Assert.That(dictionary).Count().IsPositive();
    }

    [Test]
    public async Task Dictionary_Count_Fails_With_Count_Message()
    {
        var dictionary = new Dictionary<string, long>
        {
            ["a"] = 1L
        };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(dictionary).Count().IsEqualTo(5));

        await Assert.That(exception.Message).Contains("count");
    }

    [Test]
    public async Task Dictionary_ContainsKey_And_Count()
    {
        var dictionary = new Dictionary<string, long>
        {
            ["Key"] = 1234L,
            ["Other"] = 1L
        };

        await Assert.That(dictionary)
            .ContainsKey("Key").And.Count().IsEqualTo(2);
    }

    [Test]
    public async Task Dictionary_HasSingleItem_Preserves_Dictionary_Continuation()
    {
        var dictionary = new Dictionary<string, long>
        {
            ["Key"] = 1234L
        };

        await Assert.That(dictionary)
            .HasSingleItem().And.ContainsKey("Key").And.Value.IsEqualTo(1234L);
    }

    [Test]
    public async Task Dictionary_Size_Methods_Preserve_Dictionary_Continuation()
    {
        var dictionary = new Dictionary<string, long>
        {
            ["a"] = 1L,
            ["b"] = 2L,
            ["c"] = 3L
        };

        await Assert.That(dictionary).HasAtLeast(2).And.ContainsKey("a");
        await Assert.That(dictionary).HasAtMost(5).And.ContainsKey("b");
        await Assert.That(dictionary).HasCountBetween(1, 5).And.ContainsKey("c");
    }

    [Test]
    public async Task IDictionary_Count_And_HasSingleItem_Preserve_Continuation()
    {
        IDictionary<string, long> single = new Dictionary<string, long>
        {
            ["Key"] = 1234L
        };

        await Assert.That(single)
            .HasSingleItem().And.ContainsKey("Key").And.Value.IsEqualTo(1234L);

        IDictionary<string, long> many = new Dictionary<string, long>
        {
            ["a"] = 1L,
            ["b"] = 2L
        };

        await Assert.That(many)
            .Count().IsEqualTo(2).And.ContainsKey("a");
    }

    private sealed record Holder(long Inner);
}
