using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class CollectionAssertionTests
{
    [Test]
    public async Task IsEmpty()
    {
        var items = new List<int>();

        await Assert.That(items).IsEmpty();
    }

    [Test]
    public async Task IsEmpty2()
    {
        var items = new List<int>();

        await Assert.That(() => items).IsEmpty();
    }

    [Test]
    public async Task Count()
    {
        var items = new List<int>();

        await Assert.That(items).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Count2()
    {
        var items = new List<int>();

        await Assert.That(() => items).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Count_WithInnerAssertion_IsGreaterThan()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };

        // Count items where item > 2 using inner assertion builder
        await Assert.That(items).Count(item => item.IsGreaterThan(2)).IsEqualTo(3);
    }

    [Test]
    public async Task Count_WithInnerAssertion_IsLessThan()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };

        // Count items where item < 4 using inner assertion builder
        await Assert.That(items).Count(item => item.IsLessThan(4)).IsEqualTo(3);
    }

    [Test]
    public async Task Count_WithInnerAssertion_IsBetween()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };

        // Count items where 2 <= item <= 4 using inner assertion builder
        await Assert.That(items).Count(item => item.IsBetween(2, 4)).IsEqualTo(3);
    }

    [Test]
    public async Task Count_WithInnerAssertion_String_Contains()
    {
        var items = new List<string> { "apple", "banana", "apricot", "cherry" };

        // Count items that contain "ap" using inner assertion builder
        await Assert.That(items).Count(item => item.Contains("ap")).IsEqualTo(2);
    }

    [Test]
    public async Task Count_WithInnerAssertion_String_StartsWith()
    {
        var items = new List<string> { "apple", "banana", "apricot", "cherry" };

        // Count items that start with "a" using inner assertion builder
        await Assert.That(items).Count(item => item.StartsWith("a")).IsEqualTo(2);
    }

    [Test]
    public async Task Count_WithInnerAssertion_EmptyCollection()
    {
        var items = new List<int>();

        // Count on empty collection should return 0
        await Assert.That(items).Count(item => item.IsGreaterThan(0)).IsEqualTo(0);
    }

    [Test]
    public async Task Count_WithInnerAssertion_NoneMatch()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };

        // Count items > 10 (none match)
        await Assert.That(items).Count(item => item.IsGreaterThan(10)).IsEqualTo(0);
    }

    [Test]
    public async Task Count_WithInnerAssertion_AllMatch()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };

        // Count items > 0 (all match)
        await Assert.That(items).Count(item => item.IsGreaterThan(0)).IsEqualTo(5);
    }

    [Test]
    public async Task Count_WithInnerAssertion_Lambda_Collection()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };

        // Test with lambda-wrapped collection
        await Assert.That(() => items).Count(item => item.IsGreaterThan(2)).IsEqualTo(3);
    }

    // Tests for collection chaining after Count assertions

    [Test]
    public async Task Count_ThenAnd_Contains()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };

        // Count and then chain with Contains
        await Assert.That(items)
            .Count().IsEqualTo(5)
            .And.Contains(3);
    }

    [Test]
    public async Task Count_ThenAnd_IsNotEmpty()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };

        // Count and then chain with IsNotEmpty
        await Assert.That(items)
            .Count().IsGreaterThan(0)
            .And.IsNotEmpty();
    }

    [Test]
    public async Task Count_WithInnerAssertion_ThenAnd_Contains()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };

        // Count with inner assertion and then chain with Contains
        await Assert.That(items)
            .Count(item => item.IsGreaterThan(2)).IsEqualTo(3)
            .And.Contains(5);
    }

    [Test]
    public async Task Count_ThenAnd_All()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };

        // Count and then chain with All
        await Assert.That(items)
            .Count().IsEqualTo(5)
            .And.All(x => x > 0);
    }

    [Test]
    public async Task Count_ThenAnd_Count()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };

        // Chain multiple Count assertions
        await Assert.That(items)
            .Count().IsGreaterThan(3)
            .And.Count().IsLessThan(10);
    }

    [Test]
    public async Task Count_WithInnerAssertion_ThenAnd_IsInOrder()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };

        // Count with inner assertion and then check ordering
        await Assert.That(items)
            .Count(item => item.IsGreaterThan(0)).IsEqualTo(5)
            .And.IsInOrder();
    }

    [Test]
    public async Task Count_IsGreaterThan()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };

        await Assert.That(items).Count().IsGreaterThan(3);
    }

    [Test]
    public async Task Count_IsLessThan()
    {
        var items = new List<int> { 1, 2, 3 };

        await Assert.That(items).Count().IsLessThan(5);
    }

    [Test]
    public async Task Count_IsGreaterThanOrEqualTo()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };

        await Assert.That(items).Count().IsGreaterThanOrEqualTo(5);
    }

    [Test]
    public async Task Count_IsLessThanOrEqualTo()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };

        await Assert.That(items).Count().IsLessThanOrEqualTo(5);
    }

    [Test]
    public async Task Count_IsZero()
    {
        var items = new List<int>();

        await Assert.That(items).Count().IsZero();
    }

    [Test]
    public async Task Count_IsPositive()
    {
        var items = new List<int> { 1 };

        await Assert.That(items).Count().IsPositive();
    }

    [Test]
    public async Task Count_IsNotEqualTo()
    {
        var items = new List<int> { 1, 2, 3 };

        await Assert.That(items).Count().IsNotEqualTo(5);
    }

    [Test]
    public async Task Chained_Collection_Assertions()
    {
        var numbers = new[] { 1, 2, 3, 4, 5 };

        // For collections of int, use Count().IsEqualTo(5) instead of Count(c => c.IsEqualTo(5))
        // to avoid ambiguity with item-filtering
        await Assert.That(numbers)
            .IsNotEmpty()
            .And.Count().IsEqualTo(5)
            .And.Contains(3)
            .And.DoesNotContain(10)
            .And.IsInOrder()
            .And.All(n => n > 0)
            .And.Any(n => n == 5);
    }

    [Test]
    public async Task Chained_Collection_Assertions_WithStrings()
    {
        var names = new[] { "Alice", "Bob", "Charlie" };

        // For non-int collections, Count(c => c.IsEqualTo(3)) works unambiguously
        await Assert.That(names)
            .IsNotEmpty()
            .And.Count(c => c.IsEqualTo(3))
            .And.Contains("Bob")
            .And.DoesNotContain("Dave");
    }

    [Test]
    public async Task All_Predicate_Failure_Message_Contains_Index_And_Value()
    {
        var items = new[] { 2, 4, -5, 8 };

        await Assert.That(async () =>
            await Assert.That(items).All(x => x > 0)
        ).Throws<AssertionException>()
        .WithMessageContaining("index 2")
        .And.WithMessageContaining("[-5]");
    }

    [Test]
    public async Task All_Predicate_Failure_Message_Contains_String_Value()
    {
        var names = new[] { "Alice", "Bob", "" };

        await Assert.That(async () =>
            await Assert.That(names).All(x => !string.IsNullOrEmpty(x))
        ).Throws<AssertionException>()
        .WithMessageContaining("index 2")
        .And.WithMessageContaining("[]");
    }

    [Test]
    public async Task All_Predicate_Failure_Message_Contains_First_Failing_Item()
    {
        var items = new[] { 1, 2, 3, -1, -2, -3 };

        await Assert.That(async () =>
            await Assert.That(items).All(x => x > 0)
        ).Throws<AssertionException>()
        .WithMessageContaining("index 3")
        .And.WithMessageContaining("[-1]");
    }
}
