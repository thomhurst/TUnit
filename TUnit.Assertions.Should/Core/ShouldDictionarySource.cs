using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;
using TUnit.Assertions.Should.Attributes;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Should.Core;

[ShouldGeneratePartial(typeof(DictionaryAssertion<,>))]
public sealed partial class ShouldDictionarySource<TKey, TValue>
    : ShouldEnumerableSourceBase<IReadOnlyDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>, ShouldDictionarySource<TKey, TValue>>
    where TKey : notnull
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ShouldDictionarySource(IReadOnlyDictionary<TKey, TValue>? value, string? expression)
        : base(new AssertionContext<IReadOnlyDictionary<TKey, TValue>>(value!, ShouldExpressionBuilder.Build(expression)))
    {
    }

    internal ShouldDictionarySource(AssertionContext<IReadOnlyDictionary<TKey, TValue>> context)
        : base(context)
    {
    }

    public ShouldAssertion<IReadOnlyDictionary<TKey, TValue>> ContainKey(
        TKey expectedKey,
        [CallerArgumentExpression(nameof(expectedKey))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".ContainKey(").Append(expression).Append(')');
        var inner = ApplyBecause(new DictionaryContainsKeyAssertion<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>(Context, expectedKey));
        return new ShouldAssertion<IReadOnlyDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IReadOnlyDictionary<TKey, TValue>> ContainKey(
        TKey expectedKey,
        IEqualityComparer<TKey>? comparer,
        [CallerArgumentExpression(nameof(expectedKey))] string? keyExpression = null,
        [CallerArgumentExpression(nameof(comparer))] string? comparerExpression = null)
    {
        Context.ExpressionBuilder.Append($".ContainKey({keyExpression}, {comparerExpression})");
        var inner = ApplyBecause(new DictionaryContainsKeyAssertion<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>(Context, expectedKey, comparer));
        return new ShouldAssertion<IReadOnlyDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IReadOnlyDictionary<TKey, TValue>> NotContainKey(
        TKey expectedKey,
        [CallerArgumentExpression(nameof(expectedKey))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".NotContainKey(").Append(expression).Append(')');
        var inner = ApplyBecause(new DictionaryDoesNotContainKeyAssertion<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>(Context, expectedKey));
        return new ShouldAssertion<IReadOnlyDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IReadOnlyDictionary<TKey, TValue>> ContainValue(
        TValue expectedValue,
        [CallerArgumentExpression(nameof(expectedValue))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".ContainValue(").Append(expression).Append(')');
        var inner = ApplyBecause(new DictionaryContainsValueAssertion<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>(Context, expectedValue));
        return new ShouldAssertion<IReadOnlyDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IReadOnlyDictionary<TKey, TValue>> NotContainValue(
        TValue expectedValue,
        [CallerArgumentExpression(nameof(expectedValue))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".NotContainValue(").Append(expression).Append(')');
        var inner = ApplyBecause(new DictionaryDoesNotContainValueAssertion<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>(Context, expectedValue));
        return new ShouldAssertion<IReadOnlyDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IReadOnlyDictionary<TKey, TValue>> ContainKeyWithValue(
        TKey expectedKey,
        TValue expectedValue,
        [CallerArgumentExpression(nameof(expectedKey))] string? keyExpression = null,
        [CallerArgumentExpression(nameof(expectedValue))] string? valueExpression = null)
    {
        Context.ExpressionBuilder.Append($".ContainKeyWithValue({keyExpression}, {valueExpression})");
        var inner = ApplyBecause(new DictionaryContainsKeyWithValueAssertion<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>(Context, expectedKey, expectedValue));
        return new ShouldAssertion<IReadOnlyDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IReadOnlyDictionary<TKey, TValue>> AllKeys(
        Func<TKey, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".AllKeys(").Append(expression).Append(')');
        var inner = ApplyBecause(new DictionaryAllKeysAssertion<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>(Context, predicate));
        return new ShouldAssertion<IReadOnlyDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IReadOnlyDictionary<TKey, TValue>> AllValues(
        Func<TValue, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".AllValues(").Append(expression).Append(')');
        var inner = ApplyBecause(new DictionaryAllValuesAssertion<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>(Context, predicate));
        return new ShouldAssertion<IReadOnlyDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IReadOnlyDictionary<TKey, TValue>> AnyKey(
        Func<TKey, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".AnyKey(").Append(expression).Append(')');
        var inner = ApplyBecause(new DictionaryAnyKeyAssertion<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>(Context, predicate));
        return new ShouldAssertion<IReadOnlyDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IReadOnlyDictionary<TKey, TValue>> AnyValue(
        Func<TValue, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".AnyValue(").Append(expression).Append(')');
        var inner = ApplyBecause(new DictionaryAnyValueAssertion<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>(Context, predicate));
        return new ShouldAssertion<IReadOnlyDictionary<TKey, TValue>>(Context, inner);
    }

    // The count/size methods below are hand-written because the source DictionaryAssertion shadows the
    // inherited collection methods (IsEmpty, HasSingleItem, ...) with dictionary-typed `public new`
    // overloads whose abstract return type the Should generator can't construct. Without these the
    // generated Be/Have counterparts would silently disappear from the dictionary Should surface.

    public ShouldAssertion<IReadOnlyDictionary<TKey, TValue>> BeEmpty()
    {
        Context.ExpressionBuilder.Append(".BeEmpty()");
        var inner = ApplyBecause(new CollectionIsEmptyAssertion<IReadOnlyDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>(Context));
        return new ShouldAssertion<IReadOnlyDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IReadOnlyDictionary<TKey, TValue>> NotBeEmpty()
    {
        Context.ExpressionBuilder.Append(".NotBeEmpty()");
        var inner = ApplyBecause(new CollectionIsNotEmptyAssertion<IReadOnlyDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>(Context));
        return new ShouldAssertion<IReadOnlyDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IReadOnlyDictionary<TKey, TValue>> HaveSingleItem()
    {
        Context.ExpressionBuilder.Append(".HaveSingleItem()");
        var inner = ApplyBecause(new HasSingleItemAssertion<IReadOnlyDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>(Context));
        return new ShouldAssertion<IReadOnlyDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IReadOnlyDictionary<TKey, TValue>> HaveAtLeast(
        int minCount,
        [CallerArgumentExpression(nameof(minCount))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".HaveAtLeast(").Append(expression).Append(')');
        var inner = ApplyBecause(new CollectionHasAtLeastAssertion<IReadOnlyDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>(Context, minCount));
        return new ShouldAssertion<IReadOnlyDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IReadOnlyDictionary<TKey, TValue>> HaveAtMost(
        int maxCount,
        [CallerArgumentExpression(nameof(maxCount))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".HaveAtMost(").Append(expression).Append(')');
        var inner = ApplyBecause(new CollectionHasAtMostAssertion<IReadOnlyDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>(Context, maxCount));
        return new ShouldAssertion<IReadOnlyDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IReadOnlyDictionary<TKey, TValue>> HaveCountBetween(
        int min,
        int max,
        [CallerArgumentExpression(nameof(min))] string? minExpression = null,
        [CallerArgumentExpression(nameof(max))] string? maxExpression = null)
    {
        Context.ExpressionBuilder.Append($".HaveCountBetween({minExpression}, {maxExpression})");
        var inner = ApplyBecause(new CollectionHasCountBetweenAssertion<IReadOnlyDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>(Context, min, max));
        return new ShouldAssertion<IReadOnlyDictionary<TKey, TValue>>(Context, inner);
    }
}

[ShouldGeneratePartial(typeof(MutableDictionaryAssertion<,>))]
public sealed partial class ShouldMutableDictionarySource<TKey, TValue>
    : ShouldEnumerableSourceBase<IDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>, ShouldMutableDictionarySource<TKey, TValue>>
    where TKey : notnull
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ShouldMutableDictionarySource(IDictionary<TKey, TValue>? value, string? expression)
        : base(new AssertionContext<IDictionary<TKey, TValue>>(value!, ShouldExpressionBuilder.Build(expression)))
    {
    }

    internal ShouldMutableDictionarySource(AssertionContext<IDictionary<TKey, TValue>> context)
        : base(context)
    {
    }

    public ShouldAssertion<IDictionary<TKey, TValue>> ContainKey(
        TKey expectedKey,
        [CallerArgumentExpression(nameof(expectedKey))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".ContainKey(").Append(expression).Append(')');
        var inner = ApplyBecause(new MutableDictionaryContainsKeyAssertion<IDictionary<TKey, TValue>, TKey, TValue>(Context, expectedKey));
        return new ShouldAssertion<IDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IDictionary<TKey, TValue>> ContainKey(
        TKey expectedKey,
        IEqualityComparer<TKey>? comparer,
        [CallerArgumentExpression(nameof(expectedKey))] string? keyExpression = null,
        [CallerArgumentExpression(nameof(comparer))] string? comparerExpression = null)
    {
        Context.ExpressionBuilder.Append($".ContainKey({keyExpression}, {comparerExpression})");
        var inner = ApplyBecause(new MutableDictionaryContainsKeyAssertion<IDictionary<TKey, TValue>, TKey, TValue>(Context, expectedKey, comparer));
        return new ShouldAssertion<IDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IDictionary<TKey, TValue>> NotContainKey(
        TKey expectedKey,
        [CallerArgumentExpression(nameof(expectedKey))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".NotContainKey(").Append(expression).Append(')');
        var inner = ApplyBecause(new MutableDictionaryDoesNotContainKeyAssertion<IDictionary<TKey, TValue>, TKey, TValue>(Context, expectedKey));
        return new ShouldAssertion<IDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IDictionary<TKey, TValue>> ContainValue(
        TValue expectedValue,
        [CallerArgumentExpression(nameof(expectedValue))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".ContainValue(").Append(expression).Append(')');
        var inner = ApplyBecause(new MutableDictionaryContainsValueAssertion<IDictionary<TKey, TValue>, TKey, TValue>(Context, expectedValue));
        return new ShouldAssertion<IDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IDictionary<TKey, TValue>> NotContainValue(
        TValue expectedValue,
        [CallerArgumentExpression(nameof(expectedValue))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".NotContainValue(").Append(expression).Append(')');
        var inner = ApplyBecause(new MutableDictionaryDoesNotContainValueAssertion<IDictionary<TKey, TValue>, TKey, TValue>(Context, expectedValue));
        return new ShouldAssertion<IDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IDictionary<TKey, TValue>> ContainKeyWithValue(
        TKey expectedKey,
        TValue expectedValue,
        [CallerArgumentExpression(nameof(expectedKey))] string? keyExpression = null,
        [CallerArgumentExpression(nameof(expectedValue))] string? valueExpression = null)
    {
        Context.ExpressionBuilder.Append($".ContainKeyWithValue({keyExpression}, {valueExpression})");
        var inner = ApplyBecause(new MutableDictionaryContainsKeyWithValueAssertion<IDictionary<TKey, TValue>, TKey, TValue>(Context, expectedKey, expectedValue));
        return new ShouldAssertion<IDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IDictionary<TKey, TValue>> AllKeys(
        Func<TKey, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".AllKeys(").Append(expression).Append(')');
        var inner = ApplyBecause(new MutableDictionaryAllKeysAssertion<IDictionary<TKey, TValue>, TKey, TValue>(Context, predicate));
        return new ShouldAssertion<IDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IDictionary<TKey, TValue>> AllValues(
        Func<TValue, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".AllValues(").Append(expression).Append(')');
        var inner = ApplyBecause(new MutableDictionaryAllValuesAssertion<IDictionary<TKey, TValue>, TKey, TValue>(Context, predicate));
        return new ShouldAssertion<IDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IDictionary<TKey, TValue>> AnyKey(
        Func<TKey, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".AnyKey(").Append(expression).Append(')');
        var inner = ApplyBecause(new MutableDictionaryAnyKeyAssertion<IDictionary<TKey, TValue>, TKey, TValue>(Context, predicate));
        return new ShouldAssertion<IDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IDictionary<TKey, TValue>> AnyValue(
        Func<TValue, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".AnyValue(").Append(expression).Append(')');
        var inner = ApplyBecause(new MutableDictionaryAnyValueAssertion<IDictionary<TKey, TValue>, TKey, TValue>(Context, predicate));
        return new ShouldAssertion<IDictionary<TKey, TValue>>(Context, inner);
    }

    // See ShouldDictionarySource: hand-written because MutableDictionaryAssertion shadows the inherited
    // collection methods with dictionary-typed `public new` overloads the Should generator can't construct.

    public ShouldAssertion<IDictionary<TKey, TValue>> BeEmpty()
    {
        Context.ExpressionBuilder.Append(".BeEmpty()");
        var inner = ApplyBecause(new CollectionIsEmptyAssertion<IDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>(Context));
        return new ShouldAssertion<IDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IDictionary<TKey, TValue>> NotBeEmpty()
    {
        Context.ExpressionBuilder.Append(".NotBeEmpty()");
        var inner = ApplyBecause(new CollectionIsNotEmptyAssertion<IDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>(Context));
        return new ShouldAssertion<IDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IDictionary<TKey, TValue>> HaveSingleItem()
    {
        Context.ExpressionBuilder.Append(".HaveSingleItem()");
        var inner = ApplyBecause(new HasSingleItemAssertion<IDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>(Context));
        return new ShouldAssertion<IDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IDictionary<TKey, TValue>> HaveAtLeast(
        int minCount,
        [CallerArgumentExpression(nameof(minCount))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".HaveAtLeast(").Append(expression).Append(')');
        var inner = ApplyBecause(new CollectionHasAtLeastAssertion<IDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>(Context, minCount));
        return new ShouldAssertion<IDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IDictionary<TKey, TValue>> HaveAtMost(
        int maxCount,
        [CallerArgumentExpression(nameof(maxCount))] string? expression = null)
    {
        Context.ExpressionBuilder.Append(".HaveAtMost(").Append(expression).Append(')');
        var inner = ApplyBecause(new CollectionHasAtMostAssertion<IDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>(Context, maxCount));
        return new ShouldAssertion<IDictionary<TKey, TValue>>(Context, inner);
    }

    public ShouldAssertion<IDictionary<TKey, TValue>> HaveCountBetween(
        int min,
        int max,
        [CallerArgumentExpression(nameof(min))] string? minExpression = null,
        [CallerArgumentExpression(nameof(max))] string? maxExpression = null)
    {
        Context.ExpressionBuilder.Append($".HaveCountBetween({minExpression}, {maxExpression})");
        var inner = ApplyBecause(new CollectionHasCountBetweenAssertion<IDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>(Context, min, max));
        return new ShouldAssertion<IDictionary<TKey, TValue>>(Context, inner);
    }
}
