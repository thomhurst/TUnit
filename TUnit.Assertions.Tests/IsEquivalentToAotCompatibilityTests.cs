using System.Collections.Generic;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests to verify that IsEquivalentTo and IsNotEquivalentTo work with custom comparers
/// and are AOT-compatible (no reflection needed when using custom comparers).
/// </summary>
public class IsEquivalentToAotCompatibilityTests
{
    [Test]
    public async Task IsEquivalentTo_WithEqualityComparerDefault_ShouldPass()
    {
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 3, 2, 1 };

        await Assert.That(list1).IsEquivalentTo(list2, comparer: EqualityComparer<int>.Default);
    }

    [Test]
    public async Task IsEquivalentTo_WithCustomComparer_ShouldPass()
    {
        var list1 = new List<string> { "apple", "BANANA", "cherry" };
        var list2 = new List<string> { "CHERRY", "APPLE", "banana" };

        await Assert.That(list1).IsEquivalentTo(list2, comparer: StringComparer.OrdinalIgnoreCase);
    }

    [Test]
    public async Task IsEquivalentTo_WithCustomComparer_ShouldFail()
    {
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 4, 5, 6 };

        await Assert.That(async () => await Assert.That(list1).IsEquivalentTo(list2, comparer: EqualityComparer<int>.Default))
            .ThrowsException();
    }

    [Test]
    public async Task IsNotEquivalentTo_WithEqualityComparerDefault_ShouldPass()
    {
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 4, 5, 6 };

        await Assert.That(list1).IsNotEquivalentTo(list2, comparer: EqualityComparer<int>.Default);
    }

    [Test]
    public async Task IsNotEquivalentTo_WithCustomComparer_ShouldPass()
    {
        var list1 = new List<string> { "apple", "banana" };
        var list2 = new List<string> { "CHERRY", "DURIAN" };

        await Assert.That(list1).IsNotEquivalentTo(list2, comparer: StringComparer.OrdinalIgnoreCase);
    }

    [Test]
    public async Task IsNotEquivalentTo_WithCustomComparer_ShouldFail()
    {
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 3, 2, 1 };

        await Assert.That(async () => await Assert.That(list1).IsNotEquivalentTo(list2, comparer: EqualityComparer<int>.Default))
            .ThrowsException();
    }

    [Test]
    public async Task IsEquivalentTo_WithCustomComparerAndOrdering_ShouldWork()
    {
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 1, 2, 3 };

        await Assert.That(list1).IsEquivalentTo(
            list2, 
            comparer: EqualityComparer<int>.Default, 
            ordering: Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task IsEquivalentTo_WithUsingMethod_ShouldWork()
    {
        var list1 = new List<string> { "apple", "BANANA", "cherry" };
        var list2 = new List<string> { "CHERRY", "APPLE", "banana" };

        await Assert.That(list1)
            .IsEquivalentTo(list2, comparer: StringComparer.Ordinal)
            .Using(StringComparer.OrdinalIgnoreCase);
    }

    private class Person
    {
        public string Name { get; init; } = "";
        public int Age { get; init; }
    }

    private class PersonNameComparer : IEqualityComparer<Person>
    {
        public bool Equals(Person? x, Person? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Name == y.Name;
        }

        public int GetHashCode(Person obj) => obj.Name.GetHashCode();
    }

    [Test]
    public async Task IsEquivalentTo_WithComplexTypeAndCustomComparer_ShouldWork()
    {
        var list1 = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Bob", Age = 25 }
        };
        var list2 = new List<Person>
        {
            new() { Name = "Bob", Age = 99 }, // Different age, but comparer only checks Name
            new() { Name = "Alice", Age = 99 }
        };

        // This works without reflection because we're using a custom comparer
        await Assert.That(list1).IsEquivalentTo(list2, comparer: new PersonNameComparer());
    }
}
