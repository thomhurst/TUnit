using TUnit.Assertions.Adapters;
using TUnit.Assertions.Conditions.Helpers;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a mutable dictionary contains the specified key.
/// </summary>
public class MutableDictionaryContainsKeyAssertion<TDictionary, TKey, TValue> : MutableDictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly TKey _expectedKey;
    private readonly IEqualityComparer<TKey>? _comparer;

    public MutableDictionaryContainsKeyAssertion(AssertionContext<TDictionary> context, TKey expectedKey, IEqualityComparer<TKey>? comparer = null)
        : base(context)
    {
        _expectedKey = expectedKey;
        _comparer = comparer;
    }

    public MutableDictionaryContainsKeyAssertion<TDictionary, TKey, TValue> Using(IEqualityComparer<TKey> comparer)
    {
        return new MutableDictionaryContainsKeyAssertion<TDictionary, TKey, TValue>(Context, _expectedKey, comparer);
    }

    public MutableDictionaryContainsKeyAssertion<TDictionary, TKey, TValue> Using(Func<TKey?, TKey?, bool> equalityPredicate)
    {
        return new MutableDictionaryContainsKeyAssertion<TDictionary, TKey, TValue>(
            Context, _expectedKey, new FuncEqualityComparer<TKey>(equalityPredicate));
    }

    protected override string GetExpectation() => $"to contain key {_expectedKey}";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("dictionary was null"));
        }

        bool found;
        if (_comparer != null)
        {
            found = metadata.Value.Keys.Any(k => _comparer.Equals(k, _expectedKey));
        }
        else
        {
            found = metadata.Value.ContainsKey(_expectedKey);
        }

        if (found)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"key {_expectedKey} not found"));
    }
}

/// <summary>
/// Asserts that a mutable dictionary does not contain the specified key.
/// </summary>
public class MutableDictionaryDoesNotContainKeyAssertion<TDictionary, TKey, TValue> : MutableDictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly TKey _expectedKey;

    public MutableDictionaryDoesNotContainKeyAssertion(AssertionContext<TDictionary> context, TKey expectedKey)
        : base(context)
    {
        _expectedKey = expectedKey;
    }

    protected override string GetExpectation() => $"to not contain key {_expectedKey}";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("dictionary was null"));
        }

        if (metadata.Value.ContainsKey(_expectedKey))
        {
            return Task.FromResult(AssertionResult.Failed($"key {_expectedKey} was found"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }
}

/// <summary>
/// Asserts that a mutable dictionary contains the specified value.
/// </summary>
public class MutableDictionaryContainsValueAssertion<TDictionary, TKey, TValue> : MutableDictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly TValue _expectedValue;

    public MutableDictionaryContainsValueAssertion(AssertionContext<TDictionary> context, TValue expectedValue)
        : base(context)
    {
        _expectedValue = expectedValue;
    }

    protected override string GetExpectation() => $"to contain value {_expectedValue}";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("dictionary was null"));
        }

        var comparer = EqualityComparer<TValue>.Default;
        foreach (var v in metadata.Value.Values)
        {
            if (comparer.Equals(v, _expectedValue))
            {
                return Task.FromResult(AssertionResult.Passed);
            }
        }

        return Task.FromResult(AssertionResult.Failed($"value {_expectedValue} not found"));
    }
}

/// <summary>
/// Asserts that a mutable dictionary does not contain the specified value.
/// </summary>
public class MutableDictionaryDoesNotContainValueAssertion<TDictionary, TKey, TValue> : MutableDictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly TValue _expectedValue;

    public MutableDictionaryDoesNotContainValueAssertion(AssertionContext<TDictionary> context, TValue expectedValue)
        : base(context)
    {
        _expectedValue = expectedValue;
    }

    protected override string GetExpectation() => $"to not contain value {_expectedValue}";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("dictionary was null"));
        }

        var comparer = EqualityComparer<TValue>.Default;
        foreach (var v in metadata.Value.Values)
        {
            if (comparer.Equals(v, _expectedValue))
            {
                return Task.FromResult(AssertionResult.Failed($"value {_expectedValue} was found"));
            }
        }

        return Task.FromResult(AssertionResult.Passed);
    }
}

