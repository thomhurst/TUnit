using System.Collections.Concurrent;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Integration tests for dictionary assertion methods (ContainsKey, DoesNotContainKey, ContainsValue, DoesNotContainValue).
/// Tests cover IDictionary, IReadOnlyDictionary, and concrete dictionary types with chaining and failure scenarios.
/// </summary>
public class DictionaryAssertionTests
{
    #region IDictionary Direct Assertions

    [Test]
    public async Task IDictionary_ContainsKey_Passes_When_Key_Exists()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1,
            ["key2"] = 2
        };

        await Assert.That(dictionary).ContainsKey("key1");
    }

    [Test]
    public async Task IDictionary_ContainsKey_Fails_When_Key_Missing()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(dictionary).ContainsKey("missing"));
    }

    [Test]
    public async Task IDictionary_DoesNotContainKey_Passes_When_Key_Missing()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        await Assert.That(dictionary).DoesNotContainKey("missing");
    }

    [Test]
    public async Task IDictionary_DoesNotContainKey_Fails_When_Key_Exists()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(dictionary).DoesNotContainKey("key1"));
    }

    [Test]
    public async Task IDictionary_ContainsValue_Passes_When_Value_Exists()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 42,
            ["key2"] = 100
        };

        await Assert.That(dictionary).ContainsValue(42);
    }

    [Test]
    public async Task IDictionary_ContainsValue_Fails_When_Value_Missing()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(dictionary).ContainsValue(999));
    }

    [Test]
    public async Task IDictionary_DoesNotContainValue_Passes_When_Value_Missing()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        await Assert.That(dictionary).DoesNotContainValue(999);
    }

    [Test]
    public async Task IDictionary_DoesNotContainValue_Fails_When_Value_Exists()
    {
        IDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 42
        };

        await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(dictionary).DoesNotContainValue(42));
    }

    #endregion

    #region IReadOnlyDictionary Assertions

    [Test]
    public async Task IReadOnlyDictionary_ContainsKey_Passes_When_Key_Exists()
    {
        IReadOnlyDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1,
            ["key2"] = 2
        };

        await Assert.That(dictionary).ContainsKey("key1");
    }

    [Test]
    public async Task IReadOnlyDictionary_ContainsKey_Fails_When_Key_Missing()
    {
        IReadOnlyDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(dictionary).ContainsKey("missing"));
    }

    [Test]
    public async Task IReadOnlyDictionary_DoesNotContainKey_Passes_When_Key_Missing()
    {
        IReadOnlyDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        await Assert.That(dictionary).DoesNotContainKey("missing");
    }

    [Test]
    public async Task IReadOnlyDictionary_DoesNotContainKey_Fails_When_Key_Exists()
    {
        IReadOnlyDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(dictionary).DoesNotContainKey("key1"));
    }

    [Test]
    public async Task IReadOnlyDictionary_ContainsValue_Passes_When_Value_Exists()
    {
        IReadOnlyDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 42,
            ["key2"] = 100
        };

        await Assert.That(dictionary).ContainsValue(42);
    }

    [Test]
    public async Task IReadOnlyDictionary_ContainsValue_Fails_When_Value_Missing()
    {
        IReadOnlyDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(dictionary).ContainsValue(999));
    }

    [Test]
    public async Task IReadOnlyDictionary_DoesNotContainValue_Passes_When_Value_Missing()
    {
        IReadOnlyDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        await Assert.That(dictionary).DoesNotContainValue(999);
    }

    [Test]
    public async Task IReadOnlyDictionary_DoesNotContainValue_Fails_When_Value_Exists()
    {
        IReadOnlyDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["key1"] = 42
        };

        await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(dictionary).DoesNotContainValue(42));
    }

    #endregion

    #region Concrete Dictionary Types

    [Test]
    public async Task Dictionary_ContainsKey_Works()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1,
            ["key2"] = 2
        };

        await Assert.That(dictionary).ContainsKey("key1");
    }

    [Test]
    public async Task Dictionary_ContainsValue_Works()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 42,
            ["key2"] = 100
        };

        await Assert.That(dictionary).ContainsValue(42);
    }

    [Test]
    public async Task Dictionary_DoesNotContainKey_Works()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        await Assert.That(dictionary).DoesNotContainKey("missing");
    }

    [Test]
    public async Task Dictionary_DoesNotContainValue_Works()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        await Assert.That(dictionary).DoesNotContainValue(999);
    }

    [Test]
    public async Task ConcurrentDictionary_ContainsKey_Works()
    {
        var dictionary = new ConcurrentDictionary<string, int>();
        dictionary["key1"] = 1;
        dictionary["key2"] = 2;

        await Assert.That(dictionary).ContainsKey("key1");
    }

    [Test]
    public async Task ConcurrentDictionary_ContainsValue_Works()
    {
        var dictionary = new ConcurrentDictionary<string, int>();
        dictionary["key1"] = 42;
        dictionary["key2"] = 100;

        await Assert.That(dictionary).ContainsValue(42);
    }

    [Test]
    public async Task ConcurrentDictionary_DoesNotContainKey_Works()
    {
        var dictionary = new ConcurrentDictionary<string, int>();
        dictionary["key1"] = 1;

        await Assert.That(dictionary).DoesNotContainKey("missing");
    }

    [Test]
    public async Task ConcurrentDictionary_DoesNotContainValue_Works()
    {
        var dictionary = new ConcurrentDictionary<string, int>();
        dictionary["key1"] = 1;

        await Assert.That(dictionary).DoesNotContainValue(999);
    }

    [Test]
    public async Task SortedDictionary_ContainsKey_Works()
    {
        var dictionary = new SortedDictionary<string, int>
        {
            ["alpha"] = 1,
            ["beta"] = 2,
            ["gamma"] = 3
        };

        await Assert.That(dictionary).ContainsKey("beta");
    }

    [Test]
    public async Task SortedDictionary_ContainsValue_Works()
    {
        var dictionary = new SortedDictionary<string, int>
        {
            ["alpha"] = 1,
            ["beta"] = 2,
            ["gamma"] = 3
        };

        await Assert.That(dictionary).ContainsValue(2);
    }

    #endregion

    #region Chained Assertions

    [Test]
    public async Task Dictionary_Chained_ContainsKey_And_ContainsKey()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3
        };

        await Assert.That(dictionary)
            .ContainsKey("a")
            .And.ContainsKey("b")
            .And.DoesNotContainKey("missing");
    }

    [Test]
    public async Task Dictionary_Chained_ContainsValue_And_DoesNotContainValue()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3
        };

        await Assert.That(dictionary)
            .ContainsValue(1)
            .And.ContainsValue(2)
            .And.DoesNotContainValue(999);
    }

    [Test]
    public async Task Dictionary_Chained_Mixed_Key_And_Value_Assertions()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 100,
            ["key2"] = 200
        };

        await Assert.That(dictionary)
            .ContainsKey("key1")
            .And.ContainsValue(100)
            .And.DoesNotContainKey("missing")
            .And.DoesNotContainValue(999);
    }

    [Test]
    public async Task Dictionary_Or_Chain_Works()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key1"] = 1
        };

        // Either condition is true - passes because "key1" exists
        await Assert.That(dictionary)
            .ContainsKey("nonexistent")
            .Or.ContainsKey("key1");
    }

    [Test]
    public async Task Dictionary_Chained_With_Collection_Assertions()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2
        };

        await Assert.That(dictionary)
            .ContainsKey("a")
            .And.IsNotEmpty()
            .And.HasCount(2);
    }

    #endregion

    #region Failure Messages

    [Test]
    public async Task ContainsKey_Failure_Has_Meaningful_Message()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["existing"] = 1
        };

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(dictionary).ContainsKey("missing"));

        // Verify that the failure message is meaningful
        await Assert.That(exception.Message).Contains("contain key");
    }

    [Test]
    public async Task DoesNotContainKey_Failure_Has_Meaningful_Message()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["existing"] = 1
        };

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(dictionary).DoesNotContainKey("existing"));

        // Verify that the failure message is meaningful
        await Assert.That(exception.Message).Contains("not contain key");
    }

    [Test]
    public async Task ContainsValue_Failure_Has_Meaningful_Message()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key"] = 1
        };

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(dictionary).ContainsValue(999));

        // Verify that the failure message is meaningful
        await Assert.That(exception.Message).Contains("contain value");
    }

    [Test]
    public async Task DoesNotContainValue_Failure_Has_Meaningful_Message()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["key"] = 42
        };

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(dictionary).DoesNotContainValue(42));

        // Verify that the failure message is meaningful
        await Assert.That(exception.Message).Contains("not contain value");
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Empty_Dictionary_DoesNotContainKey_Passes()
    {
        var dictionary = new Dictionary<string, int>();

        await Assert.That(dictionary).DoesNotContainKey("any");
    }

    [Test]
    public async Task Empty_Dictionary_DoesNotContainValue_Passes()
    {
        var dictionary = new Dictionary<string, int>();

        await Assert.That(dictionary).DoesNotContainValue(42);
    }

    [Test]
    public async Task Dictionary_With_Null_Value_ContainsValue_Works()
    {
        var dictionary = new Dictionary<string, string?>
        {
            ["key1"] = "value1",
            ["key2"] = null
        };

        await Assert.That(dictionary).ContainsValue(null);
    }

    [Test]
    public async Task Dictionary_With_Int_Keys_ContainsKey_Works()
    {
        var dictionary = new Dictionary<int, string>
        {
            [1] = "one",
            [2] = "two",
            [3] = "three"
        };

        await Assert.That(dictionary).ContainsKey(2);
    }

    [Test]
    public async Task Dictionary_With_Complex_Value_ContainsValue_Works()
    {
        var person1 = new Person("Alice", 30);
        var person2 = new Person("Bob", 25);

        var dictionary = new Dictionary<string, Person>
        {
            ["alice"] = person1,
            ["bob"] = person2
        };

        // Reference equality check - should find the exact same instance
        await Assert.That(dictionary).ContainsValue(person1);
    }

    [Test]
    public async Task Null_Dictionary_ContainsKey_Fails()
    {
        IDictionary<string, int>? dictionary = null;

        await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(dictionary!).ContainsKey("key"));
    }

    #endregion

    #region Multiple Dictionary Entries

    [Test]
    public async Task Dictionary_With_Many_Entries_ContainsKey_Works()
    {
        var dictionary = Enumerable.Range(1, 100)
            .ToDictionary(i => $"key{i}", i => i);

        await Assert.That(dictionary).ContainsKey("key50");
        await Assert.That(dictionary).ContainsKey("key1");
        await Assert.That(dictionary).ContainsKey("key100");
    }

    [Test]
    public async Task Dictionary_With_Many_Entries_ContainsValue_Works()
    {
        var dictionary = Enumerable.Range(1, 100)
            .ToDictionary(i => $"key{i}", i => i * 10);

        await Assert.That(dictionary).ContainsValue(500); // key50
        await Assert.That(dictionary).ContainsValue(10);  // key1
        await Assert.That(dictionary).ContainsValue(1000); // key100
    }

    #endregion

    private record Person(string Name, int Age);
}
