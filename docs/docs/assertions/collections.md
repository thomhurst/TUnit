---
sidebar_position: 6.5
---

# Collection Assertions

TUnit provides comprehensive assertions for testing collections, including membership, count, ordering, and equivalency checks. These assertions work with any `IEnumerable<T>`.

## Membership Assertions

### Contains (Item)

Tests that a collection contains a specific item:

```csharp
[Test]
public async Task Collection_Contains_Item()
{
    var numbers = new[] { 1, 2, 3, 4, 5 };

    await Assert.That(numbers).Contains(3);
    await Assert.That(numbers).Contains(1);
}
```

Works with any collection type:

```csharp
[Test]
public async Task Various_Collection_Types()
{
    var list = new List<string> { "apple", "banana", "cherry" };
    await Assert.That(list).Contains("banana");

    var hashSet = new HashSet<int> { 10, 20, 30 };
    await Assert.That(hashSet).Contains(20);

    var queue = new Queue<string>(new[] { "first", "second" });
    await Assert.That(queue).Contains("first");
}
```

### Contains (Predicate)

Tests that a collection contains an item matching a predicate, and returns that item:

```csharp
[Test]
public async Task Collection_Contains_Matching_Item()
{
    var users = new[]
    {
        new User { Name = "Alice", Age = 30 },
        new User { Name = "Bob", Age = 25 }
    };

    // Returns the found item
    var user = await Assert.That(users).Contains(u => u.Name == "Alice");

    // Can assert on the returned item
    await Assert.That(user.Age).IsEqualTo(30);
}
```

### DoesNotContain (Item)

Tests that a collection does not contain a specific item:

```csharp
[Test]
public async Task Collection_Does_Not_Contain()
{
    var numbers = new[] { 1, 2, 3, 4, 5 };

    await Assert.That(numbers).DoesNotContain(10);
    await Assert.That(numbers).DoesNotContain(0);
}
```

### DoesNotContain (Predicate)

Tests that no items match the predicate:

```csharp
[Test]
public async Task Collection_Does_Not_Contain_Matching()
{
    var users = new[]
    {
        new User { Name = "Alice", Age = 30 },
        new User { Name = "Bob", Age = 25 }
    };

    await Assert.That(users).DoesNotContain(u => u.Age > 50);
    await Assert.That(users).DoesNotContain(u => u.Name == "Charlie");
}
```

## Count Assertions

### HasCount

Tests that a collection has an exact count:

```csharp
[Test]
public async Task Collection_Has_Count()
{
    var numbers = new[] { 1, 2, 3, 4, 5 };

    await Assert.That(numbers).HasCount(5);
}
```

### Count with Comparison

Get the count for further assertions:

```csharp
[Test]
public async Task Count_With_Comparison()
{
    var numbers = new[] { 1, 2, 3, 4, 5 };

    await Assert.That(numbers)
        .HasCount().EqualTo(5);

    await Assert.That(numbers)
        .HasCount().GreaterThan(3)
        .And.HasCount().LessThan(10);
}
```

### Count with Predicate

Count items matching a predicate:

```csharp
[Test]
public async Task Count_With_Predicate()
{
    var numbers = new[] { 1, 2, 3, 4, 5, 6 };

    // Count even numbers
    var evenCount = await Assert.That(numbers)
        .Count(n => n % 2 == 0);

    await Assert.That(evenCount).IsEqualTo(3);
}
```

### IsEmpty

Tests that a collection has no items:

```csharp
[Test]
public async Task Collection_Is_Empty()
{
    var empty = new List<int>();

    await Assert.That(empty).IsEmpty();
    await Assert.That(empty).HasCount(0);
}
```

### IsNotEmpty

Tests that a collection has at least one item:

```csharp
[Test]
public async Task Collection_Is_Not_Empty()
{
    var numbers = new[] { 1 };

    await Assert.That(numbers).IsNotEmpty();
}
```

### HasSingleItem

Tests that a collection has exactly one item, and returns that item:

```csharp
[Test]
public async Task Collection_Has_Single_Item()
{
    var users = new[] { new User { Name = "Alice", Age = 30 } };

    var user = await Assert.That(users).HasSingleItem();

    await Assert.That(user.Name).IsEqualTo("Alice");
}
```

