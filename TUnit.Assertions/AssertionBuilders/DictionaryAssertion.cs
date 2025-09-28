using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Dictionary-specific assertions
/// </summary>
public class DictionaryAssertion<TKey, TValue> : AssertionBase<IDictionary<TKey, TValue>>
{
    private readonly TKey? _key;
    private readonly bool _shouldContain;

    public DictionaryAssertion(Func<Task<IDictionary<TKey, TValue>>> actualValueProvider, TKey? key, bool shouldContain)
        : base(actualValueProvider)
    {
        _key = key;
        _shouldContain = shouldContain;
    }

    public DictionaryAssertion(Func<IDictionary<TKey, TValue>> actualValueProvider, TKey? key, bool shouldContain)
        : base(actualValueProvider)
    {
        _key = key;
        _shouldContain = shouldContain;
    }

    public DictionaryAssertion(IDictionary<TKey, TValue> actualValue, TKey? key, bool shouldContain)
        : base(actualValue)
    {
        _key = key;
        _shouldContain = shouldContain;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var dictionary = await GetActualValueAsync();

        if (dictionary == null)
        {
            return AssertionResult.Fail("Expected a dictionary but was null");
        }

        if (_key == null)
        {
            return AssertionResult.Fail("Key cannot be null");
        }

        bool containsKey = dictionary.ContainsKey(_key);

        if (containsKey == _shouldContain)
        {
            return AssertionResult.Passed;
        }

        if (_shouldContain)
        {
            return AssertionResult.Fail($"Expected dictionary to contain key '{_key}' but it did not");
        }
        else
        {
            return AssertionResult.Fail($"Expected dictionary to not contain key '{_key}' but it did");
        }
    }
}

// Extension methods for dictionary assertions
public static class DictionaryAssertionExtensions
{
    // For IDictionary<TKey, TValue>
    public static DictionaryAssertion<TKey, TValue> ContainsKey<TKey, TValue>(this AssertionBuilder<IDictionary<TKey, TValue>> builder, TKey key)
    {
        return new DictionaryAssertion<TKey, TValue>(builder.ActualValueProvider, key, shouldContain: true);
    }

    public static DictionaryAssertion<TKey, TValue> DoesNotContainKey<TKey, TValue>(this AssertionBuilder<IDictionary<TKey, TValue>> builder, TKey key)
    {
        return new DictionaryAssertion<TKey, TValue>(builder.ActualValueProvider, key, shouldContain: false);
    }

    // For Dictionary<TKey, TValue>
    public static DictionaryAssertion<TKey, TValue> ContainsKey<TKey, TValue>(this AssertionBuilder<Dictionary<TKey, TValue>> builder, TKey key)
        where TKey : notnull
    {
        Func<Task<IDictionary<TKey, TValue>>> provider = async () => await builder.ActualValueProvider();
        return new DictionaryAssertion<TKey, TValue>(provider, key, shouldContain: true);
    }

    // For Dictionary<string, TValue> with comparer
    public static CustomAssertion<Dictionary<string, TValue>> ContainsKey<TValue>(this AssertionBuilder<Dictionary<string, TValue>> builder, string key, IEqualityComparer<string> comparer)
    {
        return new CustomAssertion<Dictionary<string, TValue>>(builder.ActualValueProvider,
            dict => dict != null && dict.Keys.Contains(key, comparer),
            $"Expected dictionary to contain key '{key}' using specified comparer");
    }

    public static DictionaryAssertion<TKey, TValue> DoesNotContainKey<TKey, TValue>(this AssertionBuilder<Dictionary<TKey, TValue>> builder, TKey key)
        where TKey : notnull
    {
        Func<Task<IDictionary<TKey, TValue>>> provider = async () => await builder.ActualValueProvider();
        return new DictionaryAssertion<TKey, TValue>(provider, key, shouldContain: false);
    }

    // For IReadOnlyDictionary<TKey, TValue>
    public static CustomAssertion<IReadOnlyDictionary<TKey, TValue>> ContainsKey<TKey, TValue>(this AssertionBuilder<IReadOnlyDictionary<TKey, TValue>> builder, TKey key)
    {
        return new CustomAssertion<IReadOnlyDictionary<TKey, TValue>>(builder.ActualValueProvider,
            dict => dict?.ContainsKey(key) ?? false,
            $"Expected dictionary to contain key '{key}'");
    }

    public static CustomAssertion<IReadOnlyDictionary<TKey, TValue>> DoesNotContainKey<TKey, TValue>(this AssertionBuilder<IReadOnlyDictionary<TKey, TValue>> builder, TKey key)
    {
        return new CustomAssertion<IReadOnlyDictionary<TKey, TValue>>(builder.ActualValueProvider,
            dict => dict == null || !dict.ContainsKey(key),
            $"Expected dictionary not to contain key '{key}'");
    }

    // Generic overload for any type implementing IReadOnlyDictionary
    public static CustomAssertion<T> ContainsKey<T>(this AssertionBuilder<T> builder, string key)
        where T : IReadOnlyDictionary<string, string>
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            dict => dict?.ContainsKey(key) ?? false,
            $"Expected dictionary to contain key '{key}'");
    }

    // Generic overload for any IDictionary
    public static CustomAssertion<T> ContainsKey<T>(this AssertionBuilder<T> builder, object key)
        where T : IDictionary
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            dict => dict?.Contains(key) ?? false,
            $"Expected dictionary to contain key '{key}'");
    }
}