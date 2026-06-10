using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Assertion source for the value stored at a dictionary key.
/// Exposes the full value-assertion surface (IsEqualTo, IsNotNull, Member, Satisfies, …)
/// by virtue of being an <see cref="IAssertionSource{TValue}"/>.
/// Created via <c>ContainsKey(key).And.Value</c>.
/// </summary>
/// <typeparam name="TValue">The dictionary value type.</typeparam>
[global::TUnit.Assertions.Attributes.GenerateCollectionShapeDrillIns]
public sealed class DictionaryValueSource<TValue> : ValueAssertion<TValue>
{
    internal DictionaryValueSource(AssertionContext<TValue> context)
        : base(context)
    {
    }
}

/// <summary>
/// Shared value lookup for the <c>ContainsKey(key).And.Value</c> drill-in, used by both the read-only
/// and mutable continuations (which cannot share a base due to the IReadOnlyDictionary vs IDictionary
/// constraint). Existence/null is already validated by the ContainsKey pre-work, so a miss returns
/// default rather than throwing — letting the pre-work's clean failure surface.
/// </summary>
internal static class DictionaryValueLookup
{
    internal static TValue? Extract<TKey, TValue>(
        IEnumerable<KeyValuePair<TKey, TValue>>? dictionary, TKey key, IEqualityComparer<TKey>? comparer)
        where TKey : notnull
    {
        if (dictionary is null)
        {
            return default;
        }

        if (comparer is not null)
        {
            foreach (var pair in dictionary)
            {
                if (comparer.Equals(pair.Key, key))
                {
                    return pair.Value;
                }
            }

            return default;
        }

        // No custom comparer: use the dictionary's own TryGetValue (O(1), and it honours the
        // dictionary's internal comparer — which a default-equality scan would not).
        if (dictionary is IReadOnlyDictionary<TKey, TValue> readOnly)
        {
            return readOnly.TryGetValue(key, out var value) ? value : default;
        }

        if (dictionary is IDictionary<TKey, TValue> mutable)
        {
            return mutable.TryGetValue(key, out var value) ? value : default;
        }

        foreach (var pair in dictionary)
        {
            if (EqualityComparer<TKey>.Default.Equals(pair.Key, key))
            {
                return pair.Value;
            }
        }

        return default;
    }
}

/// <summary>
/// And continuation returned by <c>ContainsKey(key).And</c> on a read-only dictionary.
/// Behaves like the standard <see cref="DictionaryAndContinuation{TDictionary,TKey,TValue}"/>
/// but additionally carries the asserted key so the entry's value can be drilled into via
/// <see cref="Value"/>.
/// Example: <c>await Assert.That(dict).ContainsKey("key").And.Value.IsEqualTo(123);</c>
/// </summary>
public sealed class DictionaryContainsKeyAndContinuation<TDictionary, TKey, TValue>
    : DictionaryAndContinuation<TDictionary, TKey, TValue>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly TKey _expectedKey;
    private readonly IEqualityComparer<TKey>? _comparer;

    internal DictionaryContainsKeyAndContinuation(
        AssertionContext<TDictionary> context,
        Assertion<TDictionary> previousAssertion,
        TKey expectedKey,
        IEqualityComparer<TKey>? comparer)
        : base(context, previousAssertion)
    {
        _expectedKey = expectedKey;
        _comparer = comparer;
    }

    /// <summary>
    /// Drills into the value stored at the key asserted by the preceding <c>ContainsKey</c>,
    /// allowing assertions to be made directly against the value.
    /// The <c>ContainsKey</c> check runs first (as pre-work transferred by the <c>.And</c> link),
    /// so a missing key fails with the standard "to contain key" message before the value is read.
    /// </summary>
    public DictionaryValueSource<TValue> Value
    {
        get
        {
            Context.ExpressionBuilder.Append(".Value");
            var valueContext = Context.Map<TValue>(
                dictionary => DictionaryValueLookup.Extract<TKey, TValue>(dictionary, _expectedKey, _comparer));
            return new DictionaryValueSource<TValue>(valueContext);
        }
    }
}

/// <summary>
/// And continuation returned by <c>ContainsKey(key).And</c> on a mutable dictionary (IDictionary).
/// Mutable twin of <see cref="DictionaryContainsKeyAndContinuation{TDictionary,TKey,TValue}"/>.
/// </summary>
public sealed class MutableDictionaryContainsKeyAndContinuation<TDictionary, TKey, TValue>
    : MutableDictionaryAndContinuation<TDictionary, TKey, TValue>
    where TDictionary : IDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly TKey _expectedKey;
    private readonly IEqualityComparer<TKey>? _comparer;

    internal MutableDictionaryContainsKeyAndContinuation(
        AssertionContext<TDictionary> context,
        Assertion<TDictionary> previousAssertion,
        TKey expectedKey,
        IEqualityComparer<TKey>? comparer)
        : base(context, previousAssertion)
    {
        _expectedKey = expectedKey;
        _comparer = comparer;
    }

    /// <inheritdoc cref="DictionaryContainsKeyAndContinuation{TDictionary,TKey,TValue}.Value"/>
    public DictionaryValueSource<TValue> Value
    {
        get
        {
            Context.ExpressionBuilder.Append(".Value");
            var valueContext = Context.Map<TValue>(
                dictionary => DictionaryValueLookup.Extract<TKey, TValue>(dictionary, _expectedKey, _comparer));
            return new DictionaryValueSource<TValue>(valueContext);
        }
    }
}
