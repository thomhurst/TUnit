using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for IList&lt;T&gt; assertions that provides index-based operations
/// in addition to standard collection assertions.
/// </summary>
/// <typeparam name="TList">The concrete list type</typeparam>
/// <typeparam name="TItem">The type of items in the list</typeparam>
public abstract class ListAssertionBase<TList, TItem> : CollectionAssertionBase<TList, TItem>
    where TList : IList<TItem>
{
    protected ListAssertionBase(AssertionContext<TList> context)
        : base(context)
    {
    }

    /// <summary>
    /// Constructor for continuation classes (ListAndContinuation, ListOrContinuation).
    /// Handles linking to previous assertion and appending combiner expression.
    /// </summary>
    private protected ListAssertionBase(
        AssertionContext<TList> context,
        Assertion<TList> previousAssertion,
        string combinerExpression,
        CombinerType combinerType)
        : base(context, previousAssertion, combinerExpression, combinerType)
    {
    }

    /// <summary>
    /// Asserts that the item at the specified index equals the expected value.
    /// Example: await Assert.That(list).HasItemAt(0, "expected");
    /// </summary>
    public ListHasItemAtAssertion<TList, TItem> HasItemAt(
        int index,
        TItem expected,
        [CallerArgumentExpression(nameof(index))] string? indexExpression = null,
        [CallerArgumentExpression(nameof(expected))] string? expectedExpression = null)
    {
        Context.ExpressionBuilder.Append($".HasItemAt({indexExpression}, {expectedExpression})");
        return new ListHasItemAtAssertion<TList, TItem>(Context, index, expected);
    }

    /// <summary>
    /// Gets the item at the specified index for further assertions.
    /// Example: await Assert.That(list).ItemAt(0).IsEqualTo("expected");
    /// </summary>
    public ListItemAtSource<TList, TItem> ItemAt(
        int index,
        [CallerArgumentExpression(nameof(index))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".ItemAt({expression})");
        return new ListItemAtSource<TList, TItem>(Context, index);
    }

    /// <summary>
    /// Gets the first item in the list for further assertions.
    /// Example: await Assert.That(list).FirstItem().IsEqualTo("expected");
    /// </summary>
    public ListItemAtSource<TList, TItem> FirstItem()
    {
        Context.ExpressionBuilder.Append(".FirstItem()");
        return new ListItemAtSource<TList, TItem>(Context, 0);
    }

    /// <summary>
    /// Gets the last item in the list for further assertions.
    /// Example: await Assert.That(list).LastItem().IsEqualTo("expected");
    /// </summary>
    public ListLastItemSource<TList, TItem> LastItem()
    {
        Context.ExpressionBuilder.Append(".LastItem()");
        return new ListLastItemSource<TList, TItem>(Context);
    }

    /// <summary>
    /// Returns an And continuation that preserves list type and item type.
    /// Overrides the base CollectionAssertionBase.And to return a list-specific continuation.
    /// </summary>
    public new ListAndContinuation<TList, TItem> And
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.OrAssertion<TList>>();
            return new ListAndContinuation<TList, TItem>(Context, InternalWrappedExecution ?? this);
        }
    }

    /// <summary>
    /// Returns an Or continuation that preserves list type and item type.
    /// Overrides the base CollectionAssertionBase.Or to return a list-specific continuation.
    /// </summary>
    public new ListOrContinuation<TList, TItem> Or
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.AndAssertion<TList>>();
            return new ListOrContinuation<TList, TItem>(Context, InternalWrappedExecution ?? this);
        }
    }
}
