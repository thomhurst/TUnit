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
public sealed class DictionaryValueSource<TValue> : ValueAssertion<TValue>
{
    internal DictionaryValueSource(AssertionContext<TValue> context)
        : base(context)
    {
    }
}

/// <summary>
/// And continuation returned by <c>ContainsKey(key).And</c> on a read-only dictionary.
/// Behaves like the standard <see cref="DictionaryAndContinuation{TDictionary,TKey,TValue}"/>
/// but additionally carries the asserted key so the entry's value can be drilled into via
/// <see cref="Value"/>.
/// Example: <c>await Assert.That(dict).ContainsKey("key").And.Value.IsEqualTo(123);</c>
/// </summary>
public class DictionaryContainsKeyAndContinuation<TDictionary, TKey, TValue>
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
            var key = _expectedKey;
            var comparer = _comparer;
            Context.ExpressionBuilder.Append(".Value");
            var valueContext = Context.Map<TValue>(dictionary => ExtractValue(dictionary, key, comparer));
            return new DictionaryValueSource<TValue>(valueContext);
        }
    }

    private static TValue? ExtractValue(TDictionary? dictionary, TKey key, IEqualityComparer<TKey>? comparer)
    {
        // Existence/null is already validated by the ContainsKey pre-work; these guards just
        // avoid throwing here so the pre-work's clean failure surfaces instead.
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

        return dictionary.TryGetValue(key, out var value) ? value : default;
    }
}

/// <summary>
/// And continuation returned by <c>ContainsKey(key).And</c> on a mutable dictionary (IDictionary).
/// Mutable twin of <see cref="DictionaryContainsKeyAndContinuation{TDictionary,TKey,TValue}"/>.
/// </summary>
public class MutableDictionaryContainsKeyAndContinuation<TDictionary, TKey, TValue>
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
            var key = _expectedKey;
            var comparer = _comparer;
            Context.ExpressionBuilder.Append(".Value");
            var valueContext = Context.Map<TValue>(dictionary => ExtractValue(dictionary, key, comparer));
            return new DictionaryValueSource<TValue>(valueContext);
        }
    }

    private static TValue? ExtractValue(TDictionary? dictionary, TKey key, IEqualityComparer<TKey>? comparer)
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

        return dictionary.TryGetValue(key, out var value) ? value : default;
    }
}
