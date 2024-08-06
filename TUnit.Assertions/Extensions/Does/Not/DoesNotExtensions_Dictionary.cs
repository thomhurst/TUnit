#nullable disable

using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class DoesNotExtensions
{
    public static BaseAssertCondition<TDictionary, TAnd, TOr> ContainKey<TDictionary, TKey, TAnd, TOr>(this DoesNot<TDictionary, TAnd, TOr> does, TKey expected, IEqualityComparer<TKey> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TDictionary : IDictionary
        where TAnd : And<TDictionary, TAnd, TOr>, IAnd<TAnd, TDictionary, TAnd, TOr>
        where TOr : Or<TDictionary, TAnd, TOr>, IOr<TOr, TDictionary, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(does, new DelegateAssertCondition<TDictionary, TKey, TAnd, TOr>(
            does.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue),
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return !actual.Keys.Cast<TKey>().Contains(expected, equalityComparer);
            },
            (_, _) => $"The key \"{expected}\" was found in the dictionary"));
    }
    
    public static BaseAssertCondition<TDictionary, TAnd, TOr> ContainValue<TDictionary, TValue, TAnd, TOr>(this DoesNot<TDictionary, TAnd, TOr> does, TValue expected, IEqualityComparer<TValue> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TDictionary : IDictionary
        where TAnd : And<TDictionary, TAnd, TOr>, IAnd<TAnd, TDictionary, TAnd, TOr>
        where TOr : Or<TDictionary, TAnd, TOr>, IOr<TOr, TDictionary, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(does, new DelegateAssertCondition<TDictionary, TValue, TAnd, TOr>(
            does.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue),
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return !actual.Values.Cast<TValue>().Contains(expected, equalityComparer);
            },
            (_, _) => $"The value \"{expected}\" was found in the dictionary"));
    }
}