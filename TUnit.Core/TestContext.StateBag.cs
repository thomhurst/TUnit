using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public partial class TestContext
{
    /// <inheritdoc/>
    ConcurrentDictionary<string, object?> ITestStateBag.Items => ObjectBag;

    /// <inheritdoc/>
    object? ITestStateBag.this[string key]
    {
        get => ObjectBag[key];
        set => ObjectBag[key] = value;
    }

    /// <inheritdoc/>
    int ITestStateBag.Count => ObjectBag.Count;

    /// <inheritdoc/>
    bool ITestStateBag.ContainsKey(string key) => ObjectBag.ContainsKey(key);

    /// <inheritdoc/>
    T ITestStateBag.GetOrAdd<T>(string key, Func<string, T> valueFactory)
    {
        var value = ObjectBag.GetOrAdd(key, static (k, valueFactory) => valueFactory(k)!, valueFactory);

        if (value is T typedValue)
        {
            return typedValue;
        }

        throw new InvalidCastException($"The value for key '{key}' is of type '{value?.GetType().Name}' and cannot be cast to '{typeof(T).Name}'.");
    }

    /// <inheritdoc/>
    bool ITestStateBag.TryGetValue<T>(string key, [MaybeNullWhen(false)] out T value)
    {
        if (ObjectBag.TryGetValue(key, out var objValue) && objValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc/>
    bool ITestStateBag.TryRemove(string key, [MaybeNullWhen(false)] out object? value)
    {
        return ObjectBag.TryRemove(key, out value);
    }
}
