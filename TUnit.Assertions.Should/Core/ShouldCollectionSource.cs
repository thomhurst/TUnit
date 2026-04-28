using System.ComponentModel;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Should.Core;

/// <summary>
/// Should-flavored entry wrapper for collections. Mirrors
/// <see cref="CollectionAssertionBase{TCollection, TItem}"/>'s instance-method approach so
/// element-typed assertions like <c>BeInOrder</c>/<c>All</c>/<c>HaveSingleItem</c> work
/// without explicit type arguments — the generated extension form
/// <c>Method&lt;TCollection, TItem&gt;(IShouldSource&lt;TCollection&gt;)</c> can't infer <c>TItem</c>
/// from a constraint alone.
/// </summary>
public sealed class ShouldCollectionSource<TItem> : IShouldSource<IEnumerable<TItem>>
{
    private readonly CollectionAssertion<TItem> _inner;

    public AssertionContext<IEnumerable<TItem>> Context { get; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ShouldCollectionSource(IEnumerable<TItem>? value, string? expression)
    {
        _inner = new CollectionAssertion<TItem>(value!, expression);
        Context = ((IAssertionSource<IEnumerable<TItem>>)_inner).Context;
        // CollectionAssertion seeds the ExpressionBuilder with "Assert.That({expression})" — replace
        // it with the Should-flavored "{expression}.Should()" so failure messages match the entry form.
        Context.ExpressionBuilder.Clear();
        Context.ExpressionBuilder.Append(expression ?? "?").Append(".Should()");
    }

    public ShouldAssertion<IEnumerable<TItem>> BeInOrder()
        => Wrap(_inner.IsInOrder());

    public ShouldAssertion<IEnumerable<TItem>> BeInDescendingOrder()
        => Wrap(_inner.IsInDescendingOrder());

    public ShouldAssertion<IEnumerable<TItem>> All(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
        => Wrap(_inner.All(predicate, expression));

    public ShouldAssertion<IEnumerable<TItem>> Any(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
        => Wrap(_inner.Any(predicate, expression));

    public ShouldAssertion<IEnumerable<TItem>> HaveSingleItem()
        => Wrap(_inner.HasSingleItem());

    public ShouldAssertion<IEnumerable<TItem>> HaveSingleItem(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
        => Wrap(_inner.HasSingleItem(predicate, expression));

    public ShouldAssertion<IEnumerable<TItem>> HaveDistinctItems()
        => Wrap(_inner.HasDistinctItems());

    private ShouldAssertion<IEnumerable<TItem>> Wrap(Assertion<IEnumerable<TItem>> inner)
        => new(Context, inner);
}
