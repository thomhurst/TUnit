using System.Runtime.CompilerServices;
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
