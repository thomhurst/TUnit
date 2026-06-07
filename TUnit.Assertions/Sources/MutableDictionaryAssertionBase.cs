using System.Runtime.CompilerServices;
using TUnit.Assertions.Adapters;
using TUnit.Assertions.Collections;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for mutable dictionary (IDictionary) assertions that preserves type through And/Or chains.
/// Inherits from CollectionAssertionBase to automatically get all collection methods (HasSingleItem, IsInOrder, etc.)
/// since dictionaries are collections of KeyValuePair items.
/// </summary>
/// <typeparam name="TDictionary">The dictionary type (e.g., IDictionary)</typeparam>
/// <typeparam name="TKey">The dictionary key type</typeparam>
/// <typeparam name="TValue">The dictionary value type</typeparam>
public abstract class MutableDictionaryAssertionBase<TDictionary, TKey, TValue> : CollectionAssertionBase<TDictionary, KeyValuePair<TKey, TValue>>
    where TDictionary : IDictionary<TKey, TValue>
    where TKey : notnull
{
    protected MutableDictionaryAssertionBase(AssertionContext<TDictionary> context)
        : base(context)
    {
    }

    /// <summary>
    /// Constructor for continuation classes.
    /// Handles linking to previous assertion and appending combiner expression.
    /// </summary>
    private protected MutableDictionaryAssertionBase(
        AssertionContext<TDictionary> context,
        Assertion<TDictionary> previousAssertion,
        string combinerExpression,
        CombinerType combinerType)
        : base(context, previousAssertion, combinerExpression, combinerType)
    {
    }

    protected override string GetExpectation() => "dictionary assertion";

    /// <summary>
    /// Asserts that the dictionary is null while preserving mutable-dictionary-specific chaining.
    /// </summary>
    public new MutableDictionaryAssertionBase<TDictionary, TKey, TValue> IsNull()
    {
        Context.ExpressionBuilder.Append(".IsNull()");
        return new MutableDictionaryNullAssertion<TDictionary, TKey, TValue>(Context, expectNull: true);
    }

    /// <summary>
    /// Asserts that the dictionary is not null while preserving mutable-dictionary-specific chaining.
    /// </summary>
    public new MutableDictionaryAssertionBase<TDictionary, TKey, TValue> IsNotNull()
    {
        Context.ExpressionBuilder.Append(".IsNotNull()");
        return new MutableDictionaryNullAssertion<TDictionary, TKey, TValue>(Context, expectNull: false);
    }

    // See DictionaryAssertionBase for the rationale: these `public new` collection methods return a
    // mutable-dictionary-typed assertion (preserving .And.ContainsKey, etc.) and reuse the existing
    // collection check logic via a delegating wrapper built on a detached context.

    private MutableDictionaryAssertionBase<TDictionary, TKey, TValue> Delegate(Assertion<TDictionary> inner)
        => new MutableDictionaryDelegatingAssertion<TDictionary, TKey, TValue>(Context, inner);

    private AssertionContext<TDictionary> DetachedContext()
        => new(Context.Evaluation, new System.Text.StringBuilder());

    /// <summary>
    /// Asserts that the dictionary is empty while preserving mutable-dictionary-specific chaining.
    /// </summary>
    public new MutableDictionaryAssertionBase<TDictionary, TKey, TValue> IsEmpty()
    {
        Context.ExpressionBuilder.Append(".IsEmpty()");
        return Delegate(new CollectionIsEmptyAssertion<TDictionary, KeyValuePair<TKey, TValue>>(DetachedContext()));
    }

    /// <summary>
    /// Asserts that the dictionary is not empty while preserving mutable-dictionary-specific chaining.
    /// </summary>
    public new MutableDictionaryAssertionBase<TDictionary, TKey, TValue> IsNotEmpty()
    {
        Context.ExpressionBuilder.Append(".IsNotEmpty()");
        return Delegate(new CollectionIsNotEmptyAssertion<TDictionary, KeyValuePair<TKey, TValue>>(DetachedContext()));
    }

    /// <summary>
    /// Gets the entry count for numeric assertions while preserving mutable-dictionary-specific chaining.
    /// Example: await Assert.That(dict).Count().IsEqualTo(2).And.ContainsKey("key");
    /// </summary>
    public new MutableDictionaryCountSource<TDictionary, TKey, TValue> Count()
    {
        Context.ExpressionBuilder.Append(".Count()");
        return new MutableDictionaryCountSource<TDictionary, TKey, TValue>(Context);
    }

    /// <summary>
    /// Asserts that the dictionary contains exactly one entry while preserving mutable-dictionary chaining.
    /// </summary>
    public new MutableDictionaryAssertionBase<TDictionary, TKey, TValue> HasSingleItem()
    {
        Context.ExpressionBuilder.Append(".HasSingleItem()");
        return Delegate(new HasSingleItemAssertion<TDictionary, KeyValuePair<TKey, TValue>>(DetachedContext()));
    }

    /// <summary>
    /// Asserts that the dictionary contains exactly one entry matching the predicate while preserving chaining.
    /// </summary>
    public new MutableDictionaryAssertionBase<TDictionary, TKey, TValue> HasSingleItem(
        Func<KeyValuePair<TKey, TValue>, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".HasSingleItem({expression})");
        return Delegate(new HasSingleItemPredicateAssertion<TDictionary, KeyValuePair<TKey, TValue>>(
            DetachedContext(), predicate, expression ?? "predicate"));
    }

    /// <summary>
    /// Asserts that the dictionary has at least the specified number of entries while preserving chaining.
    /// </summary>
    public new MutableDictionaryAssertionBase<TDictionary, TKey, TValue> HasAtLeast(
        int minCount,
        [CallerArgumentExpression(nameof(minCount))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".HasAtLeast({expression})");
        return Delegate(new CollectionHasAtLeastAssertion<TDictionary, KeyValuePair<TKey, TValue>>(DetachedContext(), minCount));
    }

    /// <summary>
    /// Asserts that the dictionary has at most the specified number of entries while preserving chaining.
    /// </summary>
    public new MutableDictionaryAssertionBase<TDictionary, TKey, TValue> HasAtMost(
        int maxCount,
        [CallerArgumentExpression(nameof(maxCount))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".HasAtMost({expression})");
        return Delegate(new CollectionHasAtMostAssertion<TDictionary, KeyValuePair<TKey, TValue>>(DetachedContext(), maxCount));
    }

    /// <summary>
    /// Asserts that the dictionary entry count is between min and max (inclusive) while preserving chaining.
    /// </summary>
    public new MutableDictionaryAssertionBase<TDictionary, TKey, TValue> HasCountBetween(
        int min,
        int max,
        [CallerArgumentExpression(nameof(min))] string? minExpression = null,
        [CallerArgumentExpression(nameof(max))] string? maxExpression = null)
    {
        Context.ExpressionBuilder.Append($".HasCountBetween({minExpression}, {maxExpression})");
        return Delegate(new CollectionHasCountBetweenAssertion<TDictionary, KeyValuePair<TKey, TValue>>(DetachedContext(), min, max));
    }

    /// <summary>
    /// Asserts that the dictionary contains the specified key.
    /// Example: await Assert.That(dictionary).ContainsKey("key1");
    /// </summary>
    public MutableDictionaryContainsKeyAssertion<TDictionary, TKey, TValue> ContainsKey(
        TKey expectedKey,
        [CallerArgumentExpression(nameof(expectedKey))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".ContainsKey({expression})");
        return new MutableDictionaryContainsKeyAssertion<TDictionary, TKey, TValue>(Context, expectedKey);
    }

    /// <summary>
    /// Asserts that the dictionary contains the specified key using a custom comparer.
    /// Example: await Assert.That(dictionary).ContainsKey("key1", StringComparer.OrdinalIgnoreCase);
    /// </summary>
    public MutableDictionaryContainsKeyAssertion<TDictionary, TKey, TValue> ContainsKey(
        TKey expectedKey,
        IEqualityComparer<TKey>? comparer,
        [CallerArgumentExpression(nameof(expectedKey))] string? keyExpression = null,
        [CallerArgumentExpression(nameof(comparer))] string? comparerExpression = null)
    {
        Context.ExpressionBuilder.Append($".ContainsKey({keyExpression}, {comparerExpression})");
        return new MutableDictionaryContainsKeyAssertion<TDictionary, TKey, TValue>(Context, expectedKey, comparer);
    }

    /// <summary>
    /// Asserts that the dictionary does not contain the specified key.
    /// Example: await Assert.That(dictionary).DoesNotContainKey("key1");
    /// </summary>
    public MutableDictionaryDoesNotContainKeyAssertion<TDictionary, TKey, TValue> DoesNotContainKey(
        TKey expectedKey,
        [CallerArgumentExpression(nameof(expectedKey))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".DoesNotContainKey({expression})");
        return new MutableDictionaryDoesNotContainKeyAssertion<TDictionary, TKey, TValue>(Context, expectedKey);
    }

    /// <summary>
    /// Asserts that the dictionary contains the specified value.
    /// Example: await Assert.That(dictionary).ContainsValue("value1");
    /// </summary>
    public MutableDictionaryContainsValueAssertion<TDictionary, TKey, TValue> ContainsValue(
        TValue expectedValue,
        [CallerArgumentExpression(nameof(expectedValue))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".ContainsValue({expression})");
        return new MutableDictionaryContainsValueAssertion<TDictionary, TKey, TValue>(Context, expectedValue);
    }

    /// <summary>
    /// Asserts that the dictionary does not contain the specified value.
    /// Example: await Assert.That(dictionary).DoesNotContainValue("value1");
    /// </summary>
    public MutableDictionaryDoesNotContainValueAssertion<TDictionary, TKey, TValue> DoesNotContainValue(
        TValue expectedValue,
        [CallerArgumentExpression(nameof(expectedValue))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".DoesNotContainValue({expression})");
        return new MutableDictionaryDoesNotContainValueAssertion<TDictionary, TKey, TValue>(Context, expectedValue);
    }

    /// <summary>
    /// Asserts that the dictionary contains the specified key with the specified value.
    /// Example: await Assert.That(dictionary).ContainsKeyWithValue("key1", "value1");
    /// </summary>
    public MutableDictionaryContainsKeyWithValueAssertion<TDictionary, TKey, TValue> ContainsKeyWithValue(
        TKey expectedKey,
        TValue expectedValue,
        [CallerArgumentExpression(nameof(expectedKey))] string? keyExpression = null,
        [CallerArgumentExpression(nameof(expectedValue))] string? valueExpression = null)
    {
        Context.ExpressionBuilder.Append($".ContainsKeyWithValue({keyExpression}, {valueExpression})");
        return new MutableDictionaryContainsKeyWithValueAssertion<TDictionary, TKey, TValue>(Context, expectedKey, expectedValue);
    }

    /// <summary>
    /// Asserts that all keys in the dictionary satisfy the predicate.
    /// Example: await Assert.That(dictionary).AllKeys(k => k.StartsWith("prefix"));
    /// </summary>
    public MutableDictionaryAllKeysAssertion<TDictionary, TKey, TValue> AllKeys(
        Func<TKey, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".AllKeys({expression})");
        return new MutableDictionaryAllKeysAssertion<TDictionary, TKey, TValue>(Context, predicate);
    }

    /// <summary>
    /// Asserts that all values in the dictionary satisfy the predicate.
    /// Example: await Assert.That(dictionary).AllValues(v => v > 0);
    /// </summary>
    public MutableDictionaryAllValuesAssertion<TDictionary, TKey, TValue> AllValues(
        Func<TValue, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".AllValues({expression})");
        return new MutableDictionaryAllValuesAssertion<TDictionary, TKey, TValue>(Context, predicate);
    }

    /// <summary>
    /// Asserts that any key in the dictionary satisfies the predicate.
    /// Example: await Assert.That(dictionary).AnyKey(k => k.Contains("search"));
    /// </summary>
    public MutableDictionaryAnyKeyAssertion<TDictionary, TKey, TValue> AnyKey(
        Func<TKey, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".AnyKey({expression})");
        return new MutableDictionaryAnyKeyAssertion<TDictionary, TKey, TValue>(Context, predicate);
    }

    /// <summary>
    /// Asserts that any value in the dictionary satisfies the predicate.
    /// Example: await Assert.That(dictionary).AnyValue(v => v > 100);
    /// </summary>
    public MutableDictionaryAnyValueAssertion<TDictionary, TKey, TValue> AnyValue(
        Func<TValue, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".AnyValue({expression})");
        return new MutableDictionaryAnyValueAssertion<TDictionary, TKey, TValue>(Context, predicate);
    }

    /// <summary>
    /// Returns an And continuation that preserves dictionary type, key type, and value type.
    /// </summary>
    public new MutableDictionaryAndContinuation<TDictionary, TKey, TValue> And
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.OrAssertion<TDictionary>>();
            return new MutableDictionaryAndContinuation<TDictionary, TKey, TValue>(Context, InternalWrappedExecution ?? this);
        }
    }

    /// <summary>
    /// Returns an Or continuation that preserves dictionary type, key type, and value type.
    /// </summary>
    public new MutableDictionaryOrContinuation<TDictionary, TKey, TValue> Or
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.AndAssertion<TDictionary>>();
            return new MutableDictionaryOrContinuation<TDictionary, TKey, TValue>(Context, InternalWrappedExecution ?? this);
        }
    }
}
