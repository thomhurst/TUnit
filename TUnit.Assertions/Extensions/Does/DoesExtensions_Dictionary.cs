#nullable disable

using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class DoesExtensions
{
    public static BaseAssertCondition<TDictionary, TAnd, TOr> ContainsKey<TDictionary, TKey, TAnd, TOr>(this IDoes<TDictionary, TAnd, TOr> does, TKey expected, IEqualityComparer<TKey> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TDictionary : IDictionary
        where TAnd : And<TDictionary, TAnd, TOr>, IAnd<TDictionary, TAnd, TOr>
        where TOr : Or<TDictionary, TAnd, TOr>, IOr<TDictionary, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(does.AssertionConnector, new DelegateAssertCondition<TDictionary, TKey, TAnd, TOr>(
            does.AssertionConnector.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue),
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return actual.Keys.Cast<TKey>().Contains(expected, equalityComparer);
            },
            (_, _) => $"The key \"{expected}\" was not found in the dictionary"));
    }
    
    public static BaseAssertCondition<TDictionary, TAnd, TOr> ContainsValue<TDictionary, TValue, TAnd, TOr>(this IDoes<TDictionary, TAnd, TOr> does, TValue expected, IEqualityComparer<TValue> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TDictionary : IDictionary
        where TAnd : And<TDictionary, TAnd, TOr>, IAnd<TDictionary, TAnd, TOr>
        where TOr : Or<TDictionary, TAnd, TOr>, IOr<TDictionary, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(does.AssertionConnector, new DelegateAssertCondition<TDictionary, TValue, TAnd, TOr>(
            does.AssertionConnector.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue),
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return actual.Values.Cast<TValue>().Contains(expected, equalityComparer);
            },
            (_, _) => $"The value \"{expected}\" was not found in the dictionary"));
    }
}