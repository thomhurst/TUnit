---
sidebar_position: 6.8
---

# Dictionary Assertions

TUnit provides specialized assertions for testing dictionaries (`IReadOnlyDictionary<TKey, TValue>`), including key and value membership checks. Dictionaries also inherit all collection assertions.

## Key Assertions

### ContainsKey

Tests that a dictionary contains a specific key:

```csharp
[Test]
public async Task Dictionary_Contains_Key()
{
    var dict = new Dictionary<string, int>
    {
        ["apple"] = 1,
        ["banana"] = 2,
        ["cherry"] = 3
    };

    await Assert.That(dict).ContainsKey("apple");
    await Assert.That(dict).ContainsKey("banana");
}
```

#### With Custom Comparer

```csharp
[Test]
public async Task Contains_Key_With_Comparer()
{
    var dict = new Dictionary<string, int>
    {
        ["Apple"] = 1,
        ["Banana"] = 2
    };

    await Assert.That(dict)
        .ContainsKey("apple")
        .Using(StringComparer.OrdinalIgnoreCase);
}
```

### DoesNotContainKey

Tests that a dictionary does not contain a specific key:

```csharp
[Test]
public async Task Dictionary_Does_Not_Contain_Key()
{
    var dict = new Dictionary<string, int>
    {
        ["apple"] = 1,
        ["banana"] = 2
    };

    await Assert.That(dict).DoesNotContainKey("cherry");
    await Assert.That(dict).DoesNotContainKey("orange");
}
```

## Value Assertions

### ContainsValue

Tests that a dictionary contains a specific value:

```csharp
[Test]
public async Task Dictionary_Contains_Value()
{
    var dict = new Dictionary<string, int>
    {
        ["apple"] = 1,
        ["banana"] = 2,
        ["cherry"] = 3
    };

    await Assert.That(dict).ContainsValue(2);
    await Assert.That(dict).ContainsValue(3);
}
```

## Collection Assertions on Dictionaries

Dictionaries inherit all collection assertions since they implement `IEnumerable<KeyValuePair<TKey, TValue>>`:

### Count

```csharp
[Test]
public async Task Dictionary_Count()
{
    var dict = new Dictionary<string, int>
    {
        ["a"] = 1,
        ["b"] = 2,
        ["c"] = 3
    };

    await Assert.That(dict).Count().IsEqualTo(3);
}
```

### IsEmpty / IsNotEmpty

```csharp
[Test]
public async Task Dictionary_Empty()
{
    var empty = new Dictionary<string, int>();
    var populated = new Dictionary<string, int> { ["key"] = 1 };

    await Assert.That(empty).IsEmpty();
    await Assert.That(populated).IsNotEmpty();
}
```

### Contains (KeyValuePair)

```csharp
[Test]
public async Task Dictionary_Contains_Pair()
{
    var dict = new Dictionary<string, int>
    {
        ["apple"] = 1,
        ["banana"] = 2
    };

    await Assert.That(dict).Contains(new KeyValuePair<string, int>("apple", 1));
}
```

### All Pairs Match Condition

```csharp
[Test]
public async Task All_Values_Positive()
{
    var dict = new Dictionary<string, int>
    {
        ["a"] = 1,
        ["b"] = 2,
        ["c"] = 3
    };

    await Assert.That(dict).All(kvp => kvp.Value > 0);
}
```

### Any Pair Matches Condition

```csharp
[Test]
public async Task Any_Key_Starts_With()
{
    var dict = new Dictionary<string, int>
    {
        ["apple"] = 1,
        ["banana"] = 2,
        ["cherry"] = 3
    };

    await Assert.That(dict).Any(kvp => kvp.Key.StartsWith("b"));
}
```

## Practical Examples

### Configuration Validation

```csharp
[Test]
public async Task Configuration_Has_Required_Keys()
{
    var config = LoadConfiguration();

    using (Assert.Multiple())
    {
        await Assert.That(config).ContainsKey("DatabaseConnection");
        await Assert.That(config).ContainsKey("ApiKey");
        await Assert.That(config).ContainsKey("Environment");
    }
}
```

### HTTP Headers Validation

```csharp
[Test]
public async Task Response_Headers()
{
    var headers = new Dictionary<string, string>
    {
        ["Content-Type"] = "application/json",
        ["Cache-Control"] = "no-cache"
    };

    await Assert.That(headers)
        .ContainsKey("Content-Type")
        .And.ContainsValue("application/json");
}
```

### Lookup Table Validation

```csharp
[Test]
public async Task Lookup_Table()
{
    var statusCodes = new Dictionary<int, string>
    {
        [200] = "OK",
        [404] = "Not Found",
        [500] = "Internal Server Error"
    };

    await Assert.That(statusCodes)
        .Count().IsEqualTo(3)
        .And.ContainsKey(200)
        .And.ContainsValue("OK");
}
```

### Cache Validation

```csharp
[Test]
public async Task Cache_Contains_Entry()
{
    var cache = new Dictionary<string, object>
    {
        ["user:123"] = new User { Id = 123 },
        ["user:456"] = new User { Id = 456 }
    };

    await Assert.That(cache)
        .ContainsKey("user:123")
        .And.Count().IsEqualTo(2)
        .And.IsNotEmpty();
}
```

## Dictionary Key/Value Operations

### Accessing Values After Key Check

```csharp
[Test]
public async Task Get_Value_After_Key_Check()
{
    var dict = new Dictionary<string, User>
    {
        ["alice"] = new User { Name = "Alice", Age = 30 }
    };

    // First verify key exists
    await Assert.That(dict).ContainsKey("alice");

    // Then safely access
    var user = dict["alice"];
    await Assert.That(user.Age).IsEqualTo(30);
}
```

