#nullable disable

using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class DoesNotExtensions
{
    public static InvokableValueAssertionBuilder<TDictionary> DoesNotContainKey<TDictionary, TKey>(this IValueSource<TDictionary> valueSource, TKey expected, IEqualityComparer<TKey> equalityComparer = null, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        where TDictionary : IDictionary
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TDictionary, TKey>(expected,
            (actual, _, _) =>
            {
                Verify.ArgNotNull(actual);
                return !actual.Keys.Cast<TKey>().Contains(expected, equalityComparer);
            },
            (_, _, _) => $"The key \"{expected}\" was found in the dictionary",
            $"not contain the key '{expected}'")
            , [doNotPopulateThisValue]);
    }

    public static InvokableValueAssertionBuilder<TDictionary> DoesNotContainValue<TDictionary, TValue>(this IValueSource<TDictionary> valueSource, TValue expected, IEqualityComparer<TValue> equalityComparer = null, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        where TDictionary : IDictionary
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TDictionary, TValue>(expected,
            (actual, _, _) =>
            {
                Verify.ArgNotNull(actual);
                return !actual.Values.Cast<TValue>().Contains(expected, equalityComparer);
            },
            (_, _, _) => $"The value \"{expected}\" was found in the dictionary",
            $"not contain the value '{expected}'")
            , [doNotPopulateThisValue]);
    }

    public static InvokableValueAssertionBuilder<IReadOnlyDictionary<TKey, TValue>> DoesNotContainKey<TKey, TValue>(this IValueSource<IReadOnlyDictionary<TKey, TValue>> valueSource, TKey expected, IEqualityComparer<TKey> equalityComparer = null, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<IReadOnlyDictionary<TKey, TValue>, TKey>(expected,
                (actual, _, _) =>
                {
                    Verify.ArgNotNull(actual);
                    return !actual.Keys.Contains(expected, equalityComparer);
                },
                (_, _, _) => $"The key \"{expected}\" was found in the dictionary",
                $"not contain the key '{expected}'")
            , [doNotPopulateThisValue]);
    }

    public static InvokableValueAssertionBuilder<IReadOnlyDictionary<TKey, TValue>> DoesNotContainValue<TKey, TValue>(this IValueSource<IReadOnlyDictionary<TKey, TValue>> valueSource, TValue expected, IEqualityComparer<TValue> equalityComparer = null, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<IReadOnlyDictionary<TKey, TValue>, TValue>(expected,
                (actual, _, _) =>
                {
                    Verify.ArgNotNull(actual);
                    return !actual.Values.Contains(expected, equalityComparer);
                },
                (_, _, _) => $"The value \"{expected}\" was found in the dictionary",
                $"not contain the value '{expected}'")
            , [doNotPopulateThisValue]);
    }
}
