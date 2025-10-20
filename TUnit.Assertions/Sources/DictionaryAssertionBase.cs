using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Conditions.Wrappers;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for dictionary assertions that preserves type through And/Or chains.
/// Implements IAssertionSource&lt;TDictionary&gt; to enable all collection and dictionary extension methods.
/// Since dictionaries are collections of KeyValuePair items, collection assertions also work on dictionaries.
/// </summary>
/// <typeparam name="TDictionary">The dictionary type (e.g., Dictionary, IReadOnlyDictionary)</typeparam>
/// <typeparam name="TKey">The dictionary key type</typeparam>
/// <typeparam name="TValue">The dictionary value type</typeparam>
public abstract class DictionaryAssertionBase<TDictionary, TKey, TValue> : Assertion<TDictionary>, IAssertionSource<TDictionary>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
{
    /// <summary>
    /// Explicit implementation of IAssertionSource.Context to expose the context publicly.
    /// </summary>
    AssertionContext<TDictionary> IAssertionSource<TDictionary>.Context => Context;

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
        : base(context)
    {
        context.ExpressionBuilder.Append(combinerExpression);
        context.SetPendingLink(previousAssertion, combinerType);
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
    /// Asserts that the dictionary is empty (has no key-value pairs).
    /// This instance method enables calling IsEmpty with proper type inference.
    /// Example: await Assert.That(dictionary).IsEmpty();
    /// </summary>
    public CollectionIsEmptyAssertion<KeyValuePair<TKey, TValue>> IsEmpty()
    {
        Context.ExpressionBuilder.Append(".IsEmpty()");
        return new CollectionIsEmptyAssertion<KeyValuePair<TKey, TValue>>(Context.Map<IEnumerable<KeyValuePair<TKey, TValue>>>(dict => dict));
    }

    /// <summary>
    /// Asserts that the dictionary is not empty (has at least one key-value pair).
    /// This instance method enables calling IsNotEmpty with proper type inference.
    /// Example: await Assert.That(dictionary).IsNotEmpty();
    /// </summary>
    public CollectionIsNotEmptyAssertion<KeyValuePair<TKey, TValue>> IsNotEmpty()
    {
        Context.ExpressionBuilder.Append(".IsNotEmpty()");
        return new CollectionIsNotEmptyAssertion<KeyValuePair<TKey, TValue>>(Context.Map<IEnumerable<KeyValuePair<TKey, TValue>>>(dict => dict));
    }

    /// <summary>
    /// Asserts that the dictionary contains the specified key-value pair.
    /// This instance method enables calling Contains with proper type inference.
    /// Example: await Assert.That(dictionary).Contains(new KeyValuePair&lt;string, int&gt;("key", 1));
    /// </summary>
    public CollectionContainsAssertion<KeyValuePair<TKey, TValue>> Contains(
        KeyValuePair<TKey, TValue> expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".Contains({expression})");
        return new CollectionContainsAssertion<KeyValuePair<TKey, TValue>>(Context.Map<IEnumerable<KeyValuePair<TKey, TValue>>>(dict => dict), expected);
    }

    /// <summary>
    /// Asserts that the dictionary contains a key-value pair matching the predicate.
    /// This instance method enables calling Contains with proper type inference.
    /// Example: await Assert.That(dictionary).Contains(kvp => kvp.Value > 10);
    /// </summary>
    public CollectionContainsPredicateAssertion<KeyValuePair<TKey, TValue>> Contains(
        Func<KeyValuePair<TKey, TValue>, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".Contains({expression})");
        return new CollectionContainsPredicateAssertion<KeyValuePair<TKey, TValue>>(Context.Map<IEnumerable<KeyValuePair<TKey, TValue>>>(dict => dict), predicate);
    }

    /// <summary>
    /// Asserts that all key-value pairs in the dictionary satisfy the predicate.
    /// This instance method enables calling All with proper type inference.
    /// Example: await Assert.That(dictionary).All(kvp => kvp.Value > 0);
    /// </summary>
    public CollectionAllAssertion<KeyValuePair<TKey, TValue>> All(
        Func<KeyValuePair<TKey, TValue>, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".All({expression})");
        return new CollectionAllAssertion<KeyValuePair<TKey, TValue>>(Context.Map<IEnumerable<KeyValuePair<TKey, TValue>>>(dict => dict), predicate, expression ?? "predicate");
    }

    /// <summary>
    /// Returns a helper for the .All().Satisfy() pattern on dictionaries.
    /// This instance method enables calling All().Satisfy() with proper type inference.
    /// Example: await Assert.That(dictionary).All().Satisfy(kvp => kvp.Value.IsNotNull());
    /// </summary>
    public CollectionAllSatisfyHelper<KeyValuePair<TKey, TValue>> All()
    {
        Context.ExpressionBuilder.Append(".All()");
        return new CollectionAllSatisfyHelper<KeyValuePair<TKey, TValue>>(Context.Map<IEnumerable<KeyValuePair<TKey, TValue>>>(dict => dict));
    }

    /// <summary>
    /// Asserts that at least one key-value pair in the dictionary satisfies the predicate.
    /// This instance method enables calling Any with proper type inference.
    /// Example: await Assert.That(dictionary).Any(kvp => kvp.Value > 100);
    /// </summary>
    public CollectionAnyAssertion<KeyValuePair<TKey, TValue>> Any(
        Func<KeyValuePair<TKey, TValue>, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".Any({expression})");
        return new CollectionAnyAssertion<KeyValuePair<TKey, TValue>>(Context.Map<IEnumerable<KeyValuePair<TKey, TValue>>>(dict => dict), predicate, expression ?? "predicate");
    }

    /// <summary>
    /// Asserts that the dictionary has the expected number of key-value pairs.
    /// This instance method enables calling HasCount with proper type inference.
    /// Example: await Assert.That(dictionary).HasCount(5);
    /// </summary>
    public CollectionCountAssertion<KeyValuePair<TKey, TValue>> HasCount(
        int expectedCount,
        [CallerArgumentExpression(nameof(expectedCount))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".HasCount({expression})");
        return new CollectionCountAssertion<KeyValuePair<TKey, TValue>>(Context.Map<IEnumerable<KeyValuePair<TKey, TValue>>>(dict => dict), expectedCount);
    }

    /// <summary>
    /// Returns a wrapper for fluent count assertions on dictionaries.
    /// This enables the pattern: .HasCount().GreaterThan(5)
    /// Example: await Assert.That(dictionary).HasCount().EqualTo(5);
    /// </summary>
    public CountWrapper<KeyValuePair<TKey, TValue>> HasCount()
    {
        Context.ExpressionBuilder.Append(".HasCount()");
        return new CountWrapper<KeyValuePair<TKey, TValue>>(Context.Map<IEnumerable<KeyValuePair<TKey, TValue>>>(dict => dict));
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