## Ordering Assertions

### IsInOrder

Tests that a collection is sorted in ascending order:

```csharp
[Test]
public async Task Collection_In_Ascending_Order()
{
    var numbers = new[] { 1, 2, 3, 4, 5 };

    await Assert.That(numbers).IsInOrder();
}
```

```csharp
[Test]
public async Task Strings_In_Order()
{
    var names = new[] { "Alice", "Bob", "Charlie" };

    await Assert.That(names).IsInOrder();
}
```

### IsInDescendingOrder

Tests that a collection is sorted in descending order:

```csharp
[Test]
public async Task Collection_In_Descending_Order()
{
    var numbers = new[] { 5, 4, 3, 2, 1 };

    await Assert.That(numbers).IsInDescendingOrder();
}
```

### IsOrderedBy

Tests that a collection is ordered by a specific property:

```csharp
[Test]
public async Task Ordered_By_Property()
{
    var users = new[]
    {
        new User { Name = "Alice", Age = 25 },
        new User { Name = "Bob", Age = 30 },
        new User { Name = "Charlie", Age = 35 }
    };

    await Assert.That(users).IsOrderedBy(u => u.Age);
}
```

### IsOrderedByDescending

Tests that a collection is ordered by a property in descending order:

```csharp
[Test]
public async Task Ordered_By_Descending()
{
    var users = new[]
    {
        new User { Name = "Charlie", Age = 35 },
        new User { Name = "Bob", Age = 30 },
        new User { Name = "Alice", Age = 25 }
    };

    await Assert.That(users).IsOrderedByDescending(u => u.Age);
}
```

## Predicate-Based Assertions

### All

Tests that all items satisfy a condition:

```csharp
[Test]
public async Task All_Items_Match()
{
    var numbers = new[] { 2, 4, 6, 8 };

    await Assert.That(numbers).All(n => n % 2 == 0);
}
```

#### With Satisfy

Chain additional assertions on all items:

```csharp
[Test]
public async Task All_Satisfy()
{
    var users = new[]
    {
        new User { Name = "Alice", Age = 25 },
        new User { Name = "Bob", Age = 30 }
    };

    await Assert.That(users)
        .All()
        .Satisfy(u => Assert.That(u.Age).IsGreaterThan(18));
}
```

#### With Mapper

Map items before asserting:

```csharp
[Test]
public async Task All_Satisfy_With_Mapper()
{
    var users = new[]
    {
        new User { Name = "Alice", Age = 25 },
        new User { Name = "Bob", Age = 30 }
    };

    await Assert.That(users)
        .All()
        .Satisfy(
            u => u.Name,
            name => Assert.That(name).IsNotEmpty()
        );
}
```

### Any

Tests that at least one item satisfies a condition:

```csharp
[Test]
public async Task Any_Item_Matches()
{
    var numbers = new[] { 1, 3, 5, 6, 7 };

    await Assert.That(numbers).Any(n => n % 2 == 0);
}
```

## Equivalency Assertions

### IsEquivalentTo

Tests that two collections contain the same items, regardless of order:

```csharp
[Test]
public async Task Collections_Are_Equivalent()
{
    var actual = new[] { 1, 2, 3, 4, 5 };
    var expected = new[] { 5, 4, 3, 2, 1 };

    await Assert.That(actual).IsEquivalentTo(expected);
}
```

Different collection types:

```csharp
[Test]
public async Task Different_Collection_Types()
{
    var list = new List<int> { 1, 2, 3 };
    var array = new[] { 3, 2, 1 };

    await Assert.That(list).IsEquivalentTo(array);
}
```

#### With Custom Comparer

```csharp
[Test]
public async Task Equivalent_With_Comparer()
{
    var actual = new[] { "apple", "banana", "cherry" };
    var expected = new[] { "APPLE", "BANANA", "CHERRY" };

    await Assert.That(actual)
        .IsEquivalentTo(expected)
        .Using(StringComparer.OrdinalIgnoreCase);
}
```

#### With Custom Equality Predicate

