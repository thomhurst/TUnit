using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for dictionary type assertions
/// </summary>
public static class DictionaryAssertionExtensions
{
    // === HasCount that returns a ValueAssertionBuilder<int> for chaining ===
    public static ValueAssertionBuilder<int> HasCount<TKey, TValue>(this ValueAssertionBuilder<Dictionary<TKey, TValue>> builder)
        where TKey : notnull
    {
        return new ValueAssertionBuilder<int>(async () =>
        {
            var dict = await builder.ActualValueProvider();
            if (dict == null)
                throw new InvalidOperationException("Dictionary was null");
            return dict.Count;
        });
    }

    public static ValueAssertionBuilder<int> HasCount<TKey, TValue>(this ValueAssertionBuilder<IDictionary<TKey, TValue>> builder)
    {
        return new ValueAssertionBuilder<int>(async () =>
        {
            var dict = await builder.ActualValueProvider();
            if (dict == null)
                throw new InvalidOperationException("Dictionary was null");
            return dict.Count;
        });
    }
    public static CustomAssertion<Dictionary<TKey, TValue>> ContainsKey<TKey, TValue>(
        this ValueAssertionBuilder<Dictionary<TKey, TValue>> builder, TKey key)
        where TKey : notnull
    {
        return new CustomAssertion<Dictionary<TKey, TValue>>(builder.ActualValueProvider,
            dict => dict?.ContainsKey(key) ?? false,
            $"Expected dictionary to contain key '{key}'");
    }

    public static CustomAssertion<Dictionary<string, TValue>> ContainsKey<TValue>(
        this ValueAssertionBuilder<Dictionary<string, TValue>> builder, string key, IEqualityComparer<string> comparer)
    {
        return new CustomAssertion<Dictionary<string, TValue>>(builder.ActualValueProvider,
            dict =>
            {
                if (dict == null) return false;
                return dict.Keys.Any(k => comparer.Equals(k, key));
            },
            $"Expected dictionary to contain key '{key}' using custom comparer");
    }

    public static CustomAssertion<Dictionary<TKey, TValue>> DoesNotContainKey<TKey, TValue>(
        this ValueAssertionBuilder<Dictionary<TKey, TValue>> builder, TKey key)
        where TKey : notnull
    {
        return new CustomAssertion<Dictionary<TKey, TValue>>(builder.ActualValueProvider,
            dict => dict == null || !dict.ContainsKey(key),
            $"Expected dictionary to not contain key '{key}'");
    }

    public static CustomAssertion<Dictionary<TKey, TValue>> ContainsValue<TKey, TValue>(
        this ValueAssertionBuilder<Dictionary<TKey, TValue>> builder, TValue value)
        where TKey : notnull
    {
        return new CustomAssertion<Dictionary<TKey, TValue>>(builder.ActualValueProvider,
            dict => dict?.ContainsValue(value) ?? false,
            $"Expected dictionary to contain value '{value}'");
    }

    public static CustomAssertion<IDictionary<TKey, TValue>> ContainsKey<TKey, TValue>(
        this ValueAssertionBuilder<IDictionary<TKey, TValue>> builder, TKey key)
        where TKey : notnull
    {
        return new CustomAssertion<IDictionary<TKey, TValue>>(builder.ActualValueProvider,
            dict => dict?.ContainsKey(key) ?? false,
            $"Expected dictionary to contain key '{key}'");
    }

    public static CustomAssertion<IDictionary<TKey, TValue>> DoesNotContainKey<TKey, TValue>(
        this ValueAssertionBuilder<IDictionary<TKey, TValue>> builder, TKey key)
        where TKey : notnull
    {
        return new CustomAssertion<IDictionary<TKey, TValue>>(builder.ActualValueProvider,
            dict => dict == null || !dict.ContainsKey(key),
            $"Expected dictionary to not contain key '{key}'");
    }

    public static CustomAssertion<IReadOnlyDictionary<TKey, TValue>> ContainsKey<TKey, TValue>(
        this ValueAssertionBuilder<IReadOnlyDictionary<TKey, TValue>> builder, TKey key)
        where TKey : notnull
    {
        return new CustomAssertion<IReadOnlyDictionary<TKey, TValue>>(builder.ActualValueProvider,
            dict => dict?.ContainsKey(key) ?? false,
            $"Expected dictionary to contain key '{key}'");
    }

