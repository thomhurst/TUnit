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
    public async Task Count_WithPredicate()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };

        await Assert.That(items).Count(x => x > 2).IsEqualTo(3);
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
}
