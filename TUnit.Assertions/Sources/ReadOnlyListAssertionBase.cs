using System.Runtime.CompilerServices;
using TUnit.Assertions.Chaining;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for IReadOnlyList&lt;T&gt; assertions that provides index-based operations.
/// </summary>
/// <typeparam name="TList">The read-only list type</typeparam>
/// <typeparam name="TItem">The type of items in the list</typeparam>
public abstract class ReadOnlyListAssertionBase<TList, TItem> : CollectionAssertionBase<TList, TItem>
    where TList : IReadOnlyList<TItem>
{
    protected ReadOnlyListAssertionBase(AssertionContext<TList> context)
        : base(context)
    {
    }

    /// <summary>
    /// Constructor for continuation classes (ReadOnlyListAndContinuation, ReadOnlyListOrContinuation).
    /// Handles linking to previous assertion and appending combiner expression.
    /// </summary>
    private protected ReadOnlyListAssertionBase(
        AssertionContext<TList> context,
        Assertion<TList> previousAssertion,
        string combinerExpression,
        CombinerType combinerType)
        : base(context)
    {
        context.ExpressionBuilder.Append(combinerExpression);
        context.SetPendingLink(previousAssertion, combinerType);
    }

    /// <summary>
    /// Asserts that the list has an item at the specified index that equals the expected value.
    /// Example: await Assert.That(list).HasItemAt(0, "first");
    /// </summary>
    public ReadOnlyListHasItemAtAssertion<TList, TItem> HasItemAt(
        int index,
        TItem expected,
        IEqualityComparer<TItem>? comparer = null,
        [CallerArgumentExpression(nameof(index))] string? indexExpression = null,
        [CallerArgumentExpression(nameof(expected))] string? expectedExpression = null)
    {
        Context.ExpressionBuilder.Append($".HasItemAt({indexExpression}, {expectedExpression})");
        return new ReadOnlyListHasItemAtAssertion<TList, TItem>(Context, index, expected, comparer);
    }

    /// <summary>
    /// Returns an assertion source for the item at the specified index.
    /// Example: await Assert.That(list).ItemAt(0).IsEqualTo("first");
    /// </summary>
    public ReadOnlyListItemAtSource<TList, TItem> ItemAt(
        int index,
        [CallerArgumentExpression(nameof(index))] string? indexExpression = null)
    {
        Context.ExpressionBuilder.Append($".ItemAt({indexExpression})");
        return new ReadOnlyListItemAtSource<TList, TItem>(Context, index);
    }

    /// <summary>
    /// Returns an assertion source for the first item in the list.
    /// Example: await Assert.That(list).FirstItem().IsEqualTo("first");
    /// </summary>
    public ReadOnlyListItemAtSource<TList, TItem> FirstItem()
    {
        Context.ExpressionBuilder.Append(".FirstItem()");
        return new ReadOnlyListItemAtSource<TList, TItem>(Context, 0);
    }

    /// <summary>
    /// Returns an assertion source for the last item in the list.
    /// Example: await Assert.That(list).LastItem().IsEqualTo("last");
    /// </summary>
    public ReadOnlyListLastItemSource<TList, TItem> LastItem()
    {
        Context.ExpressionBuilder.Append(".LastItem()");
        return new ReadOnlyListLastItemSource<TList, TItem>(Context);
    }

    /// <summary>
    /// Returns an And continuation that preserves the read-only list type.
    /// </summary>
    public new ReadOnlyListAndContinuation<TList, TItem> And
    {
        get
        {
            ThrowIfMixingCombiner<OrAssertion<TList>>();
            return new ReadOnlyListAndContinuation<TList, TItem>(Context, InternalWrappedExecution ?? this);
        }
    }

    /// <summary>
    /// Returns an Or continuation that preserves the read-only list type.
    /// </summary>
    public new ReadOnlyListOrContinuation<TList, TItem> Or
    {
        get
        {
            ThrowIfMixingCombiner<AndAssertion<TList>>();
            return new ReadOnlyListOrContinuation<TList, TItem>(Context, InternalWrappedExecution ?? this);
        }
    }
}
