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
    public static TAssertionBuilder DoesNotContainKey<TAssertionBuilder, TDictionary, TKey, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TKey expected, IEqualityComparer<TKey> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAssertionBuilder : IChainableAssertionBuilder<TAssertionBuilder, AssertionBuilder<TDictionary, TAnd, TOr>, TDictionary, TAnd, TOr>
        where TDictionary : IDictionary
        where TAnd : And<TDictionary, TAnd, TOr>, IAnd<TDictionary, TAnd, TOr>
        where TOr : Or<TDictionary, TAnd, TOr>, IOr<TDictionary, TAnd, TOr>
    {
        
        return assertionBuilder.WithAssertion<TAssertionBuilder>(new DelegateAssertCondition<TDictionary, TKey, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(doNotPopulateThisValue),
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return !actual.Keys.Cast<TKey>().Contains(expected, equalityComparer);
            },
            (_, _) => $"The key \"{expected}\" was found in the dictionary"));
    }
    
    public static BaseAssertCondition<TDictionary, TAnd, TOr> DoesNotContainValue<TDictionary, TValue, TAnd, TOr>(this IDoes<TDictionary, TAnd, TOr> does, TValue expected, IEqualityComparer<TValue> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
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
                return !actual.Values.Cast<TValue>().Contains(expected, equalityComparer);
            },
            (_, _) => $"The value \"{expected}\" was found in the dictionary"));
    }
}