### TryGetValue Pattern

```csharp
[Test]
public async Task TryGetValue_Pattern()
{
    var dict = new Dictionary<string, int>
    {
        ["count"] = 42
    };

    var found = dict.TryGetValue("count", out var value);

    await Assert.That(found).IsTrue();
    await Assert.That(value).IsEqualTo(42);
}
```

## Working with Dictionary Keys and Values

### Keys Collection

```csharp
[Test]
public async Task Dictionary_Keys()
{
    var dict = new Dictionary<string, int>
    {
        ["a"] = 1,
        ["b"] = 2,
        ["c"] = 3
    };

    var keys = dict.Keys;

    await Assert.That(keys)
        .Count().IsEqualTo(3)
        .And.Contains("a")
        .And.Contains("b")
        .And.Contains("c");
}
```

### Values Collection

```csharp
[Test]
public async Task Dictionary_Values()
{
    var dict = new Dictionary<string, int>
    {
        ["a"] = 1,
        ["b"] = 2,
        ["c"] = 3
    };

    var values = dict.Values;

    await Assert.That(values)
        .Count().IsEqualTo(3)
        .And.Contains(1)
        .And.Contains(2)
        .And.All(v => v > 0);
}
```

## Equivalency Checks

### Same Key-Value Pairs

```csharp
[Test]
public async Task Dictionaries_Are_Equivalent()
{
    var dict1 = new Dictionary<string, int>
    {
        ["a"] = 1,
        ["b"] = 2
    };

    var dict2 = new Dictionary<string, int>
    {
        ["b"] = 2,
        ["a"] = 1
    };

    // Dictionaries are equivalent (same pairs, order doesn't matter)
    await Assert.That(dict1).IsEquivalentTo(dict2);
}
```

## Chaining Dictionary Assertions

```csharp
[Test]
public async Task Chained_Dictionary_Assertions()
{
    var dict = new Dictionary<string, int>
    {
        ["apple"] = 1,
        ["banana"] = 2,
        ["cherry"] = 3
    };

    await Assert.That(dict)
        .IsNotEmpty()
        .And.Count().IsEqualTo(3)
        .And.ContainsKey("apple")
        .And.ContainsKey("banana")
        .And.ContainsValue(2)
        .And.All(kvp => kvp.Value > 0);
}
```

## Specialized Dictionary Types

### ConcurrentDictionary

```csharp
[Test]
public async Task Concurrent_Dictionary()
{
    var concurrent = new ConcurrentDictionary<string, int>();
    concurrent.TryAdd("a", 1);
    concurrent.TryAdd("b", 2);

    await Assert.That(concurrent)
        .Count().IsEqualTo(2)
        .And.ContainsKey("a");
}
```

### ReadOnlyDictionary

```csharp
[Test]
public async Task ReadOnly_Dictionary()
{
    var dict = new Dictionary<string, int> { ["a"] = 1 };
    var readOnly = new ReadOnlyDictionary<string, int>(dict);

    await Assert.That(readOnly)
        .Count().IsEqualTo(1)
        .And.ContainsKey("a");
}
```

### SortedDictionary

```csharp
[Test]
public async Task Sorted_Dictionary()
{
    var sorted = new SortedDictionary<int, string>
    {
        [3] = "three",
        [1] = "one",
        [2] = "two"
    };

    var keys = sorted.Keys.ToArray();

    await Assert.That(keys).IsInOrder();
}
```

## Null Checks

### Null Dictionary

```csharp
[Test]
public async Task Null_Dictionary()
{
    Dictionary<string, int>? dict = null;

    await Assert.That(dict).IsNull();
}
```

### Empty vs Null

```csharp
[Test]
public async Task Empty_vs_Null_Dictionary()
{
    Dictionary<string, int>? nullDict = null;
    var emptyDict = new Dictionary<string, int>();

    await Assert.That(nullDict).IsNull();
    await Assert.That(emptyDict).IsNotNull();
    await Assert.That(emptyDict).IsEmpty();
}
```

## Common Patterns

### Required Configuration Keys

```csharp
[Test]
public async Task All_Required_Keys_Present()
{
    var config = LoadConfiguration();
    var requiredKeys = new[] { "ApiKey", "Database", "Environment" };

    foreach (var key in requiredKeys)
    {
        await Assert.That(config).ContainsKey(key);
    }
}
```

Or with `Assert.Multiple`:

```csharp
[Test]
public async Task All_Required_Keys_Present_Multiple()
{
    var config = LoadConfiguration();
    var requiredKeys = new[] { "ApiKey", "Database", "Environment" };

    using (Assert.Multiple())
    {
        foreach (var key in requiredKeys)
        {
            await Assert.That(config).ContainsKey(key);
        }
    }
}
```

### Metadata Validation

```csharp
[Test]
public async Task Validate_Metadata()
{
    var metadata = GetFileMetadata();

    await Assert.That(metadata)
        .ContainsKey("ContentType")
        .And.ContainsKey("Size")
        .And.ContainsKey("LastModified")
        .And.All(kvp => kvp.Value != null);
}
```

### Feature Flags

```csharp
[Test]
public async Task Feature_Flags()
{
    var features = new Dictionary<string, bool>
    {
        ["NewUI"] = true,
        ["BetaFeature"] = false,
        ["ExperimentalApi"] = true
    };

    await Assert.That(features)
        .ContainsKey("NewUI")
        .And.ContainsValue(true);

    var newUiEnabled = features["NewUI"];
    await Assert.That(newUiEnabled).IsTrue();
}
```

## See Also

- [Collections](collections.md) - General collection assertions
- [Equality & Comparison](equality-and-comparison.md) - Comparing dictionary values
- [Strings](string.md) - String key comparisons
