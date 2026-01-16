namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Tests for issue #4446: No Assert.That overload for ICollection/ICollection&lt;T&gt;
/// https://github.com/thomhurst/TUnit/issues/4446
/// </summary>
public class Issue4446Tests
{
    [Test]
    public async Task ICollection_Should_Support_IsEmpty()
    {
        ICollection<int> emptyCollection = new List<int>();
        await Assert.That(emptyCollection).IsEmpty();
    }

    [Test]
    public async Task ICollection_Should_Support_IsNotEmpty()
    {
        ICollection<int> collection = new List<int> { 1, 2, 3 };
        await Assert.That(collection).IsNotEmpty();
    }

    [Test]
    public async Task ICollection_Should_Support_Contains()
    {
        ICollection<int> collection = new List<int> { 1, 2, 3 };
        await Assert.That(collection).Contains(2);
    }

    [Test]
    public async Task ICollection_Should_Support_Count()
    {
        ICollection<int> collection = new List<int> { 1, 2, 3 };
        await Assert.That(collection).HasCount().EqualTo(3);
    }

    [Test]
    public async Task ICollection_Should_Support_ContainsOnly()
    {
        ICollection<int> collection = new List<int> { 2, 4, 6 };
        await Assert.That(collection).ContainsOnly(x => x % 2 == 0);
    }

    [Test]
    public async Task ICollection_Should_Support_Contains_WithPredicate()
    {
        ICollection<int> collection = new List<int> { 1, 2, 3 };
        await Assert.That(collection).Contains(x => x > 2);
    }

    [Test]
    public async Task IReadOnlyCollection_Should_Support_IsEmpty()
    {
        IReadOnlyCollection<int> emptyCollection = Array.Empty<int>();
        await Assert.That(emptyCollection).IsEmpty();
    }

    [Test]
    public async Task IReadOnlyCollection_Should_Support_IsNotEmpty()
    {
        IReadOnlyCollection<int> collection = new List<int> { 1, 2, 3 };
        await Assert.That(collection).IsNotEmpty();
    }

    [Test]
    public async Task IReadOnlyCollection_Should_Support_Contains()
    {
        IReadOnlyCollection<int> collection = new List<int> { 1, 2, 3 };
        await Assert.That(collection).Contains(2);
    }

    [Test]
    public async Task IReadOnlyCollection_Should_Support_Count()
    {
        IReadOnlyCollection<int> collection = new List<int> { 1, 2, 3 };
        await Assert.That(collection).HasCount().EqualTo(3);
    }

    [Test]
    public async Task IReadOnlyCollection_Should_Support_IsInOrder()
    {
        IReadOnlyCollection<int> collection = new List<int> { 1, 2, 3 };
        await Assert.That(collection).IsInOrder();
    }

    [Test]
    public async Task ICollection_Should_Support_IsInOrder()
    {
        ICollection<int> collection = new List<int> { 1, 2, 3 };
        await Assert.That(collection).IsInOrder();
    }

    [Test]
    public async Task ICollection_Should_Support_DoesNotContain()
    {
        ICollection<int> collection = new List<int> { 1, 2, 3 };
        await Assert.That(collection).DoesNotContain(5);
    }
}
