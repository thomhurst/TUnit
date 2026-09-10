using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.Abstractions;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Should.Core;

internal static class ShouldExpressionBuilder
{
    internal static StringBuilder Build(string? expression)
    {
        var sb = new StringBuilder((expression?.Length ?? 1) + 16);
        sb.Append(expression ?? "?").Append(".Should()");
        return sb;
    }
}

public abstract class ShouldSourceBase<TValue, TSelf> : IShouldSource<TValue>
    where TSelf : ShouldSourceBase<TValue, TSelf>
{
    private string? _becauseMessage;

    protected ShouldSourceBase(AssertionContext<TValue> context)
    {
        Context = context;
    }

    public AssertionContext<TValue> Context { get; }

    public TSelf Because(string message)
    {
        _becauseMessage = message.Trim();
        return (TSelf)this;
    }

    string? IShouldSource<TValue>.ConsumeBecauseMessage()
        => ConsumeBecauseMessage();

    protected string? ConsumeBecauseMessage()
    {
        var message = _becauseMessage;
        _becauseMessage = null;
        return message;
    }

    protected void ResetShouldExpression(string? expression)
    {
        Context.ExpressionBuilder.Clear();
        Context.ExpressionBuilder.Append(expression ?? "?").Append(".Should()");
    }
}

public abstract class ShouldEnumerableSourceBase<TCollection, TItem, TSelf> : ShouldSourceBase<TCollection, TSelf>
    where TCollection : IEnumerable<TItem>
    where TSelf : ShouldEnumerableSourceBase<TCollection, TItem, TSelf>
{
    protected ShouldEnumerableSourceBase(AssertionContext<TCollection> context)
        : base(context)
    {
    }

    protected TAssertion ApplyBecause<TAssertion>(TAssertion assertion)
        where TAssertion : Assertion<TCollection>
    {
        var because = ConsumeBecauseMessage();
        if (because is not null)
        {
            assertion.Because(because);
        }

        return assertion;
    }

    public ShouldAssertion<TCollection> All(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".All(").Append(expression).Append(')');
        var inner = ApplyBecause(new CollectionAllAssertion<TCollection, TItem>(Context, predicate, expression ?? "predicate"));
        return new ShouldAssertion<TCollection>(Context, inner);
    }

    public ShouldAssertion<TCollection> Any(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".Any(").Append(expression).Append(')');
        var inner = ApplyBecause(new CollectionAnyAssertion<TCollection, TItem>(Context, predicate, expression ?? "predicate"));
        return new ShouldAssertion<TCollection>(Context, inner);
    }

    public ShouldAssertion<TCollection> HaveSingleItem(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".HaveSingleItem(").Append(expression).Append(')');
        var inner = ApplyBecause(new HasSingleItemPredicateAssertion<TCollection, TItem>(Context, predicate, expression ?? "predicate"));
        return new ShouldAssertion<TCollection>(Context, inner);
    }
}

public abstract class ShouldSetSourceBase<TSet, TItem, TSelf> : ShouldEnumerableSourceBase<TSet, TItem, TSelf>
    where TSet : IEnumerable<TItem>
    where TSelf : ShouldSetSourceBase<TSet, TItem, TSelf>
{
    protected ShouldSetSourceBase(AssertionContext<TSet> context)
        : base(context)
    {
    }

    protected abstract ISetAdapter<TItem> CreateSetAdapter(TSet value);

    public ShouldAssertion<TSet> BeSubsetOf(
        IEnumerable<TItem> other,
        [CallerArgumentExpression(nameof(other))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".BeSubsetOf(").Append(expression).Append(')');
        var inner = ApplyBecause(new SetIsSubsetOfAssertion<TSet, TItem>(Context, CreateSetAdapter, other));
        return new ShouldAssertion<TSet>(Context, inner);
    }

    public ShouldAssertion<TSet> BeSupersetOf(
        IEnumerable<TItem> other,
        [CallerArgumentExpression(nameof(other))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".BeSupersetOf(").Append(expression).Append(')');
        var inner = ApplyBecause(new SetIsSupersetOfAssertion<TSet, TItem>(Context, CreateSetAdapter, other));
        return new ShouldAssertion<TSet>(Context, inner);
    }

    public ShouldAssertion<TSet> BeProperSubsetOf(
        IEnumerable<TItem> other,
        [CallerArgumentExpression(nameof(other))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".BeProperSubsetOf(").Append(expression).Append(')');
        var inner = ApplyBecause(new SetIsProperSubsetOfAssertion<TSet, TItem>(Context, CreateSetAdapter, other));
        return new ShouldAssertion<TSet>(Context, inner);
    }

    public ShouldAssertion<TSet> BeProperSupersetOf(
        IEnumerable<TItem> other,
        [CallerArgumentExpression(nameof(other))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".BeProperSupersetOf(").Append(expression).Append(')');
        var inner = ApplyBecause(new SetIsProperSupersetOfAssertion<TSet, TItem>(Context, CreateSetAdapter, other));
        return new ShouldAssertion<TSet>(Context, inner);
    }

    public ShouldAssertion<TSet> Overlap(
        IEnumerable<TItem> other,
        [CallerArgumentExpression(nameof(other))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".Overlap(").Append(expression).Append(')');
        var inner = ApplyBecause(new SetOverlapsAssertion<TSet, TItem>(Context, CreateSetAdapter, other));
        return new ShouldAssertion<TSet>(Context, inner);
    }

    public ShouldAssertion<TSet> NotOverlap(
        IEnumerable<TItem> other,
        [CallerArgumentExpression(nameof(other))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".NotOverlap(").Append(expression).Append(')');
        var inner = ApplyBecause(new SetDoesNotOverlapAssertion<TSet, TItem>(Context, CreateSetAdapter, other));
        return new ShouldAssertion<TSet>(Context, inner);
    }

    public ShouldAssertion<TSet> SetEquals(
        IEnumerable<TItem> other,
        [CallerArgumentExpression(nameof(other))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".SetEquals(").Append(expression).Append(')');
        var inner = ApplyBecause(new SetEqualsAssertion<TSet, TItem>(Context, CreateSetAdapter, other));
        return new ShouldAssertion<TSet>(Context, inner);
    }
}
