using TUnit.Assertions.Abstractions;
using TUnit.Assertions.Adapters;
using TUnit.Assertions.Collections;
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
    where TKey : notnull
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
    where TKey : notnull
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

/// <summary>
/// Asserts that a dictionary contains a specific value.
/// Uses CollectionChecks for the actual logic.
/// </summary>
public class DictionaryContainsValueAssertion<TDictionary, TKey, TValue> : Sources.DictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly TValue _expectedValue;
    private readonly IEqualityComparer<TValue>? _comparer;

    public DictionaryContainsValueAssertion(
        AssertionContext<TDictionary> context,
        TValue expectedValue,
        IEqualityComparer<TValue>? comparer = null)
        : base(context)
    {
        _expectedValue = expectedValue;
        _comparer = comparer;
    }

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

        var adapter = new ReadOnlyDictionaryAdapter<TKey, TValue>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckContainsValue(adapter, _expectedValue, _comparer));
    }

    protected override string GetExpectation() => $"to contain value {_expectedValue}";
}

/// <summary>
/// Asserts that a dictionary does NOT contain a specific value.
/// Uses CollectionChecks for the actual logic.
/// </summary>
public class DictionaryDoesNotContainValueAssertion<TDictionary, TKey, TValue> : Sources.DictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly TValue _expectedValue;
    private readonly IEqualityComparer<TValue>? _comparer;

    public DictionaryDoesNotContainValueAssertion(
        AssertionContext<TDictionary> context,
        TValue expectedValue,
        IEqualityComparer<TValue>? comparer = null)
        : base(context)
    {
        _expectedValue = expectedValue;
        _comparer = comparer;
    }

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

        var adapter = new ReadOnlyDictionaryAdapter<TKey, TValue>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckDoesNotContainValue(adapter, _expectedValue, _comparer));
    }

    protected override string GetExpectation() => $"to not contain value {_expectedValue}";
}

/// <summary>
/// Asserts that a dictionary contains a key with a specific value.
/// Uses CollectionChecks for the actual logic.
/// </summary>
public class DictionaryContainsKeyWithValueAssertion<TDictionary, TKey, TValue> : Sources.DictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly TKey _expectedKey;
    private readonly TValue _expectedValue;
    private readonly IEqualityComparer<TValue>? _comparer;

    public DictionaryContainsKeyWithValueAssertion(
        AssertionContext<TDictionary> context,
        TKey expectedKey,
        TValue expectedValue,
        IEqualityComparer<TValue>? comparer = null)
        : base(context)
    {
        _expectedKey = expectedKey;
        _expectedValue = expectedValue;
        _comparer = comparer;
    }

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

        var adapter = new ReadOnlyDictionaryAdapter<TKey, TValue>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckContainsKeyWithValue(adapter, _expectedKey, _expectedValue, _comparer));
    }

    protected override string GetExpectation() => $"to contain key {_expectedKey} with value {_expectedValue}";
}

/// <summary>
/// Asserts that all keys in the dictionary satisfy a predicate.
/// Uses CollectionChecks for the actual logic.
/// </summary>
public class DictionaryAllKeysAssertion<TDictionary, TKey, TValue> : Sources.DictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly Func<TKey, bool> _predicate;

    public DictionaryAllKeysAssertion(
        AssertionContext<TDictionary> context,
        Func<TKey, bool> predicate)
        : base(context)
    {
        _predicate = predicate;
    }

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

        var adapter = new ReadOnlyDictionaryAdapter<TKey, TValue>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckAllKeys(adapter, _predicate));
    }

    protected override string GetExpectation() => "all keys to satisfy the predicate";
}

/// <summary>
/// Asserts that all values in the dictionary satisfy a predicate.
/// Uses CollectionChecks for the actual logic.
/// </summary>
public class DictionaryAllValuesAssertion<TDictionary, TKey, TValue> : Sources.DictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly Func<TValue, bool> _predicate;

    public DictionaryAllValuesAssertion(
        AssertionContext<TDictionary> context,
        Func<TValue, bool> predicate)
        : base(context)
    {
        _predicate = predicate;
    }

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

        var adapter = new ReadOnlyDictionaryAdapter<TKey, TValue>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckAllValues(adapter, _predicate));
    }

    protected override string GetExpectation() => "all values to satisfy the predicate";
}

/// <summary>
/// Asserts that any key in the dictionary satisfies a predicate.
/// Uses CollectionChecks for the actual logic.
/// </summary>
public class DictionaryAnyKeyAssertion<TDictionary, TKey, TValue> : Sources.DictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly Func<TKey, bool> _predicate;

    public DictionaryAnyKeyAssertion(
        AssertionContext<TDictionary> context,
        Func<TKey, bool> predicate)
        : base(context)
    {
        _predicate = predicate;
    }

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

        var adapter = new ReadOnlyDictionaryAdapter<TKey, TValue>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckAnyKey(adapter, _predicate));
    }

    protected override string GetExpectation() => "any key to satisfy the predicate";
}

/// <summary>
/// Asserts that any value in the dictionary satisfies a predicate.
/// Uses CollectionChecks for the actual logic.
/// </summary>
public class DictionaryAnyValueAssertion<TDictionary, TKey, TValue> : Sources.DictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly Func<TValue, bool> _predicate;

    public DictionaryAnyValueAssertion(
        AssertionContext<TDictionary> context,
        Func<TValue, bool> predicate)
        : base(context)
    {
        _predicate = predicate;
    }

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

        var adapter = new ReadOnlyDictionaryAdapter<TKey, TValue>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckAnyValue(adapter, _predicate));
    }

    protected override string GetExpectation() => "any value to satisfy the predicate";
}
