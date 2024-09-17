#nullable disable

using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class DoesExtensions
{
    public static AssertionBuilder<TDictionary, TAnd, TOr> ContainsKey<TDictionary, TKey, TAnd, TOr>(this AssertionBuilder<TDictionary, TAnd, TOr> assertionBuilder, TKey expected, IEqualityComparer<TKey> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TDictionary : IDictionary
        where TAnd : IAnd<TDictionary, TAnd, TOr>
        where TOr : IOr<TDictionary, TAnd, TOr>
    {
        return new DelegateAssertCondition<TDictionary, TKey, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(doNotPopulateThisValue),
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return actual.Keys.Cast<TKey>().Contains(expected, equalityComparer);
            },
            (_, _) => $"The key \"{expected}\" was not found in the dictionary")
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TDictionary, TAnd, TOr> ContainsValue<TDictionary, TValue, TAnd, TOr>(this AssertionBuilder<TDictionary, TAnd, TOr> assertionBuilder, TValue expected, IEqualityComparer<TValue> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TDictionary : IDictionary
        where TAnd : IAnd<TDictionary, TAnd, TOr>
        where TOr : IOr<TDictionary, TAnd, TOr>
    {
        return new DelegateAssertCondition<TDictionary, TValue, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(doNotPopulateThisValue),
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return actual.Values.Cast<TValue>().Contains(expected, equalityComparer);
            },
            (_, _) => $"The value \"{expected}\" was not found in the dictionary")
            .ChainedTo(assertionBuilder);
    }
}