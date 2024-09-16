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
    public static TOutput DoesNotContainKey<TAssertionBuilder, TOutput, TDictionary, TKey, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TKey expected, IEqualityComparer<TKey> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAssertionBuilder : AssertionBuilder<TDictionary, TAnd, TOr>, IOutputsChain<TOutput, TDictionary>
        where TOutput : InvokableAssertionBuilder<TDictionary, TAnd, TOr>
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
                return !actual.Keys.Cast<TKey>().Contains(expected, equalityComparer);
            },
            (_, _) => $"The key \"{expected}\" was found in the dictionary")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput DoesNotContainValue<TAssertionBuilder, TOutput, TDictionary, TValue, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TValue expected, IEqualityComparer<TValue> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TDictionary : IDictionary
        where TAssertionBuilder : AssertionBuilder<TDictionary, TAnd, TOr>, IOutputsChain<TOutput, TDictionary>
        where TOutput : InvokableAssertionBuilder<TDictionary, TAnd, TOr>
        where TAnd : IAnd<TDictionary, TAnd, TOr>
        where TOr : IOr<TDictionary, TAnd, TOr>
    {
        return new DelegateAssertCondition<TDictionary, TValue, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(doNotPopulateThisValue),
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return !actual.Values.Cast<TValue>().Contains(expected, equalityComparer);
            },
            (_, _) => $"The value \"{expected}\" was found in the dictionary")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
}