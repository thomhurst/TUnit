using System.Text;
using TUnit.Assertions.Conditions.Helpers;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a dictionary contains a specific key.
/// Inherits from DictionaryAssertionBase to enable chaining of dictionary methods.
/// Available as an instance method on DictionaryAssertionBase for proper type inference.
/// </summary>
public class DictionaryContainsKeyAssertion<TDictionary, TKey, TValue> : Sources.DictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
{
    private readonly TKey _expectedKey;
    private readonly IEqualityComparer<TKey>? _comparer;

    public DictionaryContainsKeyAssertion(
        AssertionContext<TDictionary> context,
        TKey expectedKey,
        IEqualityComparer<TKey>? comparer = null)
        : base(context)
    {
        _expectedKey = expectedKey;
        _comparer = comparer;
    }

    public DictionaryContainsKeyAssertion<TDictionary, TKey, TValue> Using(IEqualityComparer<TKey> comparer)
    {
        return new DictionaryContainsKeyAssertion<TDictionary, TKey, TValue>(Context, _expectedKey, comparer);
    }

    public DictionaryContainsKeyAssertion<TDictionary, TKey, TValue> Using(Func<TKey?, TKey?, bool> equalityPredicate)
    {
        return new DictionaryContainsKeyAssertion<TDictionary, TKey, TValue>(
            Context, _expectedKey, new FuncEqualityComparer<TKey>(equalityPredicate));
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("dictionary was null"));
        }

        bool found;
        if (_comparer != null)
        {
            // Use custom comparer to search for key
            found = value.Keys.Any(k => _comparer.Equals(k, _expectedKey));
        }
        else
        {
            found = value.ContainsKey(_expectedKey);
        }

        if (found)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"key {_expectedKey} not found"));
    }

    protected override string GetExpectation() => $"to contain key {_expectedKey}";
}

/// <summary>
/// Asserts that a dictionary does NOT contain a specific key.
/// Inherits from DictionaryAssertionBase to enable chaining of dictionary methods.
/// Available as an instance method on DictionaryAssertionBase for proper type inference.
/// </summary>
public class DictionaryDoesNotContainKeyAssertion<TDictionary, TKey, TValue> : Sources.DictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
{
    private readonly TKey _expectedKey;

    public DictionaryDoesNotContainKeyAssertion(
        AssertionContext<TDictionary> context,
        TKey expectedKey)
        : base(context)
    {
        _expectedKey = expectedKey;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("dictionary was null"));
        }

        if (!value.ContainsKey(_expectedKey))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"key {_expectedKey} was found"));
    }

    protected override string GetExpectation() => $"to not contain key {_expectedKey}";
}