/// <summary>
/// Asserts that a mutable dictionary contains the specified key with the specified value.
/// </summary>
public class MutableDictionaryContainsKeyWithValueAssertion<TDictionary, TKey, TValue> : MutableDictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly TKey _expectedKey;
    private readonly TValue _expectedValue;

    public MutableDictionaryContainsKeyWithValueAssertion(AssertionContext<TDictionary> context, TKey expectedKey, TValue expectedValue)
        : base(context)
    {
        _expectedKey = expectedKey;
        _expectedValue = expectedValue;
    }

    protected override string GetExpectation() => $"to contain key {_expectedKey} with value {_expectedValue}";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("dictionary was null"));
        }

        if (!metadata.Value.TryGetValue(_expectedKey, out var actualValue))
        {
            return Task.FromResult(AssertionResult.Failed($"key {_expectedKey} not found"));
        }

        var comparer = EqualityComparer<TValue>.Default;
        if (comparer.Equals(actualValue, _expectedValue))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"key {_expectedKey} has value {actualValue}, not {_expectedValue}"));
    }
}

/// <summary>
/// Asserts that all keys in a mutable dictionary satisfy the predicate.
/// </summary>
public class MutableDictionaryAllKeysAssertion<TDictionary, TKey, TValue> : MutableDictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly Func<TKey, bool> _predicate;

    public MutableDictionaryAllKeysAssertion(AssertionContext<TDictionary> context, Func<TKey, bool> predicate)
        : base(context)
    {
        _predicate = predicate;
    }

    protected override string GetExpectation() => "all keys to satisfy the predicate";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("dictionary was null"));
        }

        foreach (var key in metadata.Value.Keys)
        {
            if (!_predicate(key))
            {
                return Task.FromResult(AssertionResult.Failed($"key {key} did not satisfy the predicate"));
            }
        }

        return Task.FromResult(AssertionResult.Passed);
    }
}

/// <summary>
/// Asserts that all values in a mutable dictionary satisfy the predicate.
/// </summary>
public class MutableDictionaryAllValuesAssertion<TDictionary, TKey, TValue> : MutableDictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly Func<TValue, bool> _predicate;

    public MutableDictionaryAllValuesAssertion(AssertionContext<TDictionary> context, Func<TValue, bool> predicate)
        : base(context)
    {
        _predicate = predicate;
    }

    protected override string GetExpectation() => "all values to satisfy the predicate";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("dictionary was null"));
        }

        foreach (var value in metadata.Value.Values)
        {
            if (!_predicate(value))
            {
                return Task.FromResult(AssertionResult.Failed($"value {value} did not satisfy the predicate"));
            }
        }

        return Task.FromResult(AssertionResult.Passed);
    }
}

/// <summary>
/// Asserts that any key in a mutable dictionary satisfies the predicate.
/// </summary>
public class MutableDictionaryAnyKeyAssertion<TDictionary, TKey, TValue> : MutableDictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly Func<TKey, bool> _predicate;

    public MutableDictionaryAnyKeyAssertion(AssertionContext<TDictionary> context, Func<TKey, bool> predicate)
        : base(context)
    {
        _predicate = predicate;
    }

    protected override string GetExpectation() => "any key to satisfy the predicate";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("dictionary was null"));
        }

        foreach (var key in metadata.Value.Keys)
        {
            if (_predicate(key))
            {
                return Task.FromResult(AssertionResult.Passed);
            }
        }

        return Task.FromResult(AssertionResult.Failed("no key satisfied the predicate"));
    }
}

/// <summary>
/// Asserts that any value in a mutable dictionary satisfies the predicate.
/// </summary>
public class MutableDictionaryAnyValueAssertion<TDictionary, TKey, TValue> : MutableDictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly Func<TValue, bool> _predicate;

    public MutableDictionaryAnyValueAssertion(AssertionContext<TDictionary> context, Func<TValue, bool> predicate)
        : base(context)
    {
        _predicate = predicate;
    }

    protected override string GetExpectation() => "any value to satisfy the predicate";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("dictionary was null"));
        }

        foreach (var value in metadata.Value.Values)
        {
            if (_predicate(value))
            {
                return Task.FromResult(AssertionResult.Passed);
            }
        }

        return Task.FromResult(AssertionResult.Failed("no value satisfied the predicate"));
    }
}