    public static CustomAssertion<IReadOnlyDictionary<TKey, TValue>> DoesNotContainKey<TKey, TValue>(
        this ValueAssertionBuilder<IReadOnlyDictionary<TKey, TValue>> builder, TKey key)
        where TKey : notnull
    {
        return new CustomAssertion<IReadOnlyDictionary<TKey, TValue>>(builder.ActualValueProvider,
            dict => dict == null || !dict.ContainsKey(key),
            $"Expected dictionary to not contain key '{key}'");
    }

[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
        Justification = "The reflection usage is guarded by interface checks and exceptions are handled")]
    public static CustomAssertion<TDictionary> ContainsKey<TDictionary>(
        this ValueAssertionBuilder<TDictionary> builder, object key)
        where TDictionary : class
    {
        return new CustomAssertion<TDictionary>(builder.ActualValueProvider,
            dict =>
            {
                if (dict == null) return false;

                var interfaces = dict.GetType().GetInterfaces();
                foreach (var iface in interfaces)
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
                    {
                        var containsKeyMethod = iface.GetMethod("ContainsKey");
                        if (containsKeyMethod != null)
                        {
                            try
                            {
                                return (bool)containsKeyMethod.Invoke(dict, new[] { key })!;
                            }
                            catch
                            {
                                return false;
                            }
                        }
                    }
                }

                foreach (var iface in interfaces)
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    {
                        var containsKeyMethod = iface.GetMethod("ContainsKey");
                        if (containsKeyMethod != null)
                        {
                            try
                            {
                                return (bool)containsKeyMethod.Invoke(dict, new[] { key })!;
                            }
                            catch
                            {
                                return false;
                            }
                        }
                    }
                }

                return false;
            },
            $"Expected dictionary to contain key '{key}'");
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
        Justification = "The reflection usage is guarded by interface checks and exceptions are handled")]
    public static CustomAssertion<TDictionary> DoesNotContainKey<TDictionary>(
        this ValueAssertionBuilder<TDictionary> builder, object key)
        where TDictionary : class
    {
        return new CustomAssertion<TDictionary>(builder.ActualValueProvider,
            dict =>
            {
                if (dict == null) return true;

                var interfaces = dict.GetType().GetInterfaces();
                foreach (var iface in interfaces)
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
                    {
                        var containsKeyMethod = iface.GetMethod("ContainsKey");
                        if (containsKeyMethod != null)
                        {
                            try
                            {
                                return !(bool)containsKeyMethod.Invoke(dict, new[] { key })!;
                            }
                            catch
                            {
                                return true;
                            }
                        }
                    }
                }

                foreach (var iface in interfaces)
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    {
                        var containsKeyMethod = iface.GetMethod("ContainsKey");
                        if (containsKeyMethod != null)
                        {
                            try
                            {
                                return !(bool)containsKeyMethod.Invoke(dict, new[] { key })!;
                            }
                            catch
                            {
                                return true;
                            }
                        }
                    }
                }

                return true;
            },
            $"Expected dictionary to not contain key '{key}'");
    }

    public static CustomAssertion<Dictionary<TKey, TValue>> ContainsKey<TKey, TValue>(
        this DualAssertionBuilder<Dictionary<TKey, TValue>> builder, TKey key)
        where TKey : notnull
    {
        return new CustomAssertion<Dictionary<TKey, TValue>>(builder.ActualValueProvider,
            dict => dict?.ContainsKey(key) ?? false,
            $"Expected dictionary to contain key '{key}'");
    }

    public static CustomAssertion<Dictionary<TKey, TValue>> DoesNotContainKey<TKey, TValue>(
        this DualAssertionBuilder<Dictionary<TKey, TValue>> builder, TKey key)
        where TKey : notnull
    {
        return new CustomAssertion<Dictionary<TKey, TValue>>(builder.ActualValueProvider,
            dict => dict == null || !dict.ContainsKey(key),
            $"Expected dictionary to not contain key '{key}'");
    }
}