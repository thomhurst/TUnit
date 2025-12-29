using TUnit.Assertions.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class AsyncEnumerableAssertionTests
{
    #region Helper methods

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(params T[] items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    private static async IAsyncEnumerable<int> EmptyAsyncEnumerable()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async IAsyncEnumerable<int> DelayedItems(
        int count,
        TimeSpan delay,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < count; i++)
        {
            await Task.Delay(delay, cancellationToken);
            yield return i;
        }
    }

    #endregion

    #region IsEmpty

    [Test]
    public async Task IsEmpty_WithEmptySequence_Passes()
    {
        var empty = EmptyAsyncEnumerable();
        await Assert.That(empty).IsEmpty();
    }

    [Test]
    public async Task IsEmpty_WithNonEmptySequence_Fails()
    {
        var items = ToAsyncEnumerable(1, 2, 3);
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(items).IsEmpty());
    }

    #endregion

    #region IsNotEmpty

    [Test]
    public async Task IsNotEmpty_WithNonEmptySequence_Passes()
    {
        var items = ToAsyncEnumerable(1, 2, 3);
        await Assert.That(items).IsNotEmpty();
    }

    [Test]
    public async Task IsNotEmpty_WithEmptySequence_Fails()
    {
        var empty = EmptyAsyncEnumerable();
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(empty).IsNotEmpty());
    }

    #endregion

    #region YieldsCount

    [Test]
    public async Task YieldsCount_WithMatchingCount_Passes()
    {
        var items = ToAsyncEnumerable(1, 2, 3);
        await Assert.That(items).YieldsCount(3);
    }

    [Test]
    public async Task YieldsCount_WithDifferentCount_Fails()
    {
        var items = ToAsyncEnumerable(1, 2, 3);
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(items).YieldsCount(5));
    }

    [Test]
    public async Task YieldsCount_WithEmptySequence_Passes()
    {
        var empty = EmptyAsyncEnumerable();
        await Assert.That(empty).YieldsCount(0);
    }

    #endregion

    #region YieldsExactly

    [Test]
    public async Task YieldsExactly_WithMatchingItems_Passes()
    {
        var items = ToAsyncEnumerable(1, 2, 3);
        await Assert.That(items).YieldsExactly(1, 2, 3);
    }

    [Test]
    public async Task YieldsExactly_WithDifferentItems_Fails()
    {
        var items = ToAsyncEnumerable(1, 2, 3);
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(items).YieldsExactly(1, 2, 4));
    }

    [Test]
    public async Task YieldsExactly_WithDifferentOrder_Fails()
    {
        var items = ToAsyncEnumerable(1, 2, 3);
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(items).YieldsExactly(3, 2, 1));
    }

    [Test]
    public async Task YieldsExactly_WithDifferentCount_Fails()
    {
        var items = ToAsyncEnumerable(1, 2, 3);
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(items).YieldsExactly(1, 2));
    }

    [Test]
    public async Task YieldsExactly_WithStringItems_Passes()
    {
        var items = ToAsyncEnumerable("a", "b", "c");
        await Assert.That(items).YieldsExactly("a", "b", "c");
    }

    #endregion

    #region Contains

    [Test]
    public async Task Contains_WithItemPresent_Passes()
    {
        var items = ToAsyncEnumerable(1, 2, 3);
        await Assert.That(items).Contains(2);
    }

    [Test]
    public async Task Contains_WithItemAbsent_Fails()
    {
        var items = ToAsyncEnumerable(1, 2, 3);
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(items).Contains(5));
    }

    [Test]
    public async Task Contains_WithStringItem_Passes()
    {
        var items = ToAsyncEnumerable("apple", "banana", "cherry");
        await Assert.That(items).Contains("banana");
    }

    #endregion

    #region DoesNotContain

    [Test]
    public async Task DoesNotContain_WithItemAbsent_Passes()
    {
        var items = ToAsyncEnumerable(1, 2, 3);
        await Assert.That(items).DoesNotContain(5);
    }

    [Test]
    public async Task DoesNotContain_WithItemPresent_Fails()
    {
        var items = ToAsyncEnumerable(1, 2, 3);
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(items).DoesNotContain(2));
    }

    #endregion

    #region All

    [Test]
    public async Task All_WithAllMatchingPredicate_Passes()
    {
        var items = ToAsyncEnumerable(2, 4, 6);
        await Assert.That(items).All(x => x % 2 == 0);
    }

    [Test]
    public async Task All_WithSomeNotMatchingPredicate_Fails()
    {
        var items = ToAsyncEnumerable(2, 3, 6);
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(items).All(x => x % 2 == 0));
    }

    [Test]
    public async Task All_WithEmptySequence_Passes()
    {
        var empty = EmptyAsyncEnumerable();
        await Assert.That(empty).All(x => x > 0);
    }

    #endregion

    #region Any

    [Test]
    public async Task Any_WithSomeMatchingPredicate_Passes()
    {
        var items = ToAsyncEnumerable(1, 2, 3);
        await Assert.That(items).Any(x => x % 2 == 0);
    }

    [Test]
    public async Task Any_WithNoneMatchingPredicate_Fails()
    {
        var items = ToAsyncEnumerable(1, 3, 5);
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(items).Any(x => x % 2 == 0));
    }

    [Test]
    public async Task Any_WithEmptySequence_Fails()
    {
        var empty = EmptyAsyncEnumerable();
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(empty).Any(x => x > 0));
    }

    #endregion

    #region CompletesWithin

    [Test]
    public async Task CompletesWithin_WithFastSequence_Passes()
    {
        var items = ToAsyncEnumerable(1, 2, 3);
        await Assert.That(items).CompletesWithin(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task CompletesWithin_WithSlowSequence_Fails()
    {
        var items = DelayedItems(10, TimeSpan.FromSeconds(1));
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(items).CompletesWithin(TimeSpan.FromMilliseconds(100)));
    }

    #endregion

    #region Null handling

    [Test]
    public async Task IsEmpty_WithNullSequence_Fails()
    {
        IAsyncEnumerable<int>? nullSequence = null;
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(nullSequence!).IsEmpty());
    }

    [Test]
    public async Task IsNotEmpty_WithNullSequence_Fails()
    {
        IAsyncEnumerable<int>? nullSequence = null;
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(nullSequence!).IsNotEmpty());
    }

    #endregion
}