```csharp
[Test]
public async Task Equivalent_With_Predicate()
{
    var users1 = new[]
    {
        new User { Name = "Alice", Age = 30 },
        new User { Name = "Bob", Age = 25 }
    };

    var users2 = new[]
    {
        new User { Name = "Bob", Age = 25 },
        new User { Name = "Alice", Age = 30 }
    };

    await Assert.That(users1)
        .IsEquivalentTo(users2)
        .Using((u1, u2) => u1.Name == u2.Name && u1.Age == u2.Age);
}
```

#### Explicitly Ignoring Order

```csharp
[Test]
public async Task Equivalent_Ignoring_Order()
{
    var actual = new[] { 1, 2, 3 };
    var expected = new[] { 3, 2, 1 };

    await Assert.That(actual)
        .IsEquivalentTo(expected)
        .IgnoringOrder();
}
```

### IsNotEquivalentTo

Tests that collections are not equivalent:

```csharp
[Test]
public async Task Collections_Not_Equivalent()
{
    var actual = new[] { 1, 2, 3 };
    var different = new[] { 4, 5, 6 };

    await Assert.That(actual).IsNotEquivalentTo(different);
}
```

## Structural Equivalency

### IsStructurallyEqualTo

Deep comparison of collections including nested objects:

```csharp
[Test]
public async Task Structurally_Equal()
{
    var actual = new[]
    {
        new { Name = "Alice", Address = new { City = "Seattle" } },
        new { Name = "Bob", Address = new { City = "Portland" } }
    };

    var expected = new[]
    {
        new { Name = "Alice", Address = new { City = "Seattle" } },
        new { Name = "Bob", Address = new { City = "Portland" } }
    };

    await Assert.That(actual).IsStructurallyEqualTo(expected);
}
```

### IsNotStructurallyEqualTo

```csharp
[Test]
public async Task Not_Structurally_Equal()
{
    var actual = new[]
    {
        new { Name = "Alice", Age = 30 }
    };

    var different = new[]
    {
        new { Name = "Alice", Age = 31 }
    };

    await Assert.That(actual).IsNotStructurallyEqualTo(different);
}
```

## Distinctness

### HasDistinctItems

Tests that all items in a collection are unique:

```csharp
[Test]
public async Task All_Items_Distinct()
{
    var numbers = new[] { 1, 2, 3, 4, 5 };

    await Assert.That(numbers).HasDistinctItems();
}
```

Fails if duplicates exist:

```csharp
[Test]
public async Task Duplicates_Fail()
{
    var numbers = new[] { 1, 2, 2, 3 };

    // This will fail
    // await Assert.That(numbers).HasDistinctItems();
}
```

## Practical Examples

### Filtering Results

```csharp
[Test]
public async Task Filter_And_Assert()
{
    var numbers = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    var evens = numbers.Where(n => n % 2 == 0).ToArray();

    await Assert.That(evens)
        .HasCount(5)
        .And.All(n => n % 2 == 0);
}
```

### LINQ Query Results

```csharp
[Test]
public async Task LINQ_Query_Results()
{
    var users = new[]
    {
        new User { Name = "Alice", Age = 25 },
        new User { Name = "Bob", Age = 30 },
        new User { Name = "Charlie", Age = 35 }
    };

    var adults = users.Where(u => u.Age >= 18).ToArray();

    await Assert.That(adults)
        .HasCount(3)
        .And.All(u => u.Age >= 18);
}
```

### Sorting Validation

```csharp
[Test]
public async Task Verify_Sorting()
{
    var unsorted = new[] { 5, 2, 8, 1, 9 };
    var sorted = unsorted.OrderBy(x => x).ToArray();

    await Assert.That(sorted).IsInOrder();
    await Assert.That(sorted).IsEquivalentTo(unsorted);
}
```

### API Response Validation

```csharp
[Test]
public async Task API_Returns_Expected_Items()
{
    var response = await _api.GetUsersAsync();

    await Assert.That(response)
        .IsNotEmpty()
        .And.All(u => u.Id > 0)
        .And.All(u => !string.IsNullOrEmpty(u.Name));
}
```

### Collection Transformation

