namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests to verify that collection assertion types persist through And/Or continuations,
/// enabling instance methods to remain available throughout the chain.
/// </summary>
public class SelfTypedCollectionTests
{
    [Test]
    public async Task Collection_Contains_And_Contains_UsesInstanceMethods()
    {
        int[] array = [1, 2, 3, 4, 5];

        // This should use instance methods from CollectionAssertionBase
        // Both Contains() calls should be instance methods, not extensions
        await Assert.That(array)
            .Contains(1)
            .And  // Should return SelfTypedAndContinuation that inherits from CollectionAssertionBase
            .Contains(2);  // Should be available as instance method!
    }

    [Test]
    public async Task Collection_Contains_And_ContainsOnly()
    {
        int[] array = [1, 2, 3];

        await Assert.That(array)
            .Contains(1)
            .And
            .ContainsOnly(x => x < 10);  // Instance method should be available
    }

    [Test]
    public async Task Collection_Multiple_And_Chains()
    {
        int[] array = [1, 2, 3, 4, 5];

        await Assert.That(array)
            .Contains(1)
            .And.Contains(2)
            .And.Contains(3)
            .And.HasCount(5);
    }

    [Test]
    public async Task Collection_With_Predicate_And_Chain()
    {
        int[] array = [2, 4, 6, 8];

        await Assert.That(array)
            .All(x => x % 2 == 0)  // All are even
            .And
            .Contains(4);  // And contains 4
    }

    [Test]
    public async Task Collection_Or_Chain()
    {
        int[] array = [1, 2, 3];

        await Assert.That(array)
            .Contains(10)  // This will fail
            .Or
            .Contains(1);  // But this will pass
    }

    [Test]
    public async Task Collection_Complex_Chain()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };

        await Assert.That(list)
            .Contains(1)
            .And.Contains(5)
            .And.HasCount(5)
            .And.IsNotEmpty();
    }

    // Note: IsInOrder() remains as extension method due to IComparable<TItem> constraint
    // It works on the initial assertion but has type inference issues after .And
    // This is a known limitation of the type system

    [Test]
    public async Task CustomCollection_PreservesType()
    {
        var collection = new CustomCollection("test") { "A", "B", "C" };

        // Verify that collection methods work on custom collection types
        await Assert.That(collection)
            .Contains("A")
            .And
            .Contains("B")
            .And
            .HasCount(3);
    }

    [Test]
    public async Task CustomCollection_ContainsWithPredicate_Issue3401()
    {
        var collection = new CustomCollection("test") { "A", "B", "C" };

        await Assert.That(collection).Contains(x => x == "A");
    }

    public class CustomCollection(string title) : List<string>
    {
        public string Title { get; } = title;
    }
}
