using System.Runtime.CompilerServices;
using TUnit.Assertions.Abstractions;
using TUnit.Assertions.Adapters;
using TUnit.Assertions.Collections;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Conditions.Wrappers;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for dictionary assertions that preserves type through And/Or chains.
/// Inherits from CollectionAssertionBase to automatically get all collection methods (HasSingleItem, IsInOrder, etc.)
/// since dictionaries are collections of KeyValuePair items.
/// </summary>
/// <typeparam name="TDictionary">The dictionary type (e.g., Dictionary, IReadOnlyDictionary)</typeparam>
/// <typeparam name="TKey">The dictionary key type</typeparam>
/// <typeparam name="TValue">The dictionary value type</typeparam>
public abstract class DictionaryAssertionBase<TDictionary, TKey, TValue> : CollectionAssertionBase<TDictionary, KeyValuePair<TKey, TValue>>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    protected DictionaryAssertionBase(AssertionContext<TDictionary> context)
        : base(context)
    {
    }

    /// <summary>
    /// Constructor for continuation classes (DictionaryAndContinuation, DictionaryOrContinuation).
    /// Handles linking to previous assertion and appending combiner expression.
    /// Private protected means accessible only to derived classes within the same assembly.
    /// </summary>
    private protected DictionaryAssertionBase(
        AssertionContext<TDictionary> context,
        Assertion<TDictionary> previousAssertion,
        string combinerExpression,
        CombinerType combinerType)
        : base(context, previousAssertion, combinerExpression, combinerType)
    {
    }

    protected override string GetExpectation() => "dictionary assertion";

    /// <summary>
    /// Asserts that the dictionary contains the specified key.
    /// This instance method enables calling ContainsKey with proper type inference.
    /// Example: await Assert.That(dictionary).ContainsKey("key1");
    /// </summary>
    public DictionaryContainsKeyAssertion<TDictionary, TKey, TValue> ContainsKey(
        TKey expectedKey,
        [CallerArgumentExpression(nameof(expectedKey))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".ContainsKey({expression})");
        return new DictionaryContainsKeyAssertion<TDictionary, TKey, TValue>(Context, expectedKey);
    }

    /// <summary>
    /// Asserts that the dictionary contains the specified key using a custom comparer.
    /// This instance method enables calling ContainsKey with proper type inference.
    /// Example: await Assert.That(dictionary).ContainsKey("key1", StringComparer.OrdinalIgnoreCase);
    /// </summary>
    public DictionaryContainsKeyAssertion<TDictionary, TKey, TValue> ContainsKey(
        TKey expectedKey,
        IEqualityComparer<TKey>? comparer,
        [CallerArgumentExpression(nameof(expectedKey))] string? keyExpression = null,
        [CallerArgumentExpression(nameof(comparer))] string? comparerExpression = null)
    {
        Context.ExpressionBuilder.Append($".ContainsKey({keyExpression}, {comparerExpression})");
        return new DictionaryContainsKeyAssertion<TDictionary, TKey, TValue>(Context, expectedKey, comparer);
    }

    /// <summary>
    /// Asserts that the dictionary does not contain the specified key.
    /// This instance method enables calling DoesNotContainKey with proper type inference.
    /// Example: await Assert.That(dictionary).DoesNotContainKey("key1");
    /// </summary>
    public DictionaryDoesNotContainKeyAssertion<TDictionary, TKey, TValue> DoesNotContainKey(
        TKey expectedKey,
        [CallerArgumentExpression(nameof(expectedKey))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".DoesNotContainKey({expression})");
        return new DictionaryDoesNotContainKeyAssertion<TDictionary, TKey, TValue>(Context, expectedKey);
    }

    /// <summary>
    /// Asserts that the dictionary contains the specified value.
    /// Example: await Assert.That(dictionary).ContainsValue("value1");
    /// </summary>
    public DictionaryContainsValueAssertion<TDictionary, TKey, TValue> ContainsValue(
        TValue expectedValue,
        [CallerArgumentExpression(nameof(expectedValue))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".ContainsValue({expression})");
        return new DictionaryContainsValueAssertion<TDictionary, TKey, TValue>(Context, expectedValue);
    }

    /// <summary>
    /// Asserts that the dictionary does not contain the specified value.
    /// Example: await Assert.That(dictionary).DoesNotContainValue("value1");
    /// </summary>
    public DictionaryDoesNotContainValueAssertion<TDictionary, TKey, TValue> DoesNotContainValue(
        TValue expectedValue,
        [CallerArgumentExpression(nameof(expectedValue))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".DoesNotContainValue({expression})");
        return new DictionaryDoesNotContainValueAssertion<TDictionary, TKey, TValue>(Context, expectedValue);
    }

    /// <summary>
    /// Asserts that the dictionary contains the specified key with the specified value.
    /// Example: await Assert.That(dictionary).ContainsKeyWithValue("key1", "value1");
    /// </summary>
    public DictionaryContainsKeyWithValueAssertion<TDictionary, TKey, TValue> ContainsKeyWithValue(
        TKey expectedKey,
        TValue expectedValue,
        [CallerArgumentExpression(nameof(expectedKey))] string? keyExpression = null,
        [CallerArgumentExpression(nameof(expectedValue))] string? valueExpression = null)
    {
        Context.ExpressionBuilder.Append($".ContainsKeyWithValue({keyExpression}, {valueExpression})");
        return new DictionaryContainsKeyWithValueAssertion<TDictionary, TKey, TValue>(Context, expectedKey, expectedValue);
    }

    /// <summary>
    /// Asserts that all keys in the dictionary satisfy the predicate.
    /// Example: await Assert.That(dictionary).AllKeys(k => k.StartsWith("prefix"));
    /// </summary>
    public DictionaryAllKeysAssertion<TDictionary, TKey, TValue> AllKeys(
        Func<TKey, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".AllKeys({expression})");
        return new DictionaryAllKeysAssertion<TDictionary, TKey, TValue>(Context, predicate);
    }

    /// <summary>
    /// Asserts that all values in the dictionary satisfy the predicate.
    /// Example: await Assert.That(dictionary).AllValues(v => v > 0);
    /// </summary>
    public DictionaryAllValuesAssertion<TDictionary, TKey, TValue> AllValues(
        Func<TValue, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".AllValues({expression})");
        return new DictionaryAllValuesAssertion<TDictionary, TKey, TValue>(Context, predicate);
    }

    /// <summary>
    /// Asserts that any key in the dictionary satisfies the predicate.
    /// Example: await Assert.That(dictionary).AnyKey(k => k.Contains("search"));
    /// </summary>
    public DictionaryAnyKeyAssertion<TDictionary, TKey, TValue> AnyKey(
        Func<TKey, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".AnyKey({expression})");
        return new DictionaryAnyKeyAssertion<TDictionary, TKey, TValue>(Context, predicate);
    }

    /// <summary>
    /// Asserts that any value in the dictionary satisfies the predicate.
    /// Example: await Assert.That(dictionary).AnyValue(v => v > 100);
    /// </summary>
    public DictionaryAnyValueAssertion<TDictionary, TKey, TValue> AnyValue(
        Func<TValue, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".AnyValue({expression})");
        return new DictionaryAnyValueAssertion<TDictionary, TKey, TValue>(Context, predicate);
    }

    /// <summary>
    /// Returns an And continuation that preserves dictionary type, key type, and value type.
    /// Overrides the base Assertion.And to return a dictionary-specific continuation.
    /// </summary>
    public new DictionaryAndContinuation<TDictionary, TKey, TValue> And
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.OrAssertion<TDictionary>>();
            return new DictionaryAndContinuation<TDictionary, TKey, TValue>(Context, InternalWrappedExecution ?? this);
        }
    }

    /// <summary>
    /// Returns an Or continuation that preserves dictionary type, key type, and value type.
    /// Overrides the base Assertion.Or to return a dictionary-specific continuation.
    /// </summary>
    public new DictionaryOrContinuation<TDictionary, TKey, TValue> Or
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.AndAssertion<TDictionary>>();
            return new DictionaryOrContinuation<TDictionary, TKey, TValue>(Context, InternalWrappedExecution ?? this);
        }
    }
}