```csharp
[Test]
public async Task Map_And_Verify()
{
    var users = new[]
    {
        new User { Name = "Alice", Age = 25 },
        new User { Name = "Bob", Age = 30 }
    };

    var names = users.Select(u => u.Name).ToArray();

    await Assert.That(names)
        .HasCount(2)
        .And.Contains("Alice")
        .And.Contains("Bob")
        .And.All(name => !string.IsNullOrEmpty(name));
}
```

## Empty vs Null Collections

```csharp
[Test]
public async Task Empty_vs_Null()
{
    List<int>? nullList = null;
    List<int> emptyList = new();
    List<int> populated = new() { 1, 2, 3 };

    await Assert.That(nullList).IsNull();
    await Assert.That(emptyList).IsNotNull();
    await Assert.That(emptyList).IsEmpty();
    await Assert.That(populated).IsNotEmpty();
}
```

## Nested Collections

```csharp
[Test]
public async Task Nested_Collections()
{
    var matrix = new[]
    {
        new[] { 1, 2, 3 },
        new[] { 4, 5, 6 },
        new[] { 7, 8, 9 }
    };

    await Assert.That(matrix).HasCount(3);
    await Assert.That(matrix).All(row => row.Length == 3);

    // Flatten and assert
    var flattened = matrix.SelectMany(x => x).ToArray();
    await Assert.That(flattened).HasCount(9);
}
```

## Collection of Collections

```csharp
[Test]
public async Task Collection_Of_Collections()
{
    var groups = new List<List<int>>
    {
        new() { 1, 2 },
        new() { 3, 4, 5 },
        new() { 6 }
    };

    await Assert.That(groups)
        .HasCount(3)
        .And.All(group => group.Count > 0);
}
```

## Chaining Collection Assertions

```csharp
[Test]
public async Task Chained_Collection_Assertions()
{
    var numbers = new[] { 1, 2, 3, 4, 5 };

    await Assert.That(numbers)
        .IsNotEmpty()
        .And.HasCount(5)
        .And.Contains(3)
        .And.DoesNotContain(10)
        .And.IsInOrder()
        .And.All(n => n > 0)
        .And.Any(n => n == 5);
}
```

## Performance Considerations

### Materialize IEnumerable

```csharp
[Test]
public async Task Materialize_Before_Multiple_Assertions()
{
    // This query is deferred
    IEnumerable<int> query = Enumerable.Range(1, 1000000)
        .Where(n => n % 2 == 0);

    // Materialize once to avoid re-execution
    var materialized = query.ToArray();

    await Assert.That(materialized).HasCount().GreaterThan(1000);
    await Assert.That(materialized).Contains(100);
    await Assert.That(materialized).All(n => n % 2 == 0);
}
```

## Working with HashSet and SortedSet

```csharp
[Test]
public async Task HashSet_Assertions()
{
    var set = new HashSet<int> { 1, 2, 3, 4, 5 };

    await Assert.That(set)
        .HasCount(5)
        .And.Contains(3)
        .And.HasDistinctItems();
}

[Test]
public async Task SortedSet_Assertions()
{
    var sorted = new SortedSet<int> { 5, 2, 8, 1, 9 };

    await Assert.That(sorted)
        .IsInOrder()
        .And.HasDistinctItems();
}
```

## Common Patterns

### Validate All Items

```csharp
[Test]
public async Task Validate_Each_Item()
{
    var users = GetUsers();

    await using (Assert.Multiple())
    {
        foreach (var user in users)
        {
            await Assert.That(user.Name).IsNotEmpty();
            await Assert.That(user.Age).IsGreaterThan(0);
        }
    }
}
```

Or more elegantly:

```csharp
[Test]
public async Task Validate_All_With_Assertion()
{
    var users = GetUsers();

    await Assert.That(users).All(u =>
        !string.IsNullOrEmpty(u.Name) && u.Age > 0
    );
}
```

### Find and Assert

```csharp
[Test]
public async Task Find_And_Assert()
{
    var users = GetUsers();

    var admin = await Assert.That(users)
        .Contains(u => u.Role == "Admin");

    await Assert.That(admin.Permissions).IsNotEmpty();
}
```

## See Also

- [Dictionaries](dictionaries.md) - Dictionary-specific assertions
- [Strings](string.md) - String collections
- [Equality & Comparison](equality-and-comparison.md) - Item comparison
