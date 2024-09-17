#nullable disable

using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class DoesNotExtensions
{
    public static InvokableAssertionBuilder<TDictionary, TAnd, TOr> DoesNotContainKey<TDictionary, TKey, TAnd, TOr>(this IValueSource<TDictionary, TAnd, TOr> valueSource, TKey expected, IEqualityComparer<TKey> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TDictionary : IDictionary
        where TAnd : IAnd<TDictionary, TAnd, TOr>
        where TOr : IOr<TDictionary, TAnd, TOr>
    {
        return new DelegateAssertCondition<TDictionary, TKey, TAnd, TOr>(
            valueSource.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue),
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return !actual.Keys.Cast<TKey>().Contains(expected, equalityComparer);
            },
            (_, _) => $"The key \"{expected}\" was found in the dictionary")
            .ChainedTo(valueSource.AssertionBuilder);
    }
    
    public static InvokableAssertionBuilder<TDictionary, TAnd, TOr> DoesNotContainValue<TDictionary, TValue, TAnd, TOr>(this IValueSource<TDictionary, TAnd, TOr> valueSource, TValue expected, IEqualityComparer<TValue> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TDictionary : IDictionary
        where TAnd : IAnd<TDictionary, TAnd, TOr>
        where TOr : IOr<TDictionary, TAnd, TOr>
    {
        return new DelegateAssertCondition<TDictionary, TValue, TAnd, TOr>(
            valueSource.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue),
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return !actual.Values.Cast<TValue>().Contains(expected, equalityComparer);
            },
            (_, _) => $"The value \"{expected}\" was found in the dictionary")
            .ChainedTo(valueSource.AssertionBuilder);
    }
}