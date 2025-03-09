#nullable disable

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class ImmutableArrayIsNotExtensions
{
    public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsNotEquivalentTo<TInner>(this IValueSource<ImmutableArray<TInner>> valueSource, IEnumerable<TInner> expected, IEqualityComparer<TInner> equalityComparer = null, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new EnumerableNotEquivalentToExpectedValueAssertCondition<ImmutableArray<TInner>, TInner>(expected, equalityComparer)
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsNotEmpty<TInner>(this IValueSource<ImmutableArray<TInner>> valueSource)
    {
        return valueSource.RegisterAssertion(new EnumerableCountNotEqualToExpectedValueAssertCondition<ImmutableArray<TInner>, TInner>(0)
            , []);
    }
}