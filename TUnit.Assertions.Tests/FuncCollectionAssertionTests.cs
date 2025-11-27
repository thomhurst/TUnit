using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests for FuncCollectionAssertion - verifies that collection assertions work when
/// collections are wrapped in lambdas. Addresses GitHub issue #3910.
/// </summary>
public class FuncCollectionAssertionTests
{
    [Test]
    public async Task Lambda_Collection_IsEmpty_Passes_For_Empty_Collection()
    {
        var items = new List<int>();
        await Assert.That(() => items).IsEmpty();
    }

    [Test]
    public async Task Lambda_Collection_IsEmpty_Fails_For_NonEmpty_Collection()
    {
        var items = new List<int> { 1, 2, 3 };
        await Assert.That(async () => await Assert.That(() => items).IsEmpty())
            .Throws<TUnitAssertionException>();
    }

    [Test]
    public async Task Lambda_Collection_IsNotEmpty_Passes_For_NonEmpty_Collection()
    {
        var items = new List<int> { 1, 2, 3 };
        await Assert.That(() => items).IsNotEmpty();
    }

    [Test]
    public async Task Lambda_Collection_IsNotEmpty_Fails_For_Empty_Collection()
    {
        var items = new List<int>();
        await Assert.That(async () => await Assert.That(() => items).IsNotEmpty())
            .Throws<TUnitAssertionException>();
    }

    [Test]
    public async Task Lambda_Collection_Count_IsEqualTo_Passes()
    {
        var items = new List<int> { 1, 2, 3 };
        await Assert.That(() => items).Count().IsEqualTo(3);
    }

    [Test]
    public async Task Lambda_Collection_Count_IsGreaterThan_Passes()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };
        await Assert.That(() => items).Count().IsGreaterThan(3);
    }

    [Test]
    public async Task Lambda_Collection_Contains_Passes()
    {
        var items = new List<int> { 1, 2, 3 };
        await Assert.That(() => items).Contains(2);
    }

    [Test]
    public async Task Lambda_Collection_Contains_Fails_When_Item_Not_Present()
    {
        var items = new List<int> { 1, 2, 3 };
        await Assert.That(async () => await Assert.That(() => items).Contains(99))
            .Throws<TUnitAssertionException>();
    }

    [Test]
    public async Task Lambda_Collection_DoesNotContain_Passes()
    {
        var items = new List<int> { 1, 2, 3 };
        await Assert.That(() => items).DoesNotContain(99);
    }

    [Test]
    public async Task Lambda_Collection_HasSingleItem_Passes()
    {
        var items = new List<int> { 42 };
        await Assert.That(() => items).HasSingleItem();
    }

    [Test]
    public async Task Lambda_Collection_All_Passes()
    {
        var items = new List<int> { 2, 4, 6 };
        await Assert.That(() => items).All(x => x % 2 == 0);
    }

    [Test]
    public async Task Lambda_Collection_Any_Passes()
    {
        var items = new List<int> { 1, 2, 3 };
        await Assert.That(() => items).Any(x => x > 2);
    }

    [Test]
    public async Task Lambda_Collection_IsInOrder_Passes()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };
        await Assert.That(() => items).IsInOrder();
    }

    [Test]
    public async Task Lambda_Collection_IsInDescendingOrder_Passes()
    {
        var items = new List<int> { 5, 4, 3, 2, 1 };
        await Assert.That(() => items).IsInDescendingOrder();
    }

    [Test]
    public async Task Lambda_Collection_HasDistinctItems_Passes()
    {
        var items = new List<int> { 1, 2, 3 };
        await Assert.That(() => items).HasDistinctItems();
    }

    [Test]
    public async Task Lambda_Collection_Throws_Passes_When_Exception_Thrown()
    {
        await Assert.That(() => ThrowingMethod()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Lambda_Collection_Chaining_With_And()
    {
        var items = new List<int> { 1, 2, 3 };
        await Assert.That(() => items)
            .IsNotEmpty()
            .And.Contains(2)
            .And.HasDistinctItems();
    }

    [Test]
    public async Task Lambda_Array_IsEmpty_Passes()
    {
        var items = Array.Empty<string>();
        await Assert.That(() => items).IsEmpty();
    }

    [Test]
    public async Task Lambda_Array_IsNotEmpty_Passes()
    {
        var items = new[] { "a", "b", "c" };
        await Assert.That(() => items).IsNotEmpty();
    }

    [Test]
    public async Task Lambda_Enumerable_IsEmpty_Passes()
    {
        IEnumerable<int> items = Enumerable.Empty<int>();
        await Assert.That(() => items).IsEmpty();
    }

    [Test]
    public async Task Lambda_HashSet_Contains_Passes()
    {
        var items = new HashSet<int> { 1, 2, 3 };
        await Assert.That(() => items).Contains(2);
    }

    private static IEnumerable<int> ThrowingMethod()
    {
        throw new InvalidOperationException("Test exception");
    }

    // Async lambda tests
    [Test]
    public async Task AsyncLambda_Collection_IsEmpty_Passes()
    {
        await Assert.That(async () => await GetEmptyCollectionAsync()).IsEmpty();
    }

    [Test]
    public async Task AsyncLambda_Collection_IsNotEmpty_Passes()
    {
        await Assert.That(async () => await GetCollectionAsync()).IsNotEmpty();
    }

    [Test]
    public async Task AsyncLambda_Collection_Count_IsEqualTo_Passes()
    {
        await Assert.That(async () => await GetCollectionAsync()).Count().IsEqualTo(3);
    }

    [Test]
    public async Task AsyncLambda_Collection_Contains_Passes()
    {
        await Assert.That(async () => await GetCollectionAsync()).Contains(2);
    }

    [Test]
    public async Task AsyncLambda_Collection_All_Passes()
    {
        await Assert.That(async () => await GetCollectionAsync()).All(x => x > 0);
    }

    [Test]
    public async Task AsyncLambda_Collection_Throws_Passes()
    {
        await Assert.That(async () => await ThrowingMethodAsync()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task AsyncLambda_Collection_Chaining_With_And()
    {
        await Assert.That(async () => await GetCollectionAsync())
            .IsNotEmpty()
            .And.Contains(2)
            .And.HasDistinctItems();
    }

    private static Task<IEnumerable<int>> GetEmptyCollectionAsync()
    {
        return Task.FromResult<IEnumerable<int>>(new List<int>());
    }

    private static Task<IEnumerable<int>> GetCollectionAsync()
    {
        return Task.FromResult<IEnumerable<int>>(new List<int> { 1, 2, 3 });
    }

    private static async Task<IEnumerable<int>> ThrowingMethodAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException("Test exception");
    }
}